targetScope = 'resourceGroup'

@description('Azure region for all shared resources.')
param location string = resourceGroup().location

@description('Short environment name used in resource names and tags. Registry names strip hyphens.')
@minLength(2)
@maxLength(20)
param environmentName string = 'dev'

@description('SQL administrator login. The password must come from a pipeline secret.')
param sqlAdminLogin string

@secure()
@description('SQL administrator password. Supply it from a pipeline secret or Key Vault.')
param sqlAdminPassword string

@description('Optional cost and ownership tags.')
param tags object = {}

var suffix = uniqueString(resourceGroup().id)
var logAnalyticsName = 'log-${environmentName}-${suffix}'
var containerEnvironmentName = 'cae-${environmentName}-${suffix}'
var registryName = toLower('acr${replace(environmentName, '-', '')}${suffix}')
var sqlServerName = 'sql-${environmentName}-${suffix}'

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: logAnalyticsName
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

resource registry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: registryName
  location: location
  tags: tags
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: true
  }
}

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: sqlServerName
  location: location
  tags: tags
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    minimalTlsVersion: '1.2'
  }
}

resource allowAzureServices 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource productDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: 'productdb'
  location: location
  tags: tags
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
  }
}

resource userDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: 'userdb'
  location: location
  tags: tags
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
  }
}

resource orderDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: 'orderdb'
  location: location
  tags: tags
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
  }
}

resource containerEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: containerEnvironmentName
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
  }
}

output registryName string = registry.name
output registryLoginServer string = registry.properties.loginServer
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output containerEnvironmentId string = containerEnvironment.id
output containerEnvironmentDefaultDomain string = containerEnvironment.properties.defaultDomain
output productDatabaseName string = productDatabase.name
output userDatabaseName string = userDatabase.name
output orderDatabaseName string = orderDatabase.name
