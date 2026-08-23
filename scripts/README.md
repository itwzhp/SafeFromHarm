# Skrypty odtwarzania konfiguracji spoza bicepa

[zhp-safefromharm.bicep](../zhp-safefromharm.bicep) opisuje wszystko, co da sie opisac szablonem —
lacznie z konfiguracja Easy Auth, ktora wczesniej istniala wylacznie w Portalu Azure.

Poza szablonem zostaja tylko uprawnienia nadawane managed identity **poza Azure Resource Managerem**,
i to jest jedyne zadanie [Restore-ManagedIdentityPermissions.ps1](Restore-ManagedIdentityPermissions.ps1).
Skrypt jest idempotentny i wspiera `-WhatIf`.

## Co odtwarza skrypt

| Uprawnienie | Wartosc | Do czego |
|---|---|---|
| Graph, aplikacyjne | `User.Read.All` | `EntraIdEmailMembershipNumberMapper`, `EntraIdMemberMailAccountChecker` |
| Graph, aplikacyjne | `Sites.Selected` | warunek wstepny dla grantu na site'cie |
| SharePoint, na site'cie | `write` na `SafeFromHarm-penomocnicychorgwiani` | odczyt "Lista pelnomocnikow", zapis "Zalozone konta" |

Stan odczytany 2026-08-20 ze starej aplikacji (objectId `b21748fa-2a86-4f22-ad06-95a9931762ad`,
appId `9047646b-cf8c-45b8-8e9c-240812207e87`). Poza powyzszymi tozsamosc **nie miala nic**:
zadnych przypisan Azure RBAC, czlonkostw w grupach, rol katalogowych ani uprawnien delegowanych.

Site SharePoint: `gkzhp.sharepoint.com,68e38698-4c2b-46a5-b278-a45bc93df050,b1c478c6-3557-400e-a1ef-993c8fc4e5d9`.
Srodkowy GUID to `GraphApi:SfhSiteId` z [appsettings.json](../src/Zhp.SafeFromHarm.Func/appsettings.json) —
identyfikatory site'u i list sa w repo, wiec nie wymagaja backupu.

## Dlaczego tego nie ma w bicepie

- **Uprawnienia aplikacyjne Graph** — Bicep ma rozszerzenie Microsoft Graph
  (`Microsoft.Graph/appRoleAssignedTo`), ale konto wdrazajace z GitHub Actions
  (`1728ea31-eed7-44bb-9a8c-5bab5aeae8ab`) ma wylacznie role Contributor na grupie zasobow i zero
  uprawnien do Graph. Zeby przeniesc to do szablonu, trzeba by nadac pipeline'owi
  `AppRoleAssignment.ReadWrite.All` — czyli prawo nadawania dowolnych uprawnien aplikacyjnych
  w calym tenancie. To duzo szersza wladza niz wdrazanie zasobow i nie warta tej wygody.
- **Grant na site'cie SharePoint** — `sites/{id}/permissions` nie ma odpowiednika w ARM ani
  w rozszerzeniu Graph dla Bicepa. Jedyna droga to wywolanie API.
- **RBAC na koncie magazynu** — Contributor nie moze tworzyc przypisan rol. Szablon omija ten
  problem, uwierzytelniajac kontener wdrozeniowy kluczem konta (`DEPLOYMENT_STORAGE_CONNECTION_STRING`)
  zamiast tozsamoscia, wiec zadna rola nie jest potrzebna.

Skrypt uzywa Azure CLI do uprawnien Graph, ale grant na site'cie robi przez `Connect-MgGraph` —
Azure CLI nie posiada `Sites.FullControl.All`, wiec przez `az rest` sie tego nie da. W tenancie
zhp.net.pl ten scope jest juz zgodzony dla wszystkich uzytkownikow, wiec pojawi sie samo logowanie,
bez ekranu zgody administratora. Skrypt wymaga konta z rola Global Administrator.

## Kolejnosc przy odtwarzaniu aplikacji

1. Usun stare zasoby (patrz ostrzezenie nizej).
2. Wdroz bicepa — zasoby, app settings, Easy Auth i wlaczenie System assigned identity.
3. `.\Restore-ManagedIdentityPermissions.ps1` — nowa aplikacja ma **nowy objectId i nowy appId**,
   skrypt odczytuje oba sam z Function App. Uprawnienia Graph wiaza sie po `objectId`,
   a grant na site'cie SharePoint po `appId`.
4. Opublikuj kod i sprawdz: raport testowy przez trigger HTTP oraz logowanie
   z https://safefromharm.zhp.pl i wywolanie `CreateAccounts`.

Uprawnienia aplikacyjne Graph propaguja sie do kilku minut — pierwsze 403 zaraz po wdrozeniu
nie musi oznaczac bledu w konfiguracji.

## Uwaga o migracji na Flex Consumption / Poland Central

Zasoby przenosza sie do **Poland Central**, a plan zmienia sie z Y1 (Consumption) na FC1
(Flex Consumption). Ani regionu, ani SKU planu nie da sie zmienic w miejscu — stad odtwarzanie
aplikacji od zera.

**Dopoki stare zasoby stoja w West Europe, krok `Test ARM` w
[build-and-deploy.yaml](../.github/workflows/build-and-deploy.yaml) bedzie failowal**
z `InvalidResourceLocation` ("resource already exists in location 'westeurope'").
To nie blad szablonu — nazwa zasobu jest zajeta w innym regionie. Zniknie, gdy stare zasoby
zostana usuniete.

Sekret rejestracji Easy Auth (`670599f4-bf68-4ae5-8d5e-759513a17b92`) zyje w Entra ID niezaleznie
od Function App i przetrwa jej usuniecie — warto tylko sprawdzic jego date waznosci.
