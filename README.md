# Grosik — finanse domowe

Grosik to lokalna aplikacja do prowadzenia budżetu prywatnego i domowego. Nazwa **Grosik** jest marką widoczną dla użytkownika; kod aplikacji używa przestrzeni nazw `Finance.*`.

## Architektura

- `src/Finance.Domain` — encje, enumy i reguły domenowe bez zależności od EF Core lub ASP.NET Core;
- `src/Finance.Infrastructure` — EF Core, Identity, mapowanie modelu i pojedyncza migracja inicjalna;
- `src/Finance.Api` — kontrolery MVC, kontrakty HTTP, walidatory, managerowie i serwisy bazy danych;
- `src/Finance.Localization` — komunikaty walidacji i inne zasoby `.resx`;
- `frontend` — Vue 3;
- `tests/Finance.Tests` — testy jednostkowe.

API używa klasycznych kontrolerów. Kontrolery obsługują protokół HTTP, walidatory sprawdzają wejście, managerowie mapują/orchestrują przypadki użycia, a serwisy wykonują operacje bazodanowe.

## Wymagania

- .NET SDK 10;
- Node.js 24+;
- Docker Desktop (zalecany dla SQL Servera i Mailpit).

## Uruchomienie lokalne (Debug HTTPS)

1. Utwórz lokalne sekrety API — nie zapisuj ich w plikach wersjonowanych:

   ```powershell
   dotnet user-secrets set "ConnectionStrings:FinanceDb" "Server=localhost,1433;Database=Finance;User Id=sa;Password=<twoje-haslo>;TrustServerCertificate=True;Encrypt=False" --project src/Finance.Api
   dotnet user-secrets set "Jwt:Key" "<losowy-klucz-majacy-co-najmniej-32-znaki>" --project src/Finance.Api
   ```

2. Skopiuj `.env.example` do `.env`, ustaw te same wartości haseł i uruchom zależności: `docker compose up -d sqlserver mailpit`.
3. Zaufaj certyfikatowi deweloperskiemu: `dotnet dev-certs https --trust`.
4. Uruchom profil **Debug (HTTPS)** dla `src/Finance.Api` (adres `https://localhost:7180`) i `npm run dev` w `frontend`.

## Uruchomienie w Dockerze

1. Skopiuj `.env.example` do `.env` i zastąp wszystkie wartości przykładowe unikalnymi sekretami.
2. Uruchom `docker compose up --build`.
3. Frontend będzie dostępny pod `http://localhost:5173`, API pod `http://localhost:5180`, a Mailpit pod `http://localhost:8025`.

Do lokalnego HTTPS API w Dockerze wygeneruj certyfikat i uruchom override opisany w `docker-compose.https.yml`.

## Tryb testowy (InMemory)

Tryb testowy nie wymaga SQL Servera ani Mailpit. Przy każdym uruchomieniu tworzy pustą bazę in-memory i zasiewa ją przykładowym budżetem, kontami, kategoriami oraz transakcjami z bieżącego miesiąca.

1. W bieżącej sesji ustaw dwa lokalne sekrety:

   ```powershell
   $env:Jwt__Key = "<losowy-klucz-majacy-co-najmniej-32-znaki>"
   $env:Testing__DemoPassword = "<unikalne-haslo-testowe>"
   ```

2. Uruchom API: `dotnet run --project src/Finance.Api --launch-profile "Test Mode (InMemory)"`.
3. W drugim terminalu uruchom frontend: `npm run dev --prefix frontend`.
4. Zaloguj się jako `demo@grosik.test`, używając wartości `Testing__DemoPassword`.

Tryb jest aktywny wyłącznie dla środowiska `Testing`; pozostałe środowiska nadal wymagają połączenia SQL Server skonfigurowanego przez `ConnectionStrings:FinanceDb`.

## Baza danych

Baza jest inicjalizowana jedną migracją EF Core w `src/Finance.Infrastructure/Data/Migrations`. Migracje uruchamiaj kontrolowanym krokiem wdrożenia, np. `dotnet ef database update --project src/Finance.Infrastructure --startup-project src/Finance.Api`; API nie wykonuje ich automatycznie podczas startu.
