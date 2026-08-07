// ============================================================
// GH-600 Lab - Azure Infrastructure (Bicep)
// Deploys: App Service (API) + Static Web App (Frontend)
// ============================================================

@description('Environment name')
param environment string = 'dev'

@description('Azure region')
param location string = resourceGroup().location

@description('Region for SQL (separate: some regions block new SQL server creation)')
param sqlLocation string = location

@description('Unique suffix for resource names')
param uniqueSuffix string = uniqueString(resourceGroup().id)

@description('Entra object ID that will be SQL AAD admin during provisioning')
param sqlAdminObjectId string

@description('Entra login name (UPN or app name) for the SQL AAD admin')
param sqlAdminLogin string

@description('Microsoft Entra application (client) ID that the API must accept in token audiences')
param authenticationClientId string

var appServicePlanName = 'asp-todo-${environment}-${uniqueSuffix}'
var apiAppName = 'app-todo-api-${environment}-${uniqueSuffix}'
var apiIdentityName = 'id-todo-api-${environment}-${uniqueSuffix}'
var staticWebAppName = 'swa-todo-${environment}-${uniqueSuffix}'
var sqlServerName = 'sql1-gh600-${uniqueSuffix}'
var sqlDbName = 'tododb'

// User-assigned rather than system-assigned: only a user-assigned identity exposes
// its client ID as an ARM property, which the pipeline needs to create the SQL
// contained user by SID without Microsoft Graph directory lookups.
resource apiIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: apiIdentityName
  location: location
}

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
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${apiIdentity.id}': {}
    }
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
          // Static Web Apps forwards identity as x-ms-client-principal, never as a bearer token,
          // so the API trusts the header that Easy Auth validates and injects instead.
          name: 'Authentication__RequireSignedTokens'
          value: 'false'
        }
        {
          name: 'Authentication__TenantId'
          value: subscription().tenantId
        }
        {
          name: 'Authentication__ClientId'
          value: authenticationClientId
        }
        {
          // Tells DefaultAzureCredential which user-assigned identity to present to SQL.
          name: 'AZURE_CLIENT_ID'
          value: apiIdentity.properties.clientId
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

resource apiAuth 'Microsoft.Web/sites/config@2023-12-01' = {
  parent: apiApp
  name: 'authsettingsV2'
  properties: {
    platform: {
      enabled: true
    }
    globalValidation: {
      requireAuthentication: true
      unauthenticatedClientAction: 'Return401'
    }
    httpSettings: {
      requireHttps: true
    }
    identityProviders: {
      azureStaticWebApps: {
        enabled: true
      }
    }
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
  location: sqlLocation
  tags: {
    SecurityControl: 'Ignore'
  }
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
  location: sqlLocation
  tags: {
    SecurityControl: 'Ignore'
  }
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

module sqlFirewallRules 'sql-firewall-rules.bicep' = {
  params: {
    sqlServerName: sqlServer.name
    apiOutboundIpAddresses: split(apiApp.properties.possibleOutboundIpAddresses, ',')
  }
}

resource swaLinkedBackend 'Microsoft.Web/staticSites/linkedBackends@2023-01-01' = {
  parent: staticWebApp
  name: 'api'
  properties: {
    backendResourceId: apiApp.id
    region: location
  }
  dependsOn: [
    apiAuth
  ]
}

// Outputs for CI/CD pipeline
output apiAppName string = apiApp.name
output apiAppUrl string = 'https://${apiApp.properties.defaultHostName}'
output apiPrincipalId string = apiIdentity.properties.principalId
output apiIdentityClientId string = apiIdentity.properties.clientId
output sqlServerName string = sqlServer.name
output sqlDatabaseName string = sqlDbName
output staticWebAppName string = staticWebApp.name
output staticWebAppUrl string = 'https://${staticWebApp.properties.defaultHostname}'
output resourceGroupName string = resourceGroup().name
