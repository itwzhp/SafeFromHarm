# CLAUDE.md

Ten plik dostarcza wskazówek dla Claude Code (claude.ai/code) do pracy z kodem w tym repozytorium.

## Co to jest

Aplikacja Azure Functions (.NET 10, isolated worker) wspierająca wdrażanie polityki "Safe from Harm" w ZHP. Trzy funkcje:

- **Zakładanie kont** — zakłada konta w Moodle dla osób bez konta pocztowego ZHP (głównie seniorzy zdający egzamin na miejscu, na komputerze hufca/chorągwi). Wywoływana przez frontend, uwierzytelniana przez Entra ID.
- **Powiadomienia o brakującej certyfikacji** — pobiera z Tipi listę osób, które *muszą* być certyfikowane, razem z datą ważności ich certyfikatu, i mailuje jednostkom, kto ma ważny certyfikat (i do kiedy), a kto nie.
- **Raporty chorągwiane** — co tydzień (poniedziałek) wysyła każdej chorągwi raport (HTML + załącznik CSV) stanu certyfikacji, a zespołowi kontrolującemu — raport zbiorczy dla całego związku.

Język domenowy jest polski i przenika do identyfikatorów, komentarzy oraz treści maili: *hufiec*, *chorągiew*, *GK* (Główna Kwatera), *przydział*, *numer ewidencyjny*. Nowe teksty widoczne dla użytkownika pisz po polsku.

## Komendy

```powershell
dotnet build src/Zhp.SafeFromHarm.sln
dotnet test src/Zhp.SafeFromHarm.sln
dotnet test src/Zhp.SafeFromHarm.sln --filter "FullyQualifiedName~MoodleAccountCreatorTests"   # pojedyncza klasa testowa
dotnet test src/Zhp.SafeFromHarm.sln --filter "FullyQualifiedName~MissingCertificationsNotifierTests.SendNotificationsOnMissingCertificates_FindsRequiredMembers"

func start --port 7292                    # uruchomienie funkcji lokalnie, z src/Zhp.SafeFromHarm.Func (wymaga Azurite dla AzureWebJobsStorage)
npm --prefix frontend start               # statyczny frontend na http://localhost:5000
```

`dotnet test` działa w trybie MTP (`"test": { "runner": "Microsoft.Testing.Platform" }` w [global.json](global.json)), bo xunit.v3 to natywny projekt MTP, a `dotnet test` w .NET 10 nie mostkuje już takich projektów przez VSTest. `--filter` działa tak samo jak wcześniej.

Sekrety lokalne trafiają do user secrets projektu Func, nigdy do `appsettings.Development.json`:

```powershell
dotnet user-secrets set Smtp:Username <username> --project src/Zhp.SafeFromHarm.Func
dotnet user-secrets set Smtp:Password <password> --project src/Zhp.SafeFromHarm.Func
dotnet user-secrets set Moodle:MoodleToken <token> --project src/Zhp.SafeFromHarm.Func
dotnet user-secrets set Tipi:TokenId <id> --project src/Zhp.SafeFromHarm.Func
dotnet user-secrets set Tipi:TokenSecret <secret> --project src/Zhp.SafeFromHarm.Func
```

## Architektura — porty i adaptery

Trzy projekty w `src/`:

- `Zhp.SafeFromHarm.Domain` — serwisy orkiestrujące (`AccountCreator`, `MissingCertificationsNotifier`, `ReportGenerator`), rekordy modelu i **wszystkie** interfejsy `Ports/*`. Zależy tylko od logowania/options oraz `System.Linq.AsyncEnumerable` z BCL (bez pakietu `System.Linq.Async` — jego nazewnictwo `SelectAwait`/`Select` koliduje z typem z BCL na net10.0; asynchroniczne przeciążenia `Select`/`Where` wymagają teraz jawnego parametru `CancellationToken`, żeby dowiązać się do właściwej metody — patrz `MapAsync` w [TipiRequiredMembersFetcher.cs](src/Zhp.SafeFromHarm.Func/Adapters/Tipi/TipiRequiredMembersFetcher.cs)).
- `Zhp.SafeFromHarm.Func` — triggery Azure Functions (cienkie: parsują body, wołają serwis domenowy) plus po jednym folderze adapterów na system zewnętrzny: `Tipi/`, `Moodle/`, `GraphApi/`, `Smtp/`, `TestDummy/`.
- `Zhp.SafeFromHarm.Tests` — xUnit v3 + FluentAssertions + NSubstitute. `InternalsVisibleTo` pozwala testom sięgać do adapterów `internal`.

**Wybór adaptera jest sterowany konfiguracją.** [HostExtensionMethods.cs](src/Zhp.SafeFromHarm.Func/Infrastructure/HostExtensionMethods.cs) czyta sekcję `Toggles` do `AdapterTogglesOptions`, a jego helper `AddSwitch` mapuje każdą wartość tekstową na rejestrację (np. `MembersFetcher: "Tipi" | "Dummy"`). Nieznana wartość rzuca wyjątkiem przy starcie. Dodając implementację portu, trzeba: dopisać case do właściwego słownika `AddSwitch`, dodać ustawienie do [zhp-safefromharm.bicep](zhp-safefromharm.bicep) (`Toggles__<Klucz>`, podwójny podkreślnik) i — do developmentu lokalnego — do [appsettings.Development.json](src/Zhp.SafeFromHarm.Func/appsettings.Development.json). `AccountCreationResultPublishers` to jedyny toggle *listowy* (rejestrowane i uruchamiane są wszystkie wymienione publishery). `NotificationSender` razem z senderami rejestruje też `IUnitContactMailProvider` (`Smtp` → kontakty jednostek z listy SharePoint, `Dummy` → pusta lista), bo korzystają z niego wyłącznie senderzy SMTP.

Adaptery `TestDummy/` to nie fixture'y testowe — to pełnoprawne rejestracje używane zarówno przez `appsettings.Development.json` (żeby lokalne uruchomienia nie mailowały prawdziwych jednostek), jak i przez testy jednostkowe domeny jako źródła danych.

`AspectTests/DependencyInjectionTests` buduje prawdziwy host dla każdej klasy Function i go rozwiązuje, więc zepsuta rejestracja albo nieprzechodząca `.Validate(...)` w options wywala testy zamiast produkcyjnego startu. `FunctionTriggerChecks` sprawdza, że żaden timer trigger nie ma `RunOnStartup`.

### Przepływ danych raportu certyfikacji

Obie funkcje certyfikacyjne pobierają przez `IRequiredMembersFetcher` z Tipi (`sfh/members-for-training`) listę osób wymaganych do certyfikacji, gdzie każda ma `certificateValidUntil` (`YYYY-MM-DD` albo `null` = brak certyfikatu), i budują z niej `CertificationReport`. Konstruktor raportu dostaje dzisiejszą datę i traktuje certyfikat po terminie ważności jak jego brak — to jedyne miejsce tej reguły. Jeden model `MemberToCertify` niesie zarówno dane osoby, jak i datę ważności — Moodle ani Entra ID nie biorą udziału w ustalaniu, kto jest certyfikowany. Brak pola `certificateValidUntil` w odpowiedzi Tipi kończy się wyjątkiem deserializacji (`[JsonRequired]`), żeby niezaktualizowane Tipi nie dało po cichu raportu „nikt nie ma certyfikatu”.

Dwa różne pola jednostki sterują routingiem: `Supervisor` (hufiec, w razie braku — chorągiew/GK) dostaje *powiadomienie*; `Department` (chorągiew albo GK) dostaje *raport regionalny*. `ReportGenerator` pomija `Department.Id == 2` (Główna Kwatera).

### Systemy zewnętrzne

| System | Uwierzytelnienie | Uwagi |
|---|---|---|
| Tipi (`tipi-api.zhp.pl`) | Nagłówki Cloudflare Access `CF-Access-Client-Id`/`Secret` | Retry Polly: 5 × 2 s. Pusty wynik jest traktowany jako błąd, nie jako "nikogo do certyfikacji". |
| Moodle (`edu.zhp.pl`) | `wstoken` w query stringu | Timeout HttpClient 10 min; żądania idą pod `MoodleHostName` z nadpisanym nagłówkiem `Host`, żeby obejść 100-sekundowy limit Cloudflare. Moodle zwraca HTTP 200 nawet przy błędach, więc `MoodleClient` wykrywa je po polu `exception` w body. |
| Microsoft Graph | `ManagedIdentityCredential` na Azure, `InteractiveBrowserCredential` w Development | Sprawdzanie konta pocztowego członka w Entra ID (zakładanie kont) + dwie listy SharePoint (założone konta, kontakty jednostek). |
| SMTP (`mail-auto-mx.zhp.pl`) | użytkownik/hasło | `Smtp:OverrideRecipient` przekierowuje *całą* pocztę na jeden adres testowy. Każde ciało HTML musi mieć też wersję tekstową przez `SmtpHelper.ClearHtml`. |

### Triggery

`CreateAccounts` ma `AuthorizationLevel.Anonymous` — jest chroniona przez App Service Easy Auth i sama sprawdza, czy nagłówek `X-MS-CLIENT-PRINCIPAL-NAME` zgadza się z `RequestorEmail` w body. Pozostałe dwie to triggery HTTP `AuthorizationLevel.Function` plus bliźniaczy timer: `FindMissingRequiredCertifications` co miesiąc (28. dnia), `GenerateReports` co tydzień (poniedziałki). Ręczne wywołania HTTP przyjmują `{"RecipientFilter": "*"}` dla wszystkich albo mail jednej jednostki, żeby zaadresować tylko ją.

## Zależności NuGet

Wszystkie pakiety są na bieżąco (`dotnet list package --outdated` czyste). Warte odnotowania:

- `Microsoft.Graph` 6.x i `Microsoft.Identity.Web.MicrosoftGraph` 4.x — duży skok wersji major, ale w kodzie nie ma żadnego bezpośredniego użycia namespace'u `Microsoft.Identity.Web` (tylko `Microsoft.Graph`/`Azure.Identity`), więc build i testy przeszły bez zmian w kodzie.
- `FluentAssertions` 8.x zmienił licencję (Xceed) — bezpłatny dla projektów non-profit/edukacyjnych/indywidualnych, płatny dla firm komercyjnych powyżej pewnego progu przychodu. Warto to mieć na uwadze, jeśli repozytorium kiedyś zmieni właściciela na komercyjny.
- `xunit.v3`/`xunit.runner.visualstudio` muszą iść w parze z `global.json` (patrz sekcja Komendy) — to one wymusiły przejście na tryb MTP.

## Deployment

Push na `master` wdraża automatycznie: [.github/workflows/build-and-deploy.yaml](.github/workflows/build-and-deploy.yaml) buduje, testuje, waliduje i aplikuje `zhp-safefromharm.bicep` w trybie **Complete**, po czym publikuje Function App. Bicep jest źródłem prawdy dla app settings — cokolwiek ustawione tylko w Portalu Azure zostanie skasowane przy kolejnym deployu. Frontend (`frontend/`, czysty jQuery + MSAL, bez build stepu) jest wdrażany osobno przez CloudFlare Pages i jest wykluczony z filtrów ścieżek workflow.

Aplikacja stoi na planie **Flex Consumption** (`FC1`) w regionie **Poland Central**. Runtime i sposób wdrożenia opisuje `functionAppConfig` (`runtime: dotnet-isolated 10.0`, pakiet w kontenerze blob `deployment-package`), a nie `siteConfig.linuxFxVersion` — dlatego w app settings **nie ma** `FUNCTIONS_EXTENSION_VERSION`, `FUNCTIONS_WORKER_RUNTIME`, `WEBSITE_RUN_FROM_PACKAGE`, `SCM_DO_BUILD_DURING_DEPLOYMENT` ani `WEBSITE_ENABLE_SYNC_UPDATE_SITE`; Flex ich nie obsługuje. Konfiguracja Easy Auth (`authsettingsV2`) też jest w bicepie — bez niej `CreateAccounts` wstaje bez ochrony.

Poza bicepem zostają wyłącznie uprawnienia managed identity nadawane spoza ARM: `User.Read.All` i `Sites.Selected` w Graph oraz grant `write` na site'cie SharePoint. Odtwarza je [scripts/Restore-ManagedIdentityPermissions.ps1](scripts/Restore-ManagedIdentityPermissions.ps1) — uruchamiany po odtworzeniu aplikacji, bo nowa tożsamość dostaje nowy `objectId` i nowy `appId`. Szczegóły i uzasadnienie w [scripts/README.md](scripts/README.md).
