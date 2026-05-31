[CmdletBinding()]
param(
  [string]$Location = 'eastus2',
  [string]$ResourceGroupName = 'rg-ragchat',
  [string]$BaseName = 'ragchat',
  [string]$AdminEmail = 'admin@example.com',
  [ValidateSet('free','basic')][string]$SearchSku = 'free',
  [string]$SearchLocation = '',
  [string]$DevPrincipalId = '',
  [switch]$GrantDevAccess,
  [securestring]$JwtSecret,
  [securestring]$AdminPassword
)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent

Write-Host "Checking Azure login..." -ForegroundColor Cyan
az account show --only-show-errors 1>$null
if ($LASTEXITCODE -ne 0) { throw "Not logged in. Run 'az login' first." }

# Generate secrets if not supplied
function ConvertFrom-SecureToPlain([securestring]$s) {
  if (-not $s) { return $null }
  [System.Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($s))
}
$jwt = ConvertFrom-SecureToPlain $JwtSecret
if (-not $jwt) { $jwt = [Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Maximum 256 })) }
$admin = ConvertFrom-SecureToPlain $AdminPassword
$adminGenerated = $false
if (-not $admin) {
  $admin = [Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Maximum 256 }))
  $adminGenerated = $true
}

# Search has its own region (some regions are out of Search capacity); default to $Location
if ([string]::IsNullOrWhiteSpace($SearchLocation)) { $SearchLocation = $Location }

# Dev principal data-plane + Key Vault access is opt-in: only resolve/assign it when the
# operator explicitly passes -DevPrincipalId or the -GrantDevAccess switch. Otherwise leave
# it empty so we never silently grant the signed-in user broad access.
if ([string]::IsNullOrWhiteSpace($DevPrincipalId) -and $GrantDevAccess) {
  $DevPrincipalId = az ad signed-in-user show --query id -o tsv --only-show-errors
}

Write-Host "Provisioning infrastructure..." -ForegroundColor Cyan
$deployOutput = az deployment sub create `
  --name "ragchat-$(Get-Date -Format yyyyMMddHHmmss)" `
  --location $Location `
  --template-file "$PSScriptRoot/main.bicep" `
  --parameters "$PSScriptRoot/main.parameters.json" `
  --parameters location=$Location resourceGroupName=$ResourceGroupName baseName=$BaseName `
               adminEmail=$AdminEmail searchSku=$SearchSku searchLocation=$SearchLocation `
               devPrincipalId=$DevPrincipalId `
               jwtSecret=$jwt adminPassword=$admin `
  --only-show-errors `
  --query properties.outputs -o json | ConvertFrom-Json
if ($LASTEXITCODE -ne 0) { throw "Infrastructure deployment failed." }

# az/func/npm write progress to stderr; under 'Stop' PowerShell 5.1 treats native
# stderr as terminating. Switch to 'Continue' for the operational tail, which is
# guarded by explicit exit-code / Test-Path checks (and `throw` still terminates).
$ErrorActionPreference = 'Continue'

$functionAppName = $deployOutput.functionAppName.value
$apiBaseUrl      = $deployOutput.apiBaseUrl.value
$webStorage      = $deployOutput.webStorageAccountName.value
$webEndpoint     = $deployOutput.webEndpoint.value

# --- Build + deploy the backend (Flex Consumption uses func/OneDeploy, not Kudu config-zip) ---
# (The 'app-package' deployment container is provisioned by Bicep, so no need to create it here.)
Write-Host "Publishing backend..." -ForegroundColor Cyan
Push-Location "$repoRoot/backend"
func azure functionapp publish $functionAppName --dotnet-isolated
$funcExit = $LASTEXITCODE
Pop-Location
if ($funcExit -ne 0) { throw "Backend deploy failed (func azure functionapp publish)." }

# --- Build + deploy the frontend SPA to the static website ---
Write-Host "Building frontend..." -ForegroundColor Cyan
Push-Location "$repoRoot/frontend"
"VITE_API_BASE_URL=$apiBaseUrl" | Out-File -FilePath ".env.production" -Encoding utf8
npm ci
npm run build
Pop-Location
if (-not (Test-Path "$repoRoot/frontend/dist")) { throw "Frontend build produced no dist/." }

Write-Host "Enabling static website + uploading SPA..." -ForegroundColor Cyan
# Use the storage key for the static-site upload: it is reliable (no RBAC data-plane
# propagation delay) and the web storage only hosts public static files. The app runtime
# stays fully keyless; this key is used transiently by the deploying identity for upload.
$webKey = az storage account keys list -n $webStorage -g $ResourceGroupName --query "[0].value" -o tsv --only-show-errors
az storage blob service-properties update --account-name $webStorage --account-key $webKey --static-website `
  --index-document index.html --404-document index.html --only-show-errors
az storage blob upload-batch --account-name $webStorage --account-key $webKey `
  --destination '$web' --source "$repoRoot/frontend/dist" --overwrite --only-show-errors

Write-Host ""
Write-Host "=== Deployment complete ===" -ForegroundColor Green
Write-Host "Frontend (SPA): $webEndpoint"
Write-Host "API base URL:   $apiBaseUrl"
if ($adminGenerated) {
  Write-Host ""
  Write-Host "Generated admin password (save this now - it will not be shown again):" -ForegroundColor Yellow
  Write-Host "  Admin email:    $AdminEmail" -ForegroundColor Yellow
  Write-Host "  Admin password: $admin" -ForegroundColor Yellow
}
Write-Host ""
Write-Host "Next: create the search index by running backend/Setup/create-ai-search-index.ps1 against $($deployOutput.searchEndpoint.value) (keyless: use 'az login')." -ForegroundColor Cyan
