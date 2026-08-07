@description('Name of the Azure SQL logical server')
param sqlServerName string

@description('Possible outbound IP addresses used by the API App Service')
param apiOutboundIpAddresses array

resource sqlServer 'Microsoft.Sql/servers@2023-05-01-preview' existing = {
  name: sqlServerName
}

resource apiOutboundFirewallRules 'Microsoft.Sql/servers/firewallRules@2023-05-01-preview' = [
  for (ipAddress, index) in apiOutboundIpAddresses: {
    parent: sqlServer
    // Reuse the old broad rule name so incremental deployments replace it.
    name: index == 0 ? 'AllowAllWindowsAzureIps' : 'AllowApiOutbound-${index}'
    properties: {
      startIpAddress: trim(ipAddress)
      endIpAddress: trim(ipAddress)
    }
  }
]
