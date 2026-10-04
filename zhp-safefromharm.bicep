@description('Region zasobow. Grupa zasobow zostaje w West Europe, same zasoby stoja w Polsce.')
param location string = 'polandcentral'

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

@minLength(1)
@secure()
param microsoftProviderSecret string

@description('Rejestracja aplikacji w Entra ID uzywana przez Easy Auth do ochrony triggera CreateAccounts.')
param easyAuthClientId string = '670599f4-bf68-4ae5-8d5e-759513a17b92'

@description('Tenant, ktory wystawia tokeny akceptowane przez Easy Auth (zhp.net.pl).')
param easyAuthTenantId string = 'e1368d1e-3975-4ce6-893d-fc351fd44dcd'

@description('Audience oczekiwane w tokenie — identyfikator aplikacji frontendowej.')
param easyAuthAllowedAudience string = 'https://safefromharm.zhp.pl'

@description('Gorny limit instancji planu Flex Consumption. Aplikacja jest wolana rzadko (raz na tydzien/miesiac plus recznie), wiec limit jest tu bezpiecznikiem, nie wymiarowaniem.')
@minValue(1)
@maxValue(1000)
param maximumInstanceCount int = 40

@description('Pamiec pojedynczej instancji w MB. Dozwolone: 512, 2048, 4096.')
@allowed([512, 2048, 4096])
param instanceMemoryMB int = 2048

var storageAccountName = '${uniqueString(resourceGroup().id)}azfunctions'
var deploymentContainerName = 'deployment-package'
var storageConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'

resource storageAccount 'Microsoft.Storage/storageAccounts@2025-01-01' = {
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

  resource blobServices 'blobServices' = {
    name: 'default'

    // Flex Consumption nie zna WEBSITE_RUN_FROM_PACKAGE — pakiet wdrozeniowy laduje w tym kontenerze.
    resource deploymentContainer 'containers' = {
      name: deploymentContainerName
    }
  }
}

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: 'zhp-safefromharm-logs'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'zhpsafefromharminsights'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    Request_Source: 'rest'
    WorkspaceResourceId: logAnalytics.id
  }
}

resource hostingPlan 'Microsoft.Web/serverfarms@2025-03-01' = {
  name: 'zhp-safefromharm-plan'
  location: location
  sku: {
    name: 'FC1'
    tier: 'FlexConsumption'
  }
  kind: 'functionapp'
  properties: {
    reserved: true
  }
}

resource functionApp 'Microsoft.Web/sites@2025-03-01' = {
  name: 'zhp-safefromharm'
  location: location
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: hostingPlan.id
    httpsOnly: true

    functionAppConfig: {
      deployment: {
        storage: {
          type: 'blobContainer'
          value: '${storageAccount.properties.primaryEndpoints.blob}${deploymentContainerName}'
          authentication: {
            // Kluczem, a nie tozsamoscia: konto wdrazajace z GitHub Actions ma wylacznie role
            // Contributor, wiec nie moze nadac managed identity roli Storage Blob Data Contributor.
            type: 'StorageAccountConnectionString'
            storageAccountConnectionStringName: 'DEPLOYMENT_STORAGE_CONNECTION_STRING'
          }
        }
      }
      runtime: {
        name: 'dotnet-isolated'
        version: '10.0'
      }
      scaleAndConcurrency: {
        maximumInstanceCount: maximumInstanceCount
        instanceMemoryMB: instanceMemoryMB
      }
    }

    siteConfig: {
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: storageConnectionString
        }
        {
          name: 'DEPLOYMENT_STORAGE_CONNECTION_STRING'
          value: storageConnectionString
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsights.properties.ConnectionString
        }
        {
          name: 'MICROSOFT_PROVIDER_AUTHENTICATION_SECRET'
          value: microsoftProviderSecret
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
      minTlsVersion: '1.2'
    }
  }

  dependsOn: [
    storageAccount::blobServices::deploymentContainer
  ]

  resource corsSettings 'config' = {
    name: 'web'
    properties: {
      cors: {
        allowedOrigins: [
          'https://portal.azure.com'
          'http://localhost:5000'
          'https://konta-sfh.zhp.pl'
          'https://konta-sfh.pages.dev'
        ]
        supportCredentials: true
      }
    }
  }

  // Trigger CreateAccounts ma AuthorizationLevel.Anonymous i cala jego ochrona opiera sie na
  // naglowku X-MS-CLIENT-PRINCIPAL-NAME wstrzykiwanym przez Easy Auth. Bez tej sekcji funkcja
  // wstaje calkowicie otwarta, dlatego konfiguracja jest tutaj, a nie ustawiana recznie w Portalu.
  resource authSettings 'config' = {
    name: 'authsettingsV2'
    properties: {
      platform: {
        enabled: true
        runtimeVersion: '~1'
      }
      globalValidation: {
        requireAuthentication: true
        // AllowAnonymous: token jest walidowany gdy jest, ale jego brak nie blokuje zadania —
        // funkcja sama porownuje nazwe zalogowanego uzytkownika z RequestorEmail w body.
        unauthenticatedClientAction: 'AllowAnonymous'
      }
      httpSettings: {
        requireHttps: true
        routes: {
          apiPrefix: '/.auth'
        }
        forwardProxy: {
          convention: 'NoProxy'
        }
      }
      identityProviders: {
        azureActiveDirectory: {
          enabled: true
          registration: {
            clientId: easyAuthClientId
            clientSecretSettingName: 'MICROSOFT_PROVIDER_AUTHENTICATION_SECRET'
            openIdIssuer: 'https://sts.windows.net/${easyAuthTenantId}/v2.0'
          }
          validation: {
            allowedAudiences: [
              easyAuthAllowedAudience
            ]
          }
          login: {
            disableWWWAuthenticate: false
          }
        }
      }
      login: {
        tokenStore: {
          enabled: false
          tokenRefreshExtensionHours: 72
        }
        preserveUrlFragmentsForLogins: false
        routes: {
          logoutEndpoint: '/.auth/logout'
        }
        nonce: {
          validateNonce: true
          nonceExpirationInterval: '00:05:00'
        }
        cookieExpiration: {
          convention: 'FixedTime'
          timeToExpiration: '08:00:00'
        }
      }
    }

    // App Service potrafi sie pogubic przy rownoleglym zapisie dwoch sekcji config —
    // wymuszamy kolejnosc zamiast liczyc na szczescie.
    dependsOn: [
      corsSettings
    ]
  }
}

output functionAppPrincipalId string = functionApp.identity.principalId
output functionAppName string = functionApp.name
