@description('Flex Consumption plan + Function App (system-assigned MI) with keyless app settings.')
param location string
param planName string
param functionAppName string
param deploymentStorageAccountName string
param deploymentContainerName string = 'app-package'
param appInsightsConnectionString string

// Endpoints / names injected as app settings (keyless: no keys)
param cosmosEndpoint string
param cosmosDatabaseName string
param openAiEndpoint string
param chatDeploymentName string
param embeddingDeploymentName string
param searchEndpoint string
param searchIndexName string
param appStorageAccountName string
param jwtSecretUri string
param adminPasswordUri string
param adminEmail string
param allowedCorsOrigin string

resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: planName
  location: location
  sku: { name: 'FC1', tier: 'FlexConsumption' }
  kind: 'functionapp'
  properties: { reserved: true }
}

resource deployStorage 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: deploymentStorageAccountName
}

resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp,linux'
  identity: { type: 'SystemAssigned' }
  properties: {
    serverFarmId: plan.id
    // Belt-and-suspenders transport hardening (Flex Consumption defaults to HTTPS, set explicitly).
    httpsOnly: true
    functionAppConfig: {
      deployment: {
        storage: {
          type: 'blobContainer'
          value: '${deployStorage.properties.primaryEndpoints.blob}${deploymentContainerName}'
          authentication: { type: 'SystemAssignedIdentity' }
        }
      }
      scaleAndConcurrency: { maximumInstanceCount: 40, instanceMemoryMB: 2048 }
      runtime: { name: 'dotnet-isolated', version: '10.0' }
    }
    siteConfig: {
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      cors: { allowedOrigins: [ allowedCorsOrigin, 'http://localhost:5173' ] }
      appSettings: [
        { name: 'AzureWebJobsStorage__accountName', value: appStorageAccountName }
        { name: 'AzureWebJobsStorage__blobServiceUri', value: 'https://${appStorageAccountName}.blob.core.windows.net' }
        { name: 'AzureWebJobsStorage__queueServiceUri', value: 'https://${appStorageAccountName}.queue.core.windows.net' }
        { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appInsightsConnectionString }
        { name: 'CosmosDb__Endpoint', value: cosmosEndpoint }
        { name: 'CosmosDb__DatabaseName', value: cosmosDatabaseName }
        { name: 'AzureAI__Endpoint', value: openAiEndpoint }
        { name: 'AzureAI__DeploymentName', value: chatDeploymentName }
        { name: 'AzureAI__AvailableModels', value: chatDeploymentName }
        { name: 'AzureAI__EmbeddingDeployment', value: embeddingDeploymentName }
        { name: 'AzureSearch__Endpoint', value: searchEndpoint }
        { name: 'AzureSearch__IndexName', value: searchIndexName }
        { name: 'AzureStorage__AccountName', value: appStorageAccountName }
        { name: 'AzureStorage__ContainerName', value: 'ai-chat' }
        { name: 'AzureStorage__ImageFolder', value: 'images' }
        { name: 'AzureStorage__DocumentFolder', value: 'documents' }
        { name: 'AzureStorage__QueueName', value: 'document-processing-queue' }
        { name: 'DocumentProcessing__ChunkSize', value: '800' }
        { name: 'DocumentProcessing__ChunkOverlap', value: '200' }
        { name: 'DocumentProcessing__MaxFileSize', value: '10485760' }
        { name: 'RAG__MinRelevanceScore', value: '0.7' }
        { name: 'RAG__MaxResults', value: '3' }
        { name: 'RAG__SemanticWeight', value: '0.7' }
        { name: 'Jwt__SecretKey', value: '@Microsoft.KeyVault(SecretUri=${jwtSecretUri})' }
        { name: 'Jwt__Issuer', value: 'AIChatBot' }
        { name: 'Jwt__Audience', value: 'AIChatBot' }
        { name: 'Jwt__ExpirationMinutes', value: '1440' }
        { name: 'Admin__Email', value: adminEmail }
        { name: 'Admin__Password', value: '@Microsoft.KeyVault(SecretUri=${adminPasswordUri})' }
        { name: 'Admin__Name', value: 'Administrator' }
      ]
    }
  }
}

output principalId string = functionApp.identity.principalId
output defaultHostName string = functionApp.properties.defaultHostName
output name string = functionApp.name
