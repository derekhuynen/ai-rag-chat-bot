@description('Data-plane RBAC for the Function App MI and an optional developer principal.')
param functionPrincipalId string
@description('Optional objectId of a developer to grant local access; empty to skip.')
param devPrincipalId string = ''
param appStorageAccountName string
param cosmosAccountName string
param searchServiceName string
param openAiAccountName string
param keyVaultName string

// Built-in role definition GUIDs
var roles = {
  blobContributor: 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
  queueContributor: '974c5e8b-45b9-4653-ba55-5f855dd0fb88'
  searchIndexDataContributor: '8ebe5a00-799e-43f5-93ac-243d3dce84a7'
  searchServiceContributor: '7ca78c08-252a-4471-8644-bb5ff32d4ba0'
  openAiUser: '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd'
  keyVaultSecretsUser: '4633458b-17de-408a-b874-0445c86b69e6'
}
var cosmosDataContributorRoleId = '00000000-0000-0000-0000-000000000002'

resource appStorage 'Microsoft.Storage/storageAccounts@2023-05-01' existing = { name: appStorageAccountName }
resource cosmos 'Microsoft.DocumentDB/databaseAccounts@2024-11-15' existing = { name: cosmosAccountName }
resource search 'Microsoft.Search/searchServices@2024-06-01-preview' existing = { name: searchServiceName }
resource openai 'Microsoft.CognitiveServices/accounts@2024-10-01' existing = { name: openAiAccountName }
resource vault 'Microsoft.KeyVault/vaults@2023-07-01' existing = { name: keyVaultName }

var principals = empty(devPrincipalId) ? [ functionPrincipalId ] : [ functionPrincipalId, devPrincipalId ]

resource blobRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = [for p in principals: {
  name: guid(appStorage.id, p, roles.blobContributor)
  scope: appStorage
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.blobContributor)
    principalId: p
  }
}]

resource queueRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = [for p in principals: {
  name: guid(appStorage.id, p, roles.queueContributor)
  scope: appStorage
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.queueContributor)
    principalId: p
  }
}]

resource searchIndexRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = [for p in principals: {
  name: guid(search.id, p, roles.searchIndexDataContributor)
  scope: search
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.searchIndexDataContributor)
    principalId: p
  }
}]

// Search Service Contributor (control plane) is needed to create the index, but the Function App
// runtime only needs data-plane access. Grant control plane to the dev principal only (when present),
// not to the Function App managed identity (least privilege).
resource searchServiceRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(devPrincipalId)) {
  name: guid(search.id, devPrincipalId, roles.searchServiceContributor)
  scope: search
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.searchServiceContributor)
    principalId: devPrincipalId
  }
}

resource openAiRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = [for p in principals: {
  name: guid(openai.id, p, roles.openAiUser)
  scope: openai
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.openAiUser)
    principalId: p
  }
}]

resource kvRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = [for p in principals: {
  name: guid(vault.id, p, roles.keyVaultSecretsUser)
  scope: vault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.keyVaultSecretsUser)
    principalId: p
  }
}]

// Cosmos data-plane role (SQL role assignment, not standard RBAC)
resource cosmosDataRole 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2024-11-15' = [for (p, i) in principals: {
  parent: cosmos
  name: guid(cosmos.id, p, cosmosDataContributorRoleId)
  properties: {
    roleDefinitionId: '${cosmos.id}/sqlRoleDefinitions/${cosmosDataContributorRoleId}'
    principalId: p
    scope: cosmos.id
  }
}]
