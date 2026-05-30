@description('Azure OpenAI (Cognitive Services) account + chat and embedding deployments.')
param location string
param accountName string
param chatDeploymentName string = 'gpt-4.1'
param chatModelName string = 'gpt-4.1'
param chatModelVersion string = '2025-04-14'
param embeddingDeploymentName string = 'text-embedding-3-small'
param embeddingModelName string = 'text-embedding-3-small'
param embeddingModelVersion string = '1'
param chatCapacity int = 10
param embeddingCapacity int = 10

resource openai 'Microsoft.CognitiveServices/accounts@2024-10-01' = {
  name: accountName
  location: location
  kind: 'OpenAI'
  sku: { name: 'S0' }
  properties: {
    customSubDomainName: accountName
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: true
  }
}

resource chat 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  parent: openai
  name: chatDeploymentName
  sku: { name: 'Standard', capacity: chatCapacity }
  properties: {
    model: { format: 'OpenAI', name: chatModelName, version: chatModelVersion }
  }
}

resource embedding 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  parent: openai
  name: embeddingDeploymentName
  sku: { name: 'Standard', capacity: embeddingCapacity }
  // Sequential dependency: a Cognitive Services account allows one deployment op at a time.
  dependsOn: [ chat ]
  properties: {
    model: { format: 'OpenAI', name: embeddingModelName, version: embeddingModelVersion }
  }
}

output endpoint string = openai.properties.endpoint
output id string = openai.id
output name string = openai.name
