# Azure AI Search Index Creation Script
# This script creates the ai-chat-documents index for RAG functionality

param(
    # Example: "https://YOUR-SEARCH-SERVICE.search.windows.net"
    [string]$SearchEndpoint = "",
    # IMPORTANT: Pass your Azure AI Search admin key as a parameter or via environment variable.
    # Do NOT hard-code real keys in this script.
    [string]$ApiKey = "",
    [string]$IndexName = "ai-chat-documents"
)

$ErrorActionPreference = "Stop"

Write-Host "Creating Azure AI Search Index: $IndexName" -ForegroundColor Cyan
Write-Host "Endpoint: $SearchEndpoint" -ForegroundColor Gray

# Index schema
$indexSchema = @{
    name         = $IndexName
    fields       = @(
        @{
            name       = "id"
            type       = "Edm.String"
            key        = $true
            searchable = $false
            filterable = $false
            sortable   = $false
            facetable  = $false
        },
        @{
            name       = "documentId"
            type       = "Edm.String"
            searchable = $false
            filterable = $true
            sortable   = $false
            facetable  = $false
        },
        @{
            name       = "fileName"
            type       = "Edm.String"
            searchable = $true
            filterable = $true
            sortable   = $false
            facetable  = $true
        },
        @{
            name       = "content"
            type       = "Edm.String"
            searchable = $true
            filterable = $false
            sortable   = $false
            facetable  = $false
            analyzer   = "en.microsoft"
        },
        @{
            name       = "summary"
            type       = "Edm.String"
            searchable = $true
            filterable = $false
            sortable   = $false
            facetable  = $false
            analyzer   = "en.microsoft"
        },
        @{
            name                = "contentVector"
            type                = "Collection(Edm.Single)"
            searchable          = $true
            filterable          = $false
            sortable            = $false
            facetable           = $false
            dimensions          = 1536
            vectorSearchProfile = "default-vector-profile"
        },
        @{
            name                = "summaryVector"
            type                = "Collection(Edm.Single)"
            searchable          = $true
            filterable          = $false
            sortable            = $false
            facetable           = $false
            dimensions          = 1536
            vectorSearchProfile = "default-vector-profile"
        },
        @{
            name       = "chunkIndex"
            type       = "Edm.Int32"
            searchable = $false
            filterable = $true
            sortable   = $true
            facetable  = $false
        },
        @{
            name       = "page"
            type       = "Edm.Int32"
            searchable = $false
            filterable = $true
            sortable   = $false
            facetable  = $false
        },
        @{
            name       = "uploadedBy"
            type       = "Edm.String"
            searchable = $false
            filterable = $true
            sortable   = $false
            facetable  = $false
        },
        @{
            name       = "uploadedAt"
            type       = "Edm.DateTimeOffset"
            searchable = $false
            filterable = $true
            sortable   = $true
            facetable  = $false
        },
        @{
            name       = "blobUrl"
            type       = "Edm.String"
            searchable = $false
            filterable = $false
            sortable   = $false
            facetable  = $false
        }
    )
    vectorSearch = @{
        algorithms = @(
            @{
                name           = "hnsw-config"
                kind           = "hnsw"
                hnswParameters = @{
                    metric         = "cosine"
                    m              = 4
                    efConstruction = 400
                    efSearch       = 500
                }
            }
        )
        profiles   = @(
            @{
                name      = "default-vector-profile"
                algorithm = "hnsw-config"
            }
        )
    }
} | ConvertTo-Json -Depth 10

# API endpoint
$uri = "$SearchEndpoint/indexes?api-version=2024-07-01"

# Auth: prefer keyless AAD (Search is deployed with local auth disabled).
# Falls back to api-key only if one was explicitly passed.
if ([string]::IsNullOrWhiteSpace($ApiKey)) {
    $token = az account get-access-token --resource https://search.azure.com --query accessToken -o tsv
    if ([string]::IsNullOrWhiteSpace($token)) { throw "Could not get an AAD token. Run 'az login' first." }
    $headers = @{
        "Content-Type"  = "application/json"
        "Authorization" = "Bearer $token"
    }
}
else {
    $headers = @{
        "Content-Type" = "application/json"
        "api-key"      = $ApiKey
    }
}

try {
    # Check if index already exists
    $checkUri = "$SearchEndpoint/indexes/$IndexName`?api-version=2024-07-01"
    try {
        $existing = Invoke-RestMethod -Uri $checkUri -Method Get -Headers $headers -ErrorAction SilentlyContinue
        Write-Host "Index '$IndexName' already exists. Deleting..." -ForegroundColor Yellow
        Invoke-RestMethod -Uri $checkUri -Method Delete -Headers $headers | Out-Null
        Write-Host "Existing index deleted." -ForegroundColor Green
        Start-Sleep -Seconds 2
    }
    catch {
        Write-Host "No existing index found. Creating new..." -ForegroundColor Gray
    }

    # Create the index
    Write-Host "Creating index..." -ForegroundColor Cyan
    $response = Invoke-RestMethod -Uri $uri -Method Post -Headers $headers -Body $indexSchema
    
    Write-Host "Index created successfully!" -ForegroundColor Green
    Write-Host "Index Name: $($response.name)" -ForegroundColor White
    Write-Host "Fields: $($response.fields.Count)" -ForegroundColor White
    Write-Host "Vector Fields: 2 (contentVector, summaryVector)" -ForegroundColor White
    Write-Host ""
    Write-Host "You can now upload documents to be indexed." -ForegroundColor Cyan
}
catch {
    Write-Host "Error creating index:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    if ($_.ErrorDetails.Message) {
        Write-Host $_.ErrorDetails.Message -ForegroundColor Red
    }
    exit 1
}
