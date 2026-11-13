targetScope = 'resourceGroup'

@description('Azure region for the container apps.')
param location string = resourceGroup().location

@description('Short environment name used in resource names and tags.')
@minLength(2)
@maxLength(20)
param environmentName string = 'dev'

@description('Container Apps environment resource ID created by shared.bicep.')
param containerEnvironmentId string

@description('Name of the Azure Container Registry created by shared.bicep.')
param registryName string

@description('Login server of the Azure Container Registry, for example acrdev1234.azurecr.io.')
param registryLoginServer string

@description('Azure SQL server fully qualified domain name.')
param sqlServerFqdn string

@description('Azure SQL administrator login.')
param sqlAdminLogin string

@secure()
@description('Azure SQL administrator password. Supply it from a pipeline secret or Key Vault.')
param sqlAdminPassword string

@description('Name of the Product Service database.')
param productDatabaseName string = 'productdb'

@description('Name of the User Service database.')
param userDatabaseName string = 'userdb'

@description('Name of the Order Service database.')
param orderDatabaseName string = 'orderdb'

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

@description('Browser origin allowed by CORS, normally the frontend Container App URL.')
param frontendOrigin string

@description('Optional administrator email created in the User Service.')
param adminEmail string = ''

@secure()
@description('Optional administrator password. Leave empty to skip admin seeding.')
param adminPassword string = ''

@description('Container image tag pushed by the pipeline.')
param imageTag string = 'latest'

@description('Optional cost and ownership tags.')
param tags object = {}

var productAppName = 'product-${environmentName}'
var userAppName = 'user-${environmentName}'
var orderAppName = 'order-${environmentName}'
var gatewayAppName = 'gateway-${environmentName}'
var frontendAppName = 'frontend-${environmentName}'

var sqlConnectionTemplate = 'Server=tcp:${sqlServerFqdn},1433;Database={0};User ID=${sqlAdminLogin};Password=${sqlAdminPassword};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
var productConnectionString = format(sqlConnectionTemplate, productDatabaseName)
var userConnectionString = format(sqlConnectionTemplate, userDatabaseName)
var orderConnectionString = format(sqlConnectionTemplate, orderDatabaseName)

var productImage = '${registryLoginServer}/product-service:${imageTag}'
var userImage = '${registryLoginServer}/user-service:${imageTag}'
var orderImage = '${registryLoginServer}/order-service:${imageTag}'
var gatewayImage = '${registryLoginServer}/gateway:${imageTag}'
var frontendImage = '${registryLoginServer}/frontend:${imageTag}'

// The shared deployment enables the ACR admin account so the pipeline can push
// images. Reading the credentials at deployment time avoids passing them as
// parameters, which would leave them in the deployment history.
resource registry 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
  name: registryName
}

var registryCredentials = registry.listCredentials()

var registryConfiguration = [
  {
    server: registryLoginServer
    username: registryCredentials.username
    passwordSecretRef: 'registry-password'
  }
]

var serviceProbes = [
  {
    type: 'Liveness'
    httpGet: {
      path: '/health'
      port: 8080
    }
    initialDelaySeconds: 10
    periodSeconds: 30
  }
  {
    type: 'Readiness'
    httpGet: {
      path: '/health/ready'
      port: 8080
    }
    initialDelaySeconds: 5
    periodSeconds: 10
  }
]

resource productApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: productAppName
  location: location
  tags: tags
  properties: {
    managedEnvironmentId: containerEnvironmentId
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        targetPort: 8080
        transport: 'http'
        allowInsecure: true
      }
      secrets: [
        {
          name: 'registry-password'
          value: registryCredentials.passwords[0].value
        }
        {
          name: 'product-db-connection'
          #disable-next-line use-secure-value-for-secure-inputs
          value: productConnectionString
        }
        {
          name: 'jwt-secret'
          value: jwtSecret
        }
        {
          name: 'internal-api-key'
          value: internalApiKey
        }
        ...(empty(foundryApiKey) ? [] : [
          {
            name: 'foundry-api-key'
            value: foundryApiKey
          }
        ])
      ]
      registries: registryConfiguration
    }
    template: {
      containers: [
        {
          name: 'product-service'
          image: productImage
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://+:8080'
            }
            {
              name: 'Database__Provider'
              value: 'SqlServer'
            }
            {
              name: 'ConnectionStrings__ProductDb'
              secretRef: 'product-db-connection'
            }
            {
              name: 'Jwt__Secret'
              secretRef: 'jwt-secret'
            }
            {
              name: 'Jwt__Issuer'
              value: jwtIssuer
            }
            {
              name: 'Jwt__Audience'
              value: jwtAudience
            }
            {
              name: 'Internal__ApiKey'
              secretRef: 'internal-api-key'
            }
            {
              name: 'Cors__AllowedOrigins__0'
              value: frontendOrigin
            }
            {
              name: 'Foundry__Endpoint'
              value: foundryEndpoint
            }
            ...(empty(foundryApiKey) ? [] : [
              {
                name: 'Foundry__ApiKey'
                secretRef: 'foundry-api-key'
              }
            ])
            {
              name: 'Foundry__ChatDeployment'
              value: foundryChatDeployment
            }
            {
              name: 'Foundry__Mode'
              value: foundryMode
            }
          ]
          probes: serviceProbes
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 3
      }
    }
  }
}

resource userApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: userAppName
  location: location
  tags: tags
  properties: {
    managedEnvironmentId: containerEnvironmentId
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        targetPort: 8080
        transport: 'http'
        allowInsecure: true
      }
      secrets: [
        {
          name: 'registry-password'
          value: registryCredentials.passwords[0].value
        }
        {
          name: 'user-db-connection'
          #disable-next-line use-secure-value-for-secure-inputs
          value: userConnectionString
        }
        {
          name: 'jwt-secret'
          value: jwtSecret
        }
        {
          name: 'internal-api-key'
          value: internalApiKey
        }
        ...(empty(adminPassword) ? [] : [
          {
            name: 'admin-password'
            value: adminPassword
          }
        ])
      ]
      registries: registryConfiguration
    }
    template: {
      containers: [
        {
          name: 'user-service'
          image: userImage
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://+:8080'
            }
            {
              name: 'Database__Provider'
              value: 'SqlServer'
            }
            {
              name: 'ConnectionStrings__UserDb'
              secretRef: 'user-db-connection'
            }
            {
              name: 'Jwt__Secret'
              secretRef: 'jwt-secret'
            }
            {
              name: 'Jwt__Issuer'
              value: jwtIssuer
            }
            {
              name: 'Jwt__Audience'
              value: jwtAudience
            }
            {
              name: 'Internal__ApiKey'
              secretRef: 'internal-api-key'
            }
            {
              name: 'Cors__AllowedOrigins__0'
              value: frontendOrigin
            }
            {
              name: 'Seed__AdminEmail'
              value: adminEmail
            }
            ...(empty(adminPassword) ? [] : [
              {
                name: 'Seed__AdminPassword'
                secretRef: 'admin-password'
              }
            ])
          ]
          probes: serviceProbes
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 3
      }
    }
  }
}

resource orderApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: orderAppName
  location: location
  tags: tags
  properties: {
    managedEnvironmentId: containerEnvironmentId
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        targetPort: 8080
        transport: 'http'
        allowInsecure: true
      }
      secrets: [
        {
          name: 'registry-password'
          value: registryCredentials.passwords[0].value
        }
        {
          name: 'order-db-connection'
          #disable-next-line use-secure-value-for-secure-inputs
          value: orderConnectionString
        }
        {
          name: 'jwt-secret'
          value: jwtSecret
        }
        {
          name: 'internal-api-key'
          value: internalApiKey
        }
        ...(empty(foundryApiKey) ? [] : [
          {
            name: 'foundry-api-key'
            value: foundryApiKey
          }
        ])
      ]
      registries: registryConfiguration
    }
    template: {
      containers: [
        {
          name: 'order-service'
          image: orderImage
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://+:8080'
            }
            {
              name: 'Database__Provider'
              value: 'SqlServer'
            }
            {
              name: 'ConnectionStrings__OrderDb'
              secretRef: 'order-db-connection'
            }
            {
              name: 'Jwt__Secret'
              secretRef: 'jwt-secret'
            }
            {
              name: 'Jwt__Issuer'
              value: jwtIssuer
            }
            {
              name: 'Jwt__Audience'
              value: jwtAudience
            }
            {
              name: 'Internal__ApiKey'
              secretRef: 'internal-api-key'
            }
            {
              name: 'Services__ProductServiceUrl'
              value: 'http://${productApp.name}'
            }
            {
              name: 'Cors__AllowedOrigins__0'
              value: frontendOrigin
            }
            {
              name: 'Foundry__Endpoint'
              value: foundryEndpoint
            }
            ...(empty(foundryApiKey) ? [] : [
              {
                name: 'Foundry__ApiKey'
                secretRef: 'foundry-api-key'
              }
            ])
            {
              name: 'Foundry__ChatDeployment'
              value: foundryChatDeployment
            }
            {
              name: 'Foundry__Mode'
              value: foundryMode
            }
          ]
          probes: serviceProbes
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 3
      }
    }
  }
}

resource gatewayApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: gatewayAppName
  location: location
  tags: tags
  properties: {
    managedEnvironmentId: containerEnvironmentId
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8080
        transport: 'http'
        allowInsecure: true
      }
      secrets: [
        {
          name: 'registry-password'
          value: registryCredentials.passwords[0].value
        }
        {
          name: 'jwt-secret'
          value: jwtSecret
        }
        {
          name: 'internal-api-key'
          value: internalApiKey
        }
      ]
      registries: registryConfiguration
    }
    template: {
      containers: [
        {
          name: 'gateway'
          image: gatewayImage
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://+:8080'
            }
            {
              name: 'Jwt__Secret'
              secretRef: 'jwt-secret'
            }
            {
              name: 'Jwt__Issuer'
              value: jwtIssuer
            }
            {
              name: 'Jwt__Audience'
              value: jwtAudience
            }
            {
              name: 'Services__ProductServiceUrl'
              value: 'http://${productApp.name}'
            }
            {
              name: 'Services__UserServiceUrl'
              value: 'http://${userApp.name}'
            }
            {
              name: 'Services__OrderServiceUrl'
              value: 'http://${orderApp.name}'
            }
            {
              name: 'Cors__AllowedOrigins__0'
              value: frontendOrigin
            }
          ]
          probes: serviceProbes
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 3
      }
    }
  }
}

resource frontendApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: frontendAppName
  location: location
  tags: tags
  properties: {
    managedEnvironmentId: containerEnvironmentId
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 80
        transport: 'http'
        allowInsecure: false
      }
      secrets: [
        {
          name: 'registry-password'
          value: registryCredentials.passwords[0].value
        }
      ]
      registries: registryConfiguration
    }
    template: {
      containers: [
        {
          name: 'frontend'
          image: frontendImage
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: [
            {
              name: 'GATEWAY_UPSTREAM'
              value: 'http://${gatewayApp.name}'
            }
          ]
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path: '/'
                port: 80
              }
              initialDelaySeconds: 10
              periodSeconds: 30
            }
            {
              type: 'Readiness'
              httpGet: {
                path: '/'
                port: 80
              }
              initialDelaySeconds: 5
              periodSeconds: 10
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 3
      }
    }
  }
}

output frontendUrl string = 'https://${frontendApp.properties.configuration.ingress.fqdn}'
output gatewayUrl string = 'https://${gatewayApp.properties.configuration.ingress.fqdn}'
output productServiceUrl string = 'http://${productApp.name}'
output userServiceUrl string = 'http://${userApp.name}'
output orderServiceUrl string = 'http://${orderApp.name}'
