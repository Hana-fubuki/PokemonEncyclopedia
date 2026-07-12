@description('The environment name for resource naming')
param environment string = 'dev'

@description('The Azure region for deployment')
param location string = 'eastus2'

@description('The resource name prefix')
param namePrefix string = 'pokepedia'

@description('Container registry URL for pulling images')
param containerRegistryUrl string

@description('Container registry username')
param containerRegistryUsername string

@description('Container registry password')
@secure()
param containerRegistryPassword string

@description('API Service image tag')
param apiImageTag string = 'latest'

@description('Web Service image tag')
param webImageTag string = 'latest'

@description('Enable managed identity for container apps')
param enableManagedIdentity bool = true

var resourceGroupName = resourceGroup().name
var resourceNamePrefix = '${namePrefix}-${environment}'
var containerAppEnvName = '${resourceNamePrefix}-cae'
var apiServiceName = '${resourceNamePrefix}-api'
var webServiceName = '${resourceNamePrefix}-web'
var redisCacheName = '${resourceNamePrefix}-redis'
var logAnalyticsName = '${resourceNamePrefix}-logs'
var containerRegistryName = replace('${resourceNamePrefix}acr', '-', '')

// Log Analytics Workspace for monitoring
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

// Redis Cache for caching
resource redisCache 'Microsoft.Cache/redis@2023-08-01' = {
  name: redisCacheName
  location: location
  properties: {
    sku: {
      name: 'Standard'
      family: 'C'
      capacity: 0
    }
    enableNonSslPort: false
    minimumTlsVersion: '1.2'
  }
  tags: {
    environment: environment
    application: 'pokemonencyclopedia'
  }
}

// Container App Environment
resource containerAppEnvironment 'Microsoft.App/managedEnvironments@2023-11-02-preview' = {
  name: containerAppEnvName
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
  }
  tags: {
    environment: environment
    application: 'pokemonencyclopedia'
  }
}

// API Service Container App
resource apiService 'Microsoft.App/containerApps@2023-11-02-preview' = {
  name: apiServiceName
  location: location
  identity: enableManagedIdentity ? {
    type: 'SystemAssigned'
  } : null
  properties: {
    managedEnvironmentId: containerAppEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      registries: [
        {
          server: containerRegistryUrl
          username: containerRegistryUsername
          passwordSecretRef: 'container-registry-password'
        }
      ]
      secrets: [
        {
          name: 'container-registry-password'
          value: containerRegistryPassword
        }
        {
          name: 'redis-connection-string'
          value: 'redisHost=${redisCache.properties.hostName},redisPort=${redisCache.properties.port},ssl=true,password=${redisCache.listKeys().primaryKey}'
        }
      ]
      ingress: {
        external: false
        targetPort: 8080
        transport: 'auto'
      }
    }
    template: {
      containers: [
        {
          name: 'api-service'
          image: '${containerRegistryUrl}/pokemonencyclopedia-api:${apiImageTag}'
          resources: {
            cpu: '0.5'
            memory: '1.0Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: environment
            }
            {
              name: 'REDIS_ENDPOINT'
              value: '${redisCache.properties.hostName}:${redisCache.properties.port}'
            }
            {
              name: 'REDIS_PASSWORD'
              secretRef: 'redis-connection-string'
            }
          ]
          probes: [
            {
              type: 'liveness'
              httpGet: {
                path: '/health'
                port: 8080
              }
              initialDelaySeconds: 10
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
  tags: {
    environment: environment
    application: 'pokemonencyclopedia'
    service: 'api'
  }
}

// Web Service Container App
resource webService 'Microsoft.App/containerApps@2023-11-02-preview' = {
  name: webServiceName
  location: location
  identity: enableManagedIdentity ? {
    type: 'SystemAssigned'
  } : null
  properties: {
    managedEnvironmentId: containerAppEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      registries: [
        {
          server: containerRegistryUrl
          username: containerRegistryUsername
          passwordSecretRef: 'container-registry-password'
        }
      ]
      secrets: [
        {
          name: 'container-registry-password'
          value: containerRegistryPassword
        }
      ]
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
        allowInsecure: false
        traffic: [
          {
            label: 'latest'
            latestRevision: true
            weight: 100
          }
        ]
      }
    }
    template: {
      containers: [
        {
          name: 'web-service'
          image: '${containerRegistryUrl}/pokemonencyclopedia-web:${webImageTag}'
          resources: {
            cpu: '0.5'
            memory: '1.0Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: environment
            }
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://+:8080'
            }
            {
              name: 'APISERVICE_ENDPOINT'
              value: 'http://${apiService.properties.configuration.ingress.fqdn}'
            }
          ]
          probes: [
            {
              type: 'liveness'
              httpGet: {
                path: '/'
                port: 8080
              }
              initialDelaySeconds: 10
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
  tags: {
    environment: environment
    application: 'pokemonencyclopedia'
    service: 'web'
  }
  dependsOn: [
    apiService
  ]
}

@description('The FQDN of the web service')
output webServiceUrl string = 'https://${webService.properties.configuration.ingress.fqdn}'

@description('The API service FQDN')
output apiServiceFqdn string = apiService.properties.configuration.ingress.fqdn

@description('Redis cache hostname')
output redisCacheHostname string = redisCache.properties.hostName

@description('Redis cache port')
output redisCachePort int = redisCache.properties.port

@description('Container App Environment ID')
output containerAppEnvironmentId string = containerAppEnvironment.id

@description('Log Analytics Workspace ID')
output logAnalyticsWorkspaceId string = logAnalytics.id
