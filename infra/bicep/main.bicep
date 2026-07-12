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
var cosmosDbName = '${resourceNamePrefix}-cosmos'
var appInsightsName = '${resourceNamePrefix}-ai'
var logAnalyticsName = '${resourceNamePrefix}-logs'
var containerRegistryName = replace('${resourceNamePrefix}acr', '-', '')

// Log Analytics Workspace for monitoring (Free tier: 5GB/month)
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
  tags: {
    environment: environment
    application: 'pokemonencyclopedia'
    tier: 'free'
  }
}

// Application Insights (Free tier: 5GB/month)
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    RetentionInDays: 30
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
    WorkspaceResourceId: logAnalytics.id
  }
  tags: {
    environment: environment
    application: 'pokemonencyclopedia'
    tier: 'free'
  }
}

// Redis Cache (Free tier: 250MB)
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
    tier: 'free'
  }
}

// Redis Diagnostic Settings
resource redisDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: '${redisCacheName}-diagnostics'
  scope: redisCache
  properties: {
    workspaceId: logAnalytics.id
    logs: [
      {
        category: 'ConnectedClientList'
        enabled: true
      }
    ]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
      }
    ]
  }
}

// Cosmos DB (Free tier: 400 RU/s)
resource cosmosDb 'Microsoft.DocumentDB/databaseAccounts@2023-11-15' = {
  name: cosmosDbName
  location: location
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    enableFreeTier: true
    enableAnalyticalStorage: false
    locations: [
      {
        locationName: location
        failoverPriority: 0
      }
    ]
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
  }
  tags: {
    environment: environment
    application: 'pokemonencyclopedia'
    tier: 'free'
  }
}

// Cosmos DB Diagnostic Settings
resource cosmosDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: '${cosmosDbName}-diagnostics'
  scope: cosmosDb
  properties: {
    workspaceId: logAnalytics.id
    logs: [
      {
        category: 'DataPlaneRequests'
        enabled: true
      }
      {
        category: 'MongoRequests'
        enabled: true
      }
      {
        category: 'QueryRuntimeStatistics'
        enabled: true
      }
      {
        category: 'PartitionKeyStatistics'
        enabled: true
      }
      {
        category: 'ControlPlaneRequests'
        enabled: true
      }
    ]
    metrics: [
      {
        category: 'Requests'
        enabled: true
      }
    ]
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

// Container App Environment Diagnostic Settings
resource caeOmsDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: '${containerAppEnvironment.name}-diagnostics'
  scope: containerAppEnvironment
  properties: {
    workspaceId: logAnalytics.id
    logs: [
      {
        category: 'ContainerAppConsoleLogs'
        enabled: true
      }
      {
        category: 'ContainerAppSystemLogs'
        enabled: true
      }
    ]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
      }
    ]
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
        {
          name: 'cosmos-connection-string'
          value: cosmosDb.listConnectionStrings().connectionStrings[0].connectionString
        }
        {
          name: 'app-insights-key'
          value: appInsights.properties.InstrumentationKey
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
              name: 'DEPLOYMENT_MODE'
              value: 'azure'
            }
            {
              name: 'REDIS_ENDPOINT'
              value: '${redisCache.properties.hostName}:${redisCache.properties.port}'
            }
            {
              name: 'REDIS_PASSWORD'
              secretRef: 'redis-connection-string'
            }
            {
              name: 'COSMOS_CONNECTION_STRING'
              secretRef: 'cosmos-connection-string'
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              secretRef: 'app-insights-key'
            }
            {
              name: 'OTEL_EXPORTER_OTLP_ENDPOINT'
              value: 'http://localhost:4317'
            }
            {
              name: 'OTEL_SERVICE_NAME'
              value: 'pokemonencyclopedia-api'
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
  dependsOn: [
    redisDiagnostics
    cosmosDiagnostics
  ]
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
        {
          name: 'app-insights-key'
          value: appInsights.properties.InstrumentationKey
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
              name: 'DEPLOYMENT_MODE'
              value: 'azure'
            }
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://+:8080'
            }
            {
              name: 'APISERVICE_ENDPOINT'
              value: 'http://${apiService.properties.configuration.ingress.fqdn}'
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              secretRef: 'app-insights-key'
            }
            {
              name: 'OTEL_EXPORTER_OTLP_ENDPOINT'
              value: 'http://localhost:4317'
            }
            {
              name: 'OTEL_SERVICE_NAME'
              value: 'pokemonencyclopedia-web'
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

@description('Cosmos DB endpoint')
output cosmosDbEndpoint string = cosmosDb.properties.documentEndpoint

@description('Application Insights instrumentation key')
output appInsightsInstrumentationKey string = appInsights.properties.InstrumentationKey

@description('Application Insights connection string')
output appInsightsConnectionString string = appInsights.properties.ConnectionString

@description('Container App Environment ID')
output containerAppEnvironmentId string = containerAppEnvironment.id

@description('Log Analytics Workspace ID')
output logAnalyticsWorkspaceId string = logAnalytics.id

@description('Log Analytics Workspace Key')
output logAnalyticsWorkspaceKey string = logAnalytics.listKeys().primarySharedKey
