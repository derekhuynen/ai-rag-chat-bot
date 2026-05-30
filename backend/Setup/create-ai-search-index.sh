#!/bin/bash
# Azure AI Search Index Creation Script
# This script creates the ai-chat-documents index for RAG functionality

"${SEARCH_ENDPOINT:=${1:-}}"
"${API_KEY:=${2:-}}"
INDEX_NAME="${3:-ai-chat-documents}"

if [ -z "$SEARCH_ENDPOINT" ]; then
  echo "ERROR: SEARCH_ENDPOINT must be provided as an argument or environment variable." >&2
  echo "Example: SEARCH_ENDPOINT=\"https://YOUR-SEARCH-SERVICE.search.windows.net\" ./create-ai-search-index.sh" >&2
  exit 1
fi

echo "Creating Azure AI Search Index: $INDEX_NAME"
echo "Endpoint: $SEARCH_ENDPOINT"

# Auth: prefer keyless AAD (Search is deployed with local auth disabled).
if [ -z "${API_KEY:-}" ]; then
  TOKEN="$(az account get-access-token --resource https://search.azure.com --query accessToken -o tsv)"
  if [ -z "$TOKEN" ]; then echo "Could not get an AAD token. Run 'az login' first." >&2; exit 1; fi
  AUTH_HEADER="Authorization: Bearer $TOKEN"
else
  AUTH_HEADER="api-key: $API_KEY"
fi

# Index schema JSON
INDEX_SCHEMA='{
  "name": "'$INDEX_NAME'",
  "fields": [
    {
      "name": "id",
      "type": "Edm.String",
      "key": true,
      "searchable": false
    },
    {
      "name": "documentId",
      "type": "Edm.String",
      "searchable": false,
      "filterable": true
    },
    {
      "name": "fileName",
      "type": "Edm.String",
      "searchable": true,
      "filterable": true,
      "facetable": true
    },
    {
      "name": "content",
      "type": "Edm.String",
      "searchable": true,
      "analyzer": "en.microsoft"
    },
    {
      "name": "summary",
      "type": "Edm.String",
      "searchable": true,
      "analyzer": "en.microsoft"
    },
    {
      "name": "contentVector",
      "type": "Collection(Edm.Single)",
      "searchable": true,
      "dimensions": 1536,
      "vectorSearchProfile": "default-vector-profile"
    },
    {
      "name": "summaryVector",
      "type": "Collection(Edm.Single)",
      "searchable": true,
      "dimensions": 1536,
      "vectorSearchProfile": "default-vector-profile"
    },
    {
      "name": "chunkIndex",
      "type": "Edm.Int32",
      "filterable": true,
      "sortable": true
    },
    {
      "name": "page",
      "type": "Edm.Int32",
      "filterable": true
    },
    {
      "name": "uploadedBy",
      "type": "Edm.String",
      "filterable": true
    },
    {
      "name": "uploadedAt",
      "type": "Edm.DateTimeOffset",
      "filterable": true,
      "sortable": true
    },
    {
      "name": "blobUrl",
      "type": "Edm.String",
      "searchable": false
    }
  ],
  "vectorSearch": {
    "algorithms": [
      {
        "name": "hnsw-config",
        "kind": "hnsw",
        "hnswParameters": {
          "metric": "cosine",
          "m": 4,
          "efConstruction": 400,
          "efSearch": 500
        }
      }
    ],
    "profiles": [
      {
        "name": "default-vector-profile",
        "algorithm": "hnsw-config"
      }
    ]
  },
  "semantic": {
    "configurations": [
      {
        "name": "default-semantic-config",
        "prioritizedFields": {
          "contentFields": [
            { "fieldName": "content" }
          ],
          "keywordsFields": [
            { "fieldName": "fileName" }
          ]
        }
      }
    ]
  }
}'

# Check if index exists and delete it
echo "Checking for existing index..."
CHECK_RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" \
  -X GET \
  "$SEARCH_ENDPOINT/indexes/$INDEX_NAME?api-version=2024-07-01" \
  -H "$AUTH_HEADER")

if [ "$CHECK_RESPONSE" = "200" ]; then
  echo "Index exists. Deleting..."
  curl -X DELETE \
    "$SEARCH_ENDPOINT/indexes/$INDEX_NAME?api-version=2024-07-01" \
    -H "$AUTH_HEADER"
  echo "Deleted existing index. Waiting..."
  sleep 2
fi

# Create the index
echo "Creating index..."
RESPONSE=$(curl -s -w "\n%{http_code}" \
  -X POST \
  "$SEARCH_ENDPOINT/indexes?api-version=2024-07-01" \
  -H "Content-Type: application/json" \
  -H "$AUTH_HEADER" \
  -d "$INDEX_SCHEMA")

HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
BODY=$(echo "$RESPONSE" | sed '$d')

if [ "$HTTP_CODE" = "201" ]; then
  echo "✓ Index created successfully!"
  echo "Index Name: $INDEX_NAME"
  echo "Fields: 12"
  echo "Vector Fields: 2 (contentVector, summaryVector)"
  echo ""
  echo "You can now upload documents to be indexed."
else
  echo "✗ Error creating index (HTTP $HTTP_CODE):"
  echo "$BODY"
  exit 1
fi
