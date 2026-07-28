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

var appServicePlanName = 'asp-todo-${environment}-${uniqueSuffix}'
var apiAppName = 'app-todo-api-${environment}-${uniqueSuffix}'
var staticWebAppName = 'swa-todo-${environment}-${uniqueSuffix}'

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
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: environment == 'prod' ? 'Production' : 'Development'
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
    name: 'Free'
    tier: 'Free'
  }
  properties: {
    stagingEnvironmentPolicy: 'Enabled'
    allowConfigFileUpdates: true
  }
}

// Outputs for CI/CD pipeline
output apiAppName string = apiApp.name
output apiAppUrl string = 'https://${apiApp.properties.defaultHostName}'
output staticWebAppName string = staticWebApp.name
output resourceGroupName string = resourceGroup().name
