@description('Resource-group-scope orchestrator wiring all modules together.')
param location string
param baseName string
@secure()
param jwtSecret string
@secure()
param adminPassword string
param adminEmail string
param devPrincipalId string = ''
@allowed([ 'free', 'basic' ])
param searchSku string = 'free'
@description('Region for Azure AI Search (defaults to location; override when a region lacks Search capacity).')
param searchLocation string = location
@description('Enable Key Vault purge protection. Recommended (true) for anything beyond a throwaway demo; false keeps teardown friendly (vault name reusable right after `az group delete`).')
param enableKeyVaultPurgeProtection bool = false

var suffix = substring(uniqueString(resourceGroup().id), 0, 6)
var names = {
  appStorage: toLower('${baseName}st${suffix}')
  webStorage: toLower('${baseName}web${suffix}')
  cosmos: toLower('${baseName}-cosmos-${suffix}')
  search: toLower('${baseName}-search-${suffix}')
  openai: toLower('${baseName}-openai-${suffix}')
  keyvault: toLower('${baseName}kv${suffix}')
  workspace: '${baseName}-logs-${suffix}'
  appInsights: '${baseName}-ai-${suffix}'
  plan: '${baseName}-plan-${suffix}'
  functionApp: '${baseName}-func-${suffix}'
}

module storage 'modules/storage.bicep' = {
  name: 'storage'
  params: { location: location, storageAccountName: names.appStorage }
}

module web 'modules/web.bicep' = {
  name: 'web'
  params: { location: location, webStorageAccountName: names.webStorage }
}

module cosmos 'modules/cosmos.bicep' = {
  name: 'cosmos'
  params: { location: location, accountName: names.cosmos }
}

module search 'modules/search.bicep' = {
  name: 'search'
  params: { location: searchLocation, searchServiceName: names.search, searchSku: searchSku }
}

module openai 'modules/openai.bicep' = {
  name: 'openai'
  params: { location: location, accountName: names.openai }
}

module keyvault 'modules/keyvault.bicep' = {
  name: 'keyvault'
  params: {
    location: location
    keyVaultName: names.keyvault
    jwtSecret: jwtSecret
    adminPassword: adminPassword
    enablePurgeProtection: enableKeyVaultPurgeProtection
  }
}

module observability 'modules/observability.bicep' = {
  name: 'observability'
  params: { location: location, workspaceName: names.workspace, appInsightsName: names.appInsights }
}

module functionApp 'modules/functionapp.bicep' = {
  name: 'functionApp'
  params: {
    location: location
    planName: names.plan
    functionAppName: names.functionApp
    deploymentStorageAccountName: storage.outputs.accountName
    appInsightsConnectionString: observability.outputs.connectionString
    cosmosEndpoint: cosmos.outputs.endpoint
    cosmosDatabaseName: 'ai_chat'
    openAiEndpoint: openai.outputs.endpoint
    chatDeploymentName: 'gpt-4.1'
    embeddingDeploymentName: 'text-embedding-3-small'
    searchEndpoint: search.outputs.endpoint
    searchIndexName: 'ai-chat-documents'
    appStorageAccountName: storage.outputs.accountName
    jwtSecretUri: keyvault.outputs.jwtSecretUri
    adminPasswordUri: keyvault.outputs.adminPasswordUri
    adminEmail: adminEmail
    // Strip the trailing slash: the static-website endpoint ends with '/', but browsers send
    // the Origin header without one, and Functions CORS matches the origin string exactly.
    allowedCorsOrigin: substring(web.outputs.webEndpoint, 0, length(web.outputs.webEndpoint) - 1)
  }
}

module roles 'modules/roles.bicep' = {
  name: 'roles'
  params: {
    functionPrincipalId: functionApp.outputs.principalId
    devPrincipalId: devPrincipalId
    appStorageAccountName: storage.outputs.accountName
    cosmosAccountName: cosmos.outputs.accountName
    searchServiceName: search.outputs.name
    openAiAccountName: openai.outputs.name
    keyVaultName: keyvault.outputs.name
  }
}

output functionAppName string = functionApp.outputs.name
output functionHostName string = functionApp.outputs.defaultHostName
output apiBaseUrl string = 'https://${functionApp.outputs.defaultHostName}/api'
output webStorageAccountName string = web.outputs.accountName
output webEndpoint string = web.outputs.webEndpoint
output searchEndpoint string = search.outputs.endpoint
output appStorageAccountName string = storage.outputs.accountName
