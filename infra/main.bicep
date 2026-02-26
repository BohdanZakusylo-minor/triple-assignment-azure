@description('Location for all resources')
param location string = resourceGroup().location

@description('Base name for resources (used for naming). Keep short.')
@minLength(3)
@maxLength(20)
param baseName string = 'fanoutimg'

@description('Optional explicit Function App name (must be globally unique). Leave empty to auto-generate.')
param functionAppNameOverride string = ''

@description('Table name used by Azure.Data.Tables TableClient (TableStorage:TableName).')
param tableName string = 'statustable'

@description('Blob container name used by IBlobStorage (BlobStorage:ContainerName).')
param blobContainerName string = 'blob-for-tripple'

@description('Optional extra app settings to append (array of {name,value}).')
param extraAppSettings array = []

var uniqueSuffix = uniqueString(resourceGroup().id, baseName)
var baseLower = toLower(baseName)

var storageAccountName = toLower('${take(replace(baseLower, '-', ''), 11)}${uniqueSuffix}')

var functionAppName = empty(functionAppNameOverride)
  ? toLower('${baseLower}-${uniqueSuffix}-func')
  : toLower(functionAppNameOverride)

var appInsightsName = empty(functionAppNameOverride)
  ? toLower('${baseLower}-${uniqueSuffix}-ai')
  : toLower('${functionAppName}-ai')

var planName = empty(functionAppNameOverride)
  ? toLower('${baseLower}-${uniqueSuffix}-plan')
  : toLower('${functionAppName}-plan')

var storageConnString = 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
  }
}

// --- Queues ---
resource queueService 'Microsoft.Storage/storageAccounts/queueServices@2023-01-01' = {
  parent: storageAccount
  name: 'default'
}

resource fanoutStartQueue 'Microsoft.Storage/storageAccounts/queueServices/queues@2023-01-01' = {
  parent: queueService
  name: 'fanout-start'
}

resource imageQueue 'Microsoft.Storage/storageAccounts/queueServices/queues@2023-01-01' = {
  parent: queueService
  name: 'imagequeue'
}

// --- Blob container ---
resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-01-01' = {
  parent: storageAccount
  name: 'default'
}

resource blobsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = {
  parent: blobService
  name: blobContainerName
  properties: {
    publicAccess: 'None'
  }
}

// --- Table ---
resource tableService 'Microsoft.Storage/storageAccounts/tableServices@2023-01-01' = {
  parent: storageAccount
  name: 'default'
}

resource statusTable 'Microsoft.Storage/storageAccounts/tableServices/tables@2023-01-01' = {
  parent: tableService
  name: tableName
}

// --- App Insights ---
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
  }
}

// --- Consumption plan ---
resource plan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: planName
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
}

// --- Function App ---
resource functionApp 'Microsoft.Web/sites@2023-01-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      appSettings: concat(
        [
          // Required by Functions runtime
          {
            name: 'AzureWebJobsStorage'
            value: storageConnString
          }
          {
            name: 'FUNCTIONS_EXTENSION_VERSION'
            value: '~4'
          }
          {
            name: 'FUNCTIONS_WORKER_RUNTIME'
            value: 'dotnet-isolated'
          }

          // App Insights
          {
            name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
            value: appInsights.properties.InstrumentationKey
          }
          {
            name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
            value: appInsights.properties.ConnectionString
          }

          // Your DI config keys
          {
            name: 'TableStorageConnection'
            value: storageConnString
          }
          {
            name: 'TableStorage:TableName'
            value: tableName
          }
          {
            name: 'BlobStorageConnection'
            value: storageConnString
          }
          {
            name: 'BlobStorage:ContainerName'
            value: blobContainerName
          }

          // Content share (Windows)
          {
            name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
            value: storageConnString
          }
          {
            name: 'WEBSITE_CONTENTSHARE'
            value: toLower(take(replace(functionAppName, '-', ''), 60))
          }

          // ---- Better Function host logging ----
          {
            name: 'AzureFunctionsJobHost__logging__logLevel__Default'
            value: 'Information'
          }
          {
            name: 'AzureFunctionsJobHost__logging__logLevel__Host'
            value: 'Information'
          }
          {
            name: 'AzureFunctionsJobHost__logging__logLevel__Function'
            value: 'Information'
          }
          // Keep Kudu log streaming alive longer
          {
            name: 'SCM_LOGSTREAM_TIMEOUT'
            value: '7200'
          }
        ],
        extraAppSettings
      )
    }
  }
}

output functionAppUrl string = 'https://${functionApp.properties.defaultHostName}'
output storageAccountName string = storageAccount.name
output queueFanoutStartName string = fanoutStartQueue.name
output queueImageName string = imageQueue.name
output tableNameOut string = statusTable.name
output containerNameOut string = blobsContainer.name
output functionAppName string = functionApp.name
