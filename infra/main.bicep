targetScope = 'resourceGroup'

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('Short environment name used in resource names and tags.')
@minLength(2)
@maxLength(20)
param environmentName string = 'dev'

@description('SQL administrator login.')
param sqlAdminLogin string

@secure()
@description('SQL administrator password. Supply it from a pipeline secret or Key Vault.')
param sqlAdminPassword string

@secure()
@description('Shared JWT signing secret. Must be at least 32 characters.')
param jwtSecret string

@description('JWT issuer. Must match across the gateway and every service.')
param jwtIssuer string = 'ECommerce'

@description('JWT audience. Must match across the gateway and every service.')
param jwtAudience string = 'ECommerce.Client'

@secure()
@description('Shared service-to-service API key for /api/internal endpoints.')
param internalApiKey string

@description('Microsoft Foundry endpoint. Leave empty to use the deterministic fallbacks.')
param foundryEndpoint string = ''

@secure()
@description('Microsoft Foundry API key. Leave empty when Foundry is not configured.')
param foundryApiKey string = ''

@description('Microsoft Foundry chat deployment name.')
param foundryChatDeployment string = ''

@description('Microsoft Foundry mode: auto, enabled or disabled.')
@allowed([
  'auto'
  'enabled'
  'disabled'
])
param foundryMode string = 'auto'

@description('Optional administrator email created in the User Service.')
param adminEmail string = ''

@secure()
@description('Optional administrator password. Leave empty to skip admin seeding.')
param adminPassword string = ''

@description('Browser origin allowed by CORS. Defaults to the frontend Container App URL.')
param frontendOrigin string = ''

@description('Container image tag pushed by the pipeline.')
param imageTag string = 'latest'

@description('Optional cost and ownership tags.')
param tags object = {}

module shared 'shared.bicep' = {
  name: 'shared-${environmentName}'
  params: {
    location: location
    environmentName: environmentName
    sqlAdminLogin: sqlAdminLogin
    sqlAdminPassword: sqlAdminPassword
    tags: tags
  }
}

var resolvedFrontendOrigin = empty(frontendOrigin)
  ? 'https://frontend-${environmentName}.${shared.outputs.containerEnvironmentDefaultDomain}'
  : frontendOrigin

module apps 'apps.bicep' = {
  name: 'apps-${environmentName}'
  params: {
    location: location
    environmentName: environmentName
    containerEnvironmentId: shared.outputs.containerEnvironmentId
    registryName: shared.outputs.registryName
    registryLoginServer: shared.outputs.registryLoginServer
    sqlServerFqdn: shared.outputs.sqlServerFqdn
    sqlAdminLogin: sqlAdminLogin
    sqlAdminPassword: sqlAdminPassword
    productDatabaseName: shared.outputs.productDatabaseName
    userDatabaseName: shared.outputs.userDatabaseName
    orderDatabaseName: shared.outputs.orderDatabaseName
    jwtSecret: jwtSecret
    jwtIssuer: jwtIssuer
    jwtAudience: jwtAudience
    internalApiKey: internalApiKey
    foundryEndpoint: foundryEndpoint
    foundryApiKey: foundryApiKey
    foundryChatDeployment: foundryChatDeployment
    foundryMode: foundryMode
    frontendOrigin: resolvedFrontendOrigin
    adminEmail: adminEmail
    adminPassword: adminPassword
    imageTag: imageTag
    tags: tags
  }
}

output registryLoginServer string = shared.outputs.registryLoginServer
output sqlServerFqdn string = shared.outputs.sqlServerFqdn
output containerEnvironmentDefaultDomain string = shared.outputs.containerEnvironmentDefaultDomain
output frontendUrl string = apps.outputs.frontendUrl
output gatewayUrl string = apps.outputs.gatewayUrl
