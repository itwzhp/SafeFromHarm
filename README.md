# Opis
To jest aplikacja wspierająca wdrażanie polityki Safe from Harm w ZHP, w szczególności informowanie o postępach.

## Account Creator (w trakcie tworzenia)
Ta funkcja pozwala na założenie kont w Moodle dla użytkowników, którzy nie mają konta ZHP. Dedykowana jest głównie seniorom, którzy przechodzą szkolenie na miejscu i zdają egzamin na komputerze hufca lub chorągwi

Do funkcji przesyła się listę osób do założenia konta (imię, nazwisko i nr ewidencji). Jeśli dane spełniają wymagane krytera - zakładane są konta i zwracane hasła. Jeśli nie - zwracana jest przyczyna błędu.

Funkcja ta jest wołana przez frontend i wymaga uwierzytelnienia przez Entra ID

## Missing certification notifier
Ta funkcja służy do poinformowania jednostek, kto powinien przejść certyfikację i czy już przeszedł. Osoby z przydziałem do hufca i niżej są opisani w raporcie dla hufca, osoby z przydziałem do GK-i lub chorągwi odpowiednio do swojej jednostki

Funkcja jest uruchamiana raz na miesiąc (28 dzień miesiąca). Można też uruchomić ją ręcznie triggerem HTTP. Poprzez trigger należy przekazać body `{"RecipientFilter": "*"}`, aby wysłać do wszystkich. Można też podać tam jakiegoś maila, aby wysłać tylko mail do jednej jednostki.

# Rozwój i deployment
Aplikacja jest napisana w Azure Functions w .NET 8. Wgranie zmiany na branch `master` automatycznie powoduje wgranie jej na produkcję. Za wdrożenie backendu odpowiada GitHub Actions, a frontendu - CloudFlare Pages.