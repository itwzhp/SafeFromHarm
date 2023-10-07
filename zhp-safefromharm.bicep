@description('Location for all resources.')
param location string = resourceGroup().location

@minLength(1)
param tipiTokenId string

@minLength(1)
@secure()
param tipiTokenSecret string

@minLength(1)
@secure()
param moodleToken string

@minLength(1)
param smtpHost string

param smtpPort int

@minLength(1)
param smtpUsername string

@minLength(1)
@secure()
param smtpPassword string

var storageAccountName = '${uniqueString(resourceGroup().id)}azfunctions'

resource storageAccount 'Microsoft.Storage/storageAccounts@2022-05-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'Storage'
  properties: {
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
  }
}

resource hostingPlan 'Microsoft.Web/serverfarms@2021-03-01' = {
  name: 'zhp-safefromharm-plan'
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  properties: {}
}

resource functionApp 'Microsoft.Web/sites@2021-03-01' = {
  name: 'zhp-safefromharm'
  location: location
  kind: 'functionapp'
  properties: {
    serverFarmId: hostingPlan.id
    siteConfig: {
      linuxFxVersion: 'dotnet-isolated|7.0'
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
        }
        // {
        //   name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
        //   value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
        // }
        // {
        //   name: 'WEBSITE_CONTENTSHARE'
        //   value: toLower(functionAppName)
        // }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: applicationInsights.properties.InstrumentationKey
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
        {
          name: 'SCM_DO_BUILD_DURING_DEPLOYMENT'
          value: '0'
        }
        {
          name: 'Tipi__TokenId'
          value: tipiTokenId
        }
        {
          name: 'Tipi__TokenSecret'
          value: tipiTokenSecret
        }
        {
          name: 'Moodle__MoodleToken'
          value: moodleToken
        }
        {
          name: 'Smtp__Host'
          value: smtpHost
        }
        {
          name: 'Smtp__Port'
          value: string(smtpPort)
        }
        {
          name: 'Smtp__Username'
          value: smtpUsername
        }
        {
          name: 'Smtp__Password'
          value: smtpPassword
        }

        {
          name: 'CertifiedMembersFetcher'
          value: 'Moodle'
        }
        {
          name: 'EmailMembershipNumberMapper'
          value: 'Moodle'
        }
        {
          name: 'RequiredMembersFetcher'
          value: 'Tipi'
        }
        {
          name: 'EmailMembershipNumberMapper'
          value: 'Dummy' //change to Smtp to enable mails
        }
      ]
      ftpsState: 'FtpsOnly'
      minTlsVersion: '1.2'
    }
    httpsOnly: true
  }
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'zhpsafefromharminsights'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    Request_Source: 'rest'
  }
}
