targetScope = 'subscription'

@description('Azure region for all resources.')
param location string = 'eastus2'
@description('Resource group to create/use.')
param resourceGroupName string = 'rg-ragchat'
@description('Short base name for resource naming.')
param baseName string = 'ragchat'
@secure()
param jwtSecret string
@secure()
param adminPassword string
param adminEmail string = 'admin@example.com'
@description('Optional developer objectId for local keyless access.')
param devPrincipalId string = ''
@allowed([ 'free', 'basic' ])
param searchSku string = 'free'
@description('Region for Azure AI Search (defaults to location; override when a region lacks Search capacity).')
param searchLocation string = location
@description('Enable Key Vault purge protection. Recommended (true) for anything beyond a throwaway demo; false (default) keeps teardown friendly (vault name reusable right after `az group delete`).')
param enableKeyVaultPurgeProtection bool = false

resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: resourceGroupName
  location: location
}

module resources 'resources.bicep' = {
  name: 'resources'
  scope: rg
  params: {
    location: location
    baseName: baseName
    jwtSecret: jwtSecret
    adminPassword: adminPassword
    adminEmail: adminEmail
    devPrincipalId: devPrincipalId
    searchSku: searchSku
    searchLocation: searchLocation
    enableKeyVaultPurgeProtection: enableKeyVaultPurgeProtection
  }
}

output resourceGroupName string = rg.name
output functionAppName string = resources.outputs.functionAppName
output apiBaseUrl string = resources.outputs.apiBaseUrl
output webStorageAccountName string = resources.outputs.webStorageAccountName
output webEndpoint string = resources.outputs.webEndpoint
output searchEndpoint string = resources.outputs.searchEndpoint
output appStorageAccountName string = resources.outputs.appStorageAccountName
