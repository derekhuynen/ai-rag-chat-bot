[CmdletBinding()]
param(
  [string]$ResourceGroupName = 'rg-ragchat'
)
$ErrorActionPreference = 'Stop'

Write-Host "Checking Azure login..." -ForegroundColor Cyan
az account show 1>$null
if ($LASTEXITCODE -ne 0) { throw "Not logged in. Run 'az login' first." }

$exists = az group exists --name $ResourceGroupName
if ($exists -ne 'true') {
  Write-Host "Resource group '$ResourceGroupName' does not exist. Nothing to do." -ForegroundColor Yellow
  return
}

Write-Host "Deleting resource group '$ResourceGroupName' (async)..." -ForegroundColor Yellow
az group delete --name $ResourceGroupName --yes --no-wait
Write-Host "Delete requested. Note: Key Vault has 7-day soft-delete; purge with 'az keyvault purge' if you redeploy the same name." -ForegroundColor Cyan
