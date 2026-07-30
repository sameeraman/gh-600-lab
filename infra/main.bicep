// ============================================================
// GH-600 Lab - Azure Infrastructure (Bicep)
// Deploys: App Service (API) + Static Web App (Frontend)
// ============================================================

@description('Environment name')
param environment string = 'dev'

@description('Azure region')
param location string = resourceGroup().location

@description('Unique suffix for resource names')
param uniqueSuffix string = uniqueString(resourceGroup().id)

@description('Entra object ID that will be SQL AAD admin during provisioning')
param sqlAdminObjectId string

@description('Entra login name (UPN or app name) for the SQL AAD admin')
param sqlAdminLogin string

var appServicePlanName = 'asp-todo-${environment}-${uniqueSuffix}'
var apiAppName = 'app-todo-api-${environment}-${uniqueSuffix}'
var staticWebAppName = 'swa-todo-${environment}-${uniqueSuffix}'
var sqlServerName = 'sql-gh600-${uniqueSuffix}'
var sqlDbName = 'tododb'

// App Service Plan
resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
  properties: {
    reserved: true // Linux
  }
}

// API App Service
resource apiApp 'Microsoft.Web/sites@2023-01-01' = {
  name: apiAppName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: environment == 'prod' ? 'Production' : 'Development'
        }
        {
          name: 'ConnectionStrings__TodoDb'
          value: 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=${sqlDbName};Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;Connection Timeout=60;'
        }
      ]
    }
    httpsOnly: true
  }
}

// Static Web App for Frontend
resource staticWebApp 'Microsoft.Web/staticSites@2023-01-01' = {
  name: staticWebAppName
  location: location
  sku: {
    name: 'Standard'
    tier: 'Standard'
  }
  properties: {
    stagingEnvironmentPolicy: 'Enabled'
    allowConfigFileUpdates: true
  }
}

resource sqlServer 'Microsoft.Sql/servers@2023-05-01-preview' = {
  name: sqlServerName
  location: location
  properties: {
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    administrators: {
      administratorType: 'ActiveDirectory'
      principalType: 'Application'
      login: sqlAdminLogin
      sid: sqlAdminObjectId
      tenantId: subscription().tenantId
      azureADOnlyAuthentication: true
    }
  }
}

resource sqlDb 'Microsoft.Sql/servers/databases@2023-05-01-preview' = {
  parent: sqlServer
  name: sqlDbName
  location: location
  sku: {
    name: 'GP_S_Gen5_2'
    tier: 'GeneralPurpose'
    family: 'Gen5'
    capacity: 2
  }
  properties: {
    autoPauseDelay: 60
    minCapacity: json('0.5')
    maxSizeBytes: 34359738368
  }
}

resource sqlFirewallAzure 'Microsoft.Sql/servers/firewallRules@2023-05-01-preview' = {
  parent: sqlServer
  name: 'AllowAllWindowsAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource swaLinkedBackend 'Microsoft.Web/staticSites/linkedBackends@2023-01-01' = {
  parent: staticWebApp
  name: 'api'
  properties: {
    backendResourceId: apiApp.id
    region: location
  }
}

// Outputs for CI/CD pipeline
output apiAppName string = apiApp.name
output apiAppUrl string = 'https://${apiApp.properties.defaultHostName}'
output apiPrincipalId string = apiApp.identity.principalId
output sqlServerName string = sqlServer.name
output sqlDatabaseName string = sqlDbName
output staticWebAppName string = staticWebApp.name
output resourceGroupName string = resourceGroup().name
