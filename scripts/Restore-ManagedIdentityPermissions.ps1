<#
.SYNOPSIS
    Nadaje ponownie uprawnienia system-assigned managed identity aplikacji zhp-safefromharm.

.DESCRIPTION
    Skrypt odtwarza uprawnienia, ktore NIE sa opisane w zhp-safefromharm.bicep i przepadaja
    przy usunieciu Function App (bo nowa aplikacja dostaje nowa tozsamosc — nowy objectId i appId):

      1. Microsoft Graph, uprawnienie aplikacyjne  User.Read.All   (df021288-bdef-4463-88db-98f22de89214)
         — EntraIdMemberMailAccountChecker sprawdza w Entra ID, czy czlonek (employeeId) ma
           aktywne konto pocztowe.

      2. Microsoft Graph, uprawnienie aplikacyjne  Sites.Selected  (883ea226-0bf2-4a8f-9f9d-92c9162a727d)
         — samo w sobie nie daje dostepu do zadnego site'u; dopiero krok 3 wskazuje ktory.

      3. Uprawnienie 'write' na site SharePoint "Safe From Harm - pelnomocnicy choragwiani"
         (https://gkzhp.sharepoint.com/sites/SafeFromHarm-penomocnicychorgwiani)
         — SharepointUnitContactMailProvider czyta liste "Lista pelnomocnikow",
           SharepointAccountCreationResultPublisher pisze do listy "Zalozone konta".
           Rola 'write' pokrywa oba przypadki.

    Stan zrodlowy (odczytany 2026-08-20 ze starej aplikacji, objectId b21748fa-2a86-4f22-ad06-95a9931762ad):
      - appRoleAssignments : User.Read.All (nadane 2023-11-25), Sites.Selected (nadane 2023-12-06)
      - site permission    : roles=['write'] dla aplikacji appId 9047646b-cf8c-45b8-8e9c-240812207e87
      - Azure RBAC         : BRAK zadnych przypisan (ani na subskrypcji, ani na RG)
      - grupy / role katalogowe / uprawnienia delegowane : BRAK

    Skrypt jest idempotentny — mozna go uruchomic wielokrotnie, istniejace uprawnienia sa pomijane.

.NOTES
    Wymagania:
      - Azure CLI, zalogowane konto z rola Global Administrator (nadawanie uprawnien aplikacyjnych Graph).
      - Modul PowerShell Microsoft.Graph.Authentication (do kroku 3 — Azure CLI nie ma
        uprawnienia Sites.FullControl.All, wiec grantu na site'cie nie da sie zrobic przez 'az rest').

    Krok 3 wywola logowanie do Microsoft Graph PowerShell w przegladarce. W tenancie zhp.net.pl
    scope Sites.FullControl.All jest juz zgodzony dla wszystkich uzytkownikow, wiec nie pojawi sie
    ekran zgody administratora — samo logowanie.

.EXAMPLE
    .\Restore-ManagedIdentityPermissions.ps1 -WhatIf
    Pokazuje co zostanie nadane, bez wprowadzania zmian.

.EXAMPLE
    .\Restore-ManagedIdentityPermissions.ps1
    Nadaje brakujace uprawnienia.

.EXAMPLE
    .\Restore-ManagedIdentityPermissions.ps1 -GrantDeploymentStorageRole -StorageAccountName 2xqlngsnt7xlcazfunctions
    Dodatkowo nadaje 'Storage Blob Data Contributor' na koncie magazynu — potrzebne tylko wtedy,
    gdy plan Flex Consumption bedzie korzystal z managed identity do kontenera z pakietem wdrozeniowym.
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$SubscriptionId = '35e22786-93ea-47c8-902a-16e9d0f03e17',

    [string]$ResourceGroup = 'zhp-safefromharm',

    [string]$FunctionAppName = 'zhp-safefromharm',

    # Pelny identyfikator site'u SharePoint w formacie {hostname},{siteCollectionId},{webId}.
    # Srodkowy GUID to jednoczesnie GraphApi:SfhSiteId z appsettings.json.
    [string]$SharepointSiteId = 'gkzhp.sharepoint.com,68e38698-4c2b-46a5-b278-a45bc93df050,b1c478c6-3557-400e-a1ef-993c8fc4e5d9',

    [ValidateSet('read', 'write', 'fullcontrol')]
    [string]$SharepointRole = 'write',

    # Opcjonalnie: RBAC na koncie magazynu. Stara aplikacja tego NIE miala (uzywala klucza
    # w AzureWebJobsStorage). Przydatne dopiero przy Flex Consumption z deploymentem przez MI.
    [switch]$GrantDeploymentStorageRole,

    [string]$StorageAccountName
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# appId Microsoft Graph jest staly we wszystkich tenantach; objectId service principala juz nie —
# dlatego ponizej jest wyszukiwany dynamicznie.
$graphAppId = '00000003-0000-0000-c000-000000000000'

$requiredAppRoles = @(
    [pscustomobject]@{ Id = 'df021288-bdef-4463-88db-98f22de89214'; Name = 'User.Read.All' }
    [pscustomobject]@{ Id = '883ea226-0bf2-4a8f-9f9d-92c9162a727d'; Name = 'Sites.Selected' }
)

#region helpers

function Invoke-GraphViaAz {
    <#
        Wolanie Microsoft Graph przez 'az rest'. Body jest przekazywane plikiem, bo az.cmd na Windows
        rozjezdza sie na cudzyslowach w --body i Graph zwraca "Unable to read JSON request payload".
    #>
    param(
        [Parameter(Mandatory)][ValidateSet('GET', 'POST', 'PATCH', 'DELETE')][string]$Method,
        [Parameter(Mandatory)][string]$Uri,
        [object]$Body
    )

    $azArgs = @('rest', '--method', $Method, '--url', $Uri, '--output', 'json')
    $bodyFile = $null

    try {
        if ($null -ne $Body) {
            $bodyFile = New-TemporaryFile
            ($Body | ConvertTo-Json -Depth 10 -Compress) | Set-Content -Path $bodyFile.FullName -Encoding ascii
            $azArgs += @('--headers', 'Content-Type=application/json', '--body', "@$($bodyFile.FullName)")
        }

        $output = (& az @azArgs 2>&1) | Out-String

        if ($LASTEXITCODE -ne 0) {
            throw "Wywolanie Graph ($Method $Uri) nie powiodlo sie:`n$output"
        }

        if ([string]::IsNullOrWhiteSpace($output)) { return $null }

        return $output | ConvertFrom-Json
    }
    finally {
        if ($bodyFile) { Remove-Item $bodyFile.FullName -Force -ErrorAction SilentlyContinue }
    }
}

function Write-Step {
    param([string]$Message)
    Write-Host ''
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Write-Ok {
    param([string]$Message)
    Write-Host "    [ok] $Message" -ForegroundColor Green
}

function Write-Skip {
    param([string]$Message)
    Write-Host "    [pomijam] $Message" -ForegroundColor DarkGray
}

#endregion

#region 0. Kontrola srodowiska

Write-Step 'Sprawdzam srodowisko'

if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    throw 'Nie znaleziono Azure CLI (az). Zainstaluj: https://aka.ms/installazurecli'
}

if (-not (Get-Module -ListAvailable -Name Microsoft.Graph.Authentication)) {
    throw 'Brak modulu Microsoft.Graph.Authentication. Zainstaluj: Install-Module Microsoft.Graph.Authentication -Scope CurrentUser'
}

$account = (& az account show --output json 2>&1) | Out-String
if ($LASTEXITCODE -ne 0) {
    throw "Nie jestes zalogowany do Azure CLI. Uruchom: az login"
}
$account = $account | ConvertFrom-Json

if ($account.id -ne $SubscriptionId) {
    Write-Host "    Przelaczam subskrypcje na $SubscriptionId" -ForegroundColor Yellow
    & az account set --subscription $SubscriptionId | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Nie udalo sie przelaczyc na subskrypcje $SubscriptionId" }
}

Write-Ok "Azure CLI: $($account.user.name), tenant $($account.tenantId)"

#endregion

#region 1. Tozsamosc nowej aplikacji

Write-Step "Odczytuje managed identity aplikacji '$FunctionAppName'"

$identity = (& az functionapp identity show --name $FunctionAppName --resource-group $ResourceGroup --output json 2>&1) | Out-String
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($identity)) {
    throw "Nie udalo sie odczytac tozsamosci aplikacji '$FunctionAppName' w grupie '$ResourceGroup'. " +
          "Upewnij sie, ze aplikacja istnieje i ma wlaczone System assigned identity."
}
$identity = $identity | ConvertFrom-Json

$principalId = $identity.principalId
if ([string]::IsNullOrWhiteSpace($principalId)) {
    throw "Aplikacja '$FunctionAppName' nie ma wlaczonej tozsamosci System assigned."
}

Write-Ok "objectId (principalId): $principalId"

# Swiezo utworzony service principal potrafi nie byc jeszcze widoczny w Graph — czekamy na replikacje.
$servicePrincipal = $null
$deadline = (Get-Date).AddMinutes(3)

while ($null -eq $servicePrincipal) {
    try {
        $servicePrincipal = Invoke-GraphViaAz -Method GET `
            -Uri "https://graph.microsoft.com/v1.0/servicePrincipals/$principalId`?`$select=id,appId,displayName,servicePrincipalType"
    }
    catch {
        if ((Get-Date) -gt $deadline) {
            throw "Service principal $principalId nie pojawil sie w Entra ID w ciagu 3 minut. Ostatni blad:`n$_"
        }
        Write-Host '    Czekam na replikacje tozsamosci w Entra ID...' -ForegroundColor DarkGray
        Start-Sleep -Seconds 10
    }
}

$appId = $servicePrincipal.appId
Write-Ok "appId (clientId):        $appId"
Write-Ok "displayName:             $($servicePrincipal.displayName) ($($servicePrincipal.servicePrincipalType))"

#endregion

#region 2. Uprawnienia aplikacyjne Microsoft Graph

Write-Step 'Nadaje uprawnienia aplikacyjne Microsoft Graph'

$graphSpResponse = Invoke-GraphViaAz -Method GET `
    -Uri "https://graph.microsoft.com/v1.0/servicePrincipals?`$filter=appId eq '$graphAppId'&`$select=id"

if (-not $graphSpResponse.value) {
    throw 'Nie znaleziono service principala Microsoft Graph w tym tenancie.'
}
$graphSpObjectId = $graphSpResponse.value[0].id

$existingAssignments = Invoke-GraphViaAz -Method GET `
    -Uri "https://graph.microsoft.com/v1.0/servicePrincipals/$principalId/appRoleAssignments"

foreach ($role in $requiredAppRoles) {
    $alreadyGranted = $existingAssignments.value | Where-Object { $_.appRoleId -eq $role.Id -and $_.resourceId -eq $graphSpObjectId }

    if ($alreadyGranted) {
        Write-Skip "$($role.Name) — juz nadane"
        continue
    }

    if (-not $PSCmdlet.ShouldProcess("$FunctionAppName / $principalId", "nadaj uprawnienie Graph $($role.Name)")) {
        continue
    }

    Invoke-GraphViaAz -Method POST `
        -Uri "https://graph.microsoft.com/v1.0/servicePrincipals/$principalId/appRoleAssignments" `
        -Body @{
            principalId = $principalId
            resourceId  = $graphSpObjectId
            appRoleId   = $role.Id
        } | Out-Null

    Write-Ok "$($role.Name) — nadane"
}

#endregion

#region 3. Uprawnienie do site'u SharePoint (Sites.Selected)

Write-Step "Nadaje uprawnienie '$SharepointRole' na site SharePoint"

Import-Module Microsoft.Graph.Authentication -ErrorAction Stop

$context = Get-MgContext
if (-not $context -or $context.Scopes -notcontains 'Sites.FullControl.All') {
    Write-Host '    Loguje do Microsoft Graph (otworzy sie okno przegladarki)...' -ForegroundColor Yellow
    Connect-MgGraph -Scopes 'Sites.FullControl.All' -NoWelcome
    $context = Get-MgContext
}
Write-Ok "Microsoft Graph PowerShell: $($context.Account)"

$permissionsUri = "https://graph.microsoft.com/v1.0/sites/$SharepointSiteId/permissions"
$existingPermissions = Invoke-MgGraphRequest -Method GET -Uri $permissionsUri

$existingGrant = $existingPermissions.value | Where-Object {
    $identities = @()
    if ($_.ContainsKey('grantedToIdentitiesV2')) { $identities += $_.grantedToIdentitiesV2 }
    if ($_.ContainsKey('grantedToIdentities')) { $identities += $_.grantedToIdentities }

    $identities | Where-Object { $_.application -and $_.application.id -eq $appId }
}

if ($existingGrant -and ($existingGrant.roles -contains $SharepointRole)) {
    Write-Skip "grant dla appId $appId juz istnieje (roles: $($existingGrant.roles -join ', '))"
}
elseif ($existingGrant) {
    # Grant istnieje, ale z inna rola — podnosimy/zmieniamy zamiast tworzyc drugi wpis.
    if ($PSCmdlet.ShouldProcess($SharepointSiteId, "zmien role grantu dla $appId na '$SharepointRole'")) {
        Invoke-MgGraphRequest -Method PATCH `
            -Uri "$permissionsUri/$($existingGrant.id)" `
            -Body @{ roles = @($SharepointRole) } | Out-Null

        Write-Ok "grant zaktualizowany do roli '$SharepointRole'"
    }
}
else {
    if ($PSCmdlet.ShouldProcess($SharepointSiteId, "nadaj '$SharepointRole' aplikacji $appId")) {
        Invoke-MgGraphRequest -Method POST -Uri $permissionsUri -Body @{
            roles                = @($SharepointRole)
            grantedToIdentities  = @(
                @{
                    application = @{
                        id          = $appId
                        displayName = $servicePrincipal.displayName
                    }
                }
            )
        } | Out-Null

        Write-Ok "grant '$SharepointRole' nadany aplikacji $appId"
    }
}

#endregion

#region 4. Opcjonalnie: RBAC na koncie magazynu (Flex Consumption)

if ($GrantDeploymentStorageRole) {
    Write-Step 'Nadaje Storage Blob Data Contributor na koncie magazynu'

    if ([string]::IsNullOrWhiteSpace($StorageAccountName)) {
        throw 'Podaj -StorageAccountName razem z -GrantDeploymentStorageRole.'
    }

    $scope = "/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroup/providers/Microsoft.Storage/storageAccounts/$StorageAccountName"
    $storageRole = 'Storage Blob Data Contributor'

    # Stderr celowo nie jest laczony ze stdout: ostrzezenie z az zanieczysciloby JSON.
    $roleJson = (& az role assignment list --assignee-object-id $principalId --scope $scope `
            --role $storageRole --output json) | Out-String

    if ($LASTEXITCODE -ne 0) {
        throw "Nie udalo sie odczytac przypisan roli na $scope. Sprawdz, czy konto magazynu '$StorageAccountName' istnieje w grupie '$ResourceGroup'."
    }

    $existingRoles = @()
    if (-not [string]::IsNullOrWhiteSpace($roleJson)) {
        $existingRoles = @($roleJson | ConvertFrom-Json)
    }

    if ($existingRoles.Count -gt 0) {
        Write-Skip "$storageRole — juz nadane"
    }
    elseif ($PSCmdlet.ShouldProcess($scope, "nadaj $storageRole")) {
        & az role assignment create --assignee-object-id $principalId --assignee-principal-type ServicePrincipal `
            --role $storageRole --scope $scope --output none
        if ($LASTEXITCODE -ne 0) { throw "Nie udalo sie nadac roli $storageRole." }

        Write-Ok "$storageRole — nadane"
    }
}

#endregion

#region 5. Weryfikacja

if ($WhatIfPreference) {
    Write-Host ''
    Write-Host 'Tryb -WhatIf: nic nie zostalo zmienione.' -ForegroundColor Yellow
    return
}

Write-Step 'Weryfikacja koncowa'

$finalAssignments = Invoke-GraphViaAz -Method GET `
    -Uri "https://graph.microsoft.com/v1.0/servicePrincipals/$principalId/appRoleAssignments"

foreach ($role in $requiredAppRoles) {
    $granted = $finalAssignments.value | Where-Object { $_.appRoleId -eq $role.Id }
    if ($granted) { Write-Ok "Graph $($role.Name)" }
    else { Write-Warning "BRAK uprawnienia Graph $($role.Name)" }
}

$finalPermissions = Invoke-MgGraphRequest -Method GET -Uri $permissionsUri
$finalGrant = $finalPermissions.value | Where-Object {
    $identities = @()
    if ($_.ContainsKey('grantedToIdentitiesV2')) { $identities += $_.grantedToIdentitiesV2 }
    if ($_.ContainsKey('grantedToIdentities')) { $identities += $_.grantedToIdentities }

    $identities | Where-Object { $_.application -and $_.application.id -eq $appId }
}

if ($finalGrant) { Write-Ok "SharePoint: $($finalGrant.roles -join ', ') na $SharepointSiteId" }
else { Write-Warning "BRAK grantu na site SharePoint $SharepointSiteId" }

if ($GrantDeploymentStorageRole) {
    $verifyJson = (& az role assignment list --assignee-object-id $principalId --scope $scope `
            --role $storageRole --output json) | Out-String

    $verifyRoles = @()
    if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($verifyJson)) {
        $verifyRoles = @($verifyJson | ConvertFrom-Json)
    }

    if ($verifyRoles.Count -gt 0) { Write-Ok "$storageRole na koncie $StorageAccountName" }
    else { Write-Warning "BRAK roli $storageRole na koncie $StorageAccountName" }
}

Write-Host ''
Write-Host 'Gotowe. Uprawnienia aplikacyjne Graph potrafia propagowac sie do kilku minut —' -ForegroundColor Yellow
Write-Host 'jesli aplikacja zaraz po wdrozeniu dostaje 403 z Graph, zrestartuj ja i sprobuj ponownie.' -ForegroundColor Yellow

#endregion
