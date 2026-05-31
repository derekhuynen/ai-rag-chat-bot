@description('Key Vault holding the only real secrets: JWT signing key + admin password.')
param location string
param keyVaultName string
@secure()
param jwtSecret string
@secure()
param adminPassword string
@description('Enable Key Vault purge protection. Recommended (true) for anything beyond a throwaway demo. Left false by default to stay teardown-friendly: when true, a soft-deleted vault cannot be purged early, so the same vault name cannot be reused immediately after `az group delete`.')
param enablePurgeProtection bool = false

resource vault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  properties: {
    sku: { family: 'A', name: 'standard' }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    // Purge protection is opt-in; see param description for the teardown trade-off.
    // ARM only accepts `true` (it is irreversible once enabled);when opted out we omit
    // the property entirely rather than sending `false`, which the API rejects.
    enablePurgeProtection: enablePurgeProtection ? true : null
  }
}

resource jwtSecretResource 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: vault
  name: 'Jwt--SecretKey'
  properties: { value: jwtSecret }
}

resource adminPasswordResource 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: vault
  name: 'Admin--Password'
  properties: { value: adminPassword }
}

output vaultUri string = vault.properties.vaultUri
output id string = vault.id
output name string = vault.name
output jwtSecretUri string = jwtSecretResource.properties.secretUri
output adminPasswordUri string = adminPasswordResource.properties.secretUri
