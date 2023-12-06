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
@secure()
param smtpPassword string

@minLength(1)
@secure()
param controlTeamsChannelMail string

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
  kind: 'functionapp'
  properties: {
    reserved: true
  }
}

resource functionApp 'Microsoft.Web/sites@2021-03-01' = {
  name: 'zhp-safefromharm'
  location: location
  kind: 'functionapp'
  properties: {
    serverFarmId: hostingPlan.id
    siteConfig: {
      linuxFxVersion: 'DOTNET-ISOLATED|8.0'
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
        }
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
          value: 'mail-auto-mx.zhp.pl'
        }
        {
          name: 'Smtp__Port'
          value: '587'
        }
        {
          name: 'Smtp__Username'
          value: 'safe.from.harm@mail-auto.zhp.pl'
        }
        {
          name: 'Smtp__Password'
          value: smtpPassword
        }
        { 
          name: 'SafeFromHarm__ControlTeamsChannelMail'
          value: controlTeamsChannelMail
        } 

        // ports for reports
        {
          name: 'Toggles__CertifiedMembersFetcher'
          value: 'Moodle'
        }
        {
          name: 'Toggles__EmailMembershipNumberMapper'
          value: 'Ms365'
        }
        {
          name: 'Toggles__RequiredMembersFetcher'
          value: 'Tipi'
        }
        {
          name: 'Toggles__NotificationSender'
          value: 'Smtp'
        }

        // ports for account creation
        {
          name: 'Toggles__AccountCreator'
          value: 'Moodle'
        }
        {
          name: 'Toggles__AccountCreationResultPublishers__0'
          value: 'Sharepoint'
        }
        {
          name: 'Toggles__AccountCreationResultPublishers__1'
          value: 'Smtp'
        }
        {
          name: 'Toggles__MemberMailAccountChecker'
          value: 'Ms365'
        }
        {
          name: 'Toggles__MembersFetcher'
          value: 'Tipi'
        }
      ]
      ftpsState: 'FtpsOnly'
      minTlsVersion: '1.2'
    }
    httpsOnly: true
  }
  identity:{
    type: 'SystemAssigned'
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
