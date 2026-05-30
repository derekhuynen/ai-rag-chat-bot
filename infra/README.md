# Infrastructure

Cheap, keyless, one-command Azure environment for AI_RAG_Chat_Bot.

The whole stack lives in a single resource group, so teardown is one command. All
app-to-Azure auth is keyless (Managed Identity in the cloud, your `az login` locally); the
only secrets are the JWT signing key and the admin password, both held in Key Vault.

## What gets deployed

| Resource | Tier | Notes |
|----------|------|-------|
| Function App (API) | Flex Consumption (`FC1`), .NET 10 isolated | scale-to-zero, system-assigned identity |
| SPA host | Storage static website (`$web`) | the built Vite frontend |
| Cosmos DB | Serverless | database `ai_chat`, containers `Users`/`Conversations`/`Documents` |
| Azure AI Search | Free or Basic | index `ai-chat-documents` (created post-deploy) |
| Azure OpenAI | S0 | `gpt-4.1` + `text-embedding-3-small` deployments |
| Storage (app) | Standard LRS | blob container `ai-chat` + `document-processing-queue` |
| Key Vault | Standard | JWT secret + admin password only |
| Observability | Log Analytics + App Insights | low daily cap |

## Prerequisites

- **Azure CLI (`az`)** with the Bicep extension (`az bicep version`; install via `az bicep install`).
- **.NET 10 SDK** (the backend targets `net10.0`).
- **Azure Functions Core Tools v4** (`func`) — used to publish the backend to a Flex Consumption app.
- **Node 20+** (to build the SPA).
- An **Azure subscription** where you can create resource groups and assign roles.
- `az login` completed.

On a clean Windows machine the tooling installs via winget:
```powershell
winget install Microsoft.DotNet.SDK.10
winget install Microsoft.Azure.FunctionsCoreTools
# az + node assumed already present; az bicep install if needed
```
Open a fresh shell afterward so the updated PATH is picked up.

> **PowerShell note:** the scripts are written for Windows PowerShell 5.1. They pass
> `--only-show-errors` to every `az` call and relax `$ErrorActionPreference` for the
> publish/upload steps, because 5.1 treats a native command's stderr (including Bicep
> linter warnings) as a terminating error. If you port these to `pwsh` 7+, that handling
> is still safe.

## Deploy (one command)

```powershell
cd infra
./deploy.ps1 -Location eastus2 -ResourceGroupName rg-ragchat -SearchSku basic -SearchLocation westus2
```

This provisions everything, publishes the API (`func azure functionapp publish`), builds and
uploads the SPA to the static website, grants your signed-in user the data-plane roles (so you
can also run the app locally), and prints the **SPA URL** and **API base URL**.

Parameters:
- `-Location` — region for the whole stack (default `eastus2`).
- `-SearchSku` — `free` or `basic` (default `free`). See the Search capacity note below.
- `-SearchLocation` — region for **Azure AI Search only** (defaults to `-Location`). Use this
  when your main region is out of Search capacity; the app reaches Search over HTTPS, so it does
  not need to sit in the same region as everything else.
- `-AdminEmail` — first admin account (default `admin@example.com`).
- `-JwtSecret` / `-AdminPassword` — optional `securestring`s. If omitted, a random JWT is
  generated and the admin password defaults to `ChangeThisAdminPassword123!`.

### This is the exact, verified replication
The first real deploy of this stack used:
```powershell
./deploy.ps1 -Location eastus2 -ResourceGroupName rg-ragchat -SearchSku basic -SearchLocation westus2
```
Everything (OpenAI, Cosmos, storage, Function App, etc.) provisioned in `eastus2`; only Search
went to `westus2` because `eastus2` was out of Search capacity at the time (see below).

## After the deploy: create the search index

The RAG index is created once, keyless, against the deployed Search service:
```powershell
cd ../backend/Setup
./create-ai-search-index.ps1 -SearchEndpoint "https://<searchServiceName>.search.windows.net"
```
The script uses an AAD bearer token from your `az login` (no admin key needed, because the
service is deployed with local auth disabled). The deploy script prints the Search endpoint at
the end. Until the index exists, document upload and RAG answers will not work.

## Smoke test (proves it actually works)

1. Open the **SPA URL** in a browser.
2. Log in as the admin (`-AdminEmail` / the password above).
3. Send a chat message → a streamed reply confirms Cosmos + OpenAI chat (keyless).
4. Paste or upload an image in chat → it renders, confirming blob upload + user-delegation SAS.
5. As admin, upload a `.txt`/`.md` document, wait for processing, then ask a question it answers →
   a reply with a citation confirms embeddings + AI Search end to end.

## Tear down (one command)

```powershell
cd infra
./teardown.ps1 -ResourceGroupName rg-ragchat
```
Deletes the entire resource group. Key Vault keeps a 7-day soft-delete; to reuse the same vault
name immediately, `az keyvault purge --name <vaultName>`. If you placed Search in a different
region, it lives in the same resource group and is removed by the same teardown.

## Capacity, region, and SKU gotchas

- **Azure AI Search capacity (`InsufficientResourcesAvailable`).** A region can be out of Search
  capacity (this hit `eastus2` for both Free and Basic). Two levers:
  - `-SearchSku basic` gives dedicated capacity (Free uses a small shared pool that is often
    exhausted). Basic is ~$75/month if left running, but only pennies for a demo you tear down.
  - `-SearchLocation <region>` places Search in a region that has capacity, leaving the rest of
    the stack in your main region.
  - To find a region with capacity quickly, probe directly (it either succeeds or fails fast),
    then delete the probe:
    ```powershell
    az search service create --name probe --resource-group rg-ragchat --sku basic `
      --location westus2 --partition-count 1 --replica-count 1 --only-show-errors
    az search service delete --name probe --resource-group rg-ragchat --yes --only-show-errors
    ```
- **Free AI Search is limited to 1 per subscription.** If yours is taken, use `-SearchSku basic`.
- **Azure OpenAI model availability/quota varies by region.** Confirm the models exist in your
  region before deploying:
  ```powershell
  az cognitiveservices model list --location eastus2 `
    --query "[?contains(['gpt-4.1','text-embedding-3-small'], model.name)].{name:model.name, version:model.version}" -o table
  ```
  Adjust `chatModelVersion`/`embeddingModelVersion` in `modules/openai.bicep` if a version is gone.
- **Flex Consumption (`FC1`) is not in every region.** Confirm with
  `az functionapp list-flexconsumption-locations`. `eastus2` is supported. If your region is not,
  either move `-Location`, or edit `modules/functionapp.bicep` to use `sku: { name: 'Y1', tier:
  'Dynamic' }`, drop `functionAppConfig`, and add `FUNCTIONS_EXTENSION_VERSION=~4` +
  `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated`. Note: **.NET 10 cannot run on Linux Consumption (Y1
  Linux)** — only on Flex, so a Y1 fallback must use a Windows plan.
- **`func` publish needs `--dotnet-isolated`.** There is no committed `local.settings.json`, so
  `func azure functionapp publish` cannot auto-detect the worker runtime and errors with
  `Worker runtime cannot be 'None'`. The deploy script passes `--dotnet-isolated`; if you publish
  by hand, include that flag.
- **SPA upload uses the storage account key, not `--auth-mode login`.** Data-plane RBAC takes
  several minutes to propagate, which makes a login-auth `upload-batch` fail right after a fresh
  role assignment. The web storage only hosts public static files, so the deploy script uploads
  with the account key; the app runtime stays fully keyless.

## CI/CD (GitHub Actions, OIDC, one-time setup)

The deploy workflow authenticates with a federated credential (no stored secret). Run once:
```bash
az ad app create --display-name ragchat-gha
APP_ID=$(az ad app list --display-name ragchat-gha --query "[0].appId" -o tsv)
az ad sp create --id "$APP_ID"
SUB=$(az account show --query id -o tsv)
RG="/subscriptions/$SUB/resourceGroups/rg-ragchat"
# Least privilege: the deploy workflow only publishes function code and uploads the SPA, so
# scope the OIDC principal to just what it touches:
FUNC=$(az functionapp list -g rg-ragchat --query "[0].name" -o tsv)
WEB=$(az storage account list -g rg-ragchat --query "[?contains(name,'web')].name | [0]" -o tsv)
az role assignment create --assignee "$APP_ID" --role "Website Contributor" \
  --scope "$RG/providers/Microsoft.Web/sites/$FUNC"
az role assignment create --assignee "$APP_ID" --role "Storage Blob Data Contributor" \
  --scope "$RG/providers/Microsoft.Storage/storageAccounts/$WEB"
# Simpler-but-broader fallback (grants more than needed): RG-wide Contributor instead of the two above:
#   az role assignment create --assignee "$APP_ID" --role Contributor --scope "$RG"
# Federated credential for pushes to main:
az ad app federated-credential create --id "$APP_ID" --parameters '{
  "name": "gha-main",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:derekhuynen/ai-rag-chat-bot:ref:refs/heads/main",
  "audiences": ["api://AzureADTokenExchange"]
}'
```
Then set repo secrets `AZURE_CLIENT_ID` (=$APP_ID), `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`.
The *Storage Blob Data Contributor* assignment above is what lets the `--auth-mode login` SPA
upload succeed (the deploy workflow retries it while the role propagates). The deploy workflow
ships code only; provision the environment first with `deploy.ps1`.
