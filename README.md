# Finanse domowe

Lokalna aplikacja do prowadzenia budżetu prywatnego i domowego. Projekt nie zawiera repozytorium Git ani konfiguracji wdrożeniowej.

## Struktura rozwiązania

- `src/Grosik.Api` — REST API, uwierzytelnianie i konfiguracja hosta.
- `src/Grosik.Services` — logika aplikacyjna oraz reguły finansowe.
- `src/Grosik.Data` — encje, kontekst EF Core i migracje SQL Server.
- `frontend` (`grosik-web`) — aplikacja Vue 3.
- `tests/Grosik.Tests` — testy jednostkowe.

## Wymagania

- .NET SDK 10
- Node.js 24 lub nowszy
- Docker Desktop (do lokalnego SQL Servera)

## Uruchomienie lokalne

1. Uruchom SQL Server: `docker compose up -d`.
2. W osobnym terminalu uruchom API: `dotnet run --project src/Grosik.Api`.
3. W osobnym terminalu uruchom interfejs: `npm install` i `npm run dev` w katalogu `frontend`.
4. Otwórz adres podany przez Vite (domyślnie `http://localhost:5173`).

Połączenie API z lokalną bazą jest zdefiniowane w `Finance.Api/appsettings.json`. Przed użyciem poza własnym komputerem zmień hasło SQL Servera i przenieś connection string do sekretów środowiskowych.

## Funkcje MVP

- portfele prywatne i domowe oraz dowolna liczba kont finansowych;
- przychody, wydatki, transfery oraz bieżące salda;
- własne kategorie i podkategorie;
- raport miesięczny;
- import kont, sald otwarcia oraz kategorii z plików Excel.

Import jest dostępny w interfejsie w zakładce **Import Excel**. Najpierw pokazuje podgląd, a dopiero potem zapisuje dane. Formuły i uszkodzone odwołania `#REF!` są pomijane.
