@description('Storage account that hosts the SPA as a static website ($web).')
param location string
param webStorageAccountName string

resource webStorage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: webStorageAccountName
  location: location
  sku: { name: 'Standard_LRS' }
  kind: 'StorageV2'
  properties: {
    // The static-website ($web) endpoint serves content without account-level anonymous
    // blob access, so keep this disabled for least exposure.
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
  }
}

output accountName string = webStorage.name
output webEndpoint string = webStorage.properties.primaryEndpoints.web
