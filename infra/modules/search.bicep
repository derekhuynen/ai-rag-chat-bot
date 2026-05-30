@description('Azure AI Search service. Free tier is limited to 1 per subscription.')
param location string
param searchServiceName string
@allowed([ 'free', 'basic' ])
param searchSku string = 'free'

resource search 'Microsoft.Search/searchServices@2024-06-01-preview' = {
  name: searchServiceName
  location: location
  sku: { name: searchSku }
  properties: {
    replicaCount: 1
    partitionCount: 1
    hostingMode: 'default'
    authOptions: null
    disableLocalAuth: true
  }
  identity: { type: 'SystemAssigned' }
}

output endpoint string = 'https://${search.name}.search.windows.net'
output id string = search.id
output name string = search.name
