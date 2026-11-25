# Grosik

Grosik is a personal and household budgeting application. **Grosik** is the user-facing product name; the backend code uses the `Finance.*` namespace family.

## Architecture

- `src/Finance.Domain` — domain entities, enums, and business rules with no EF Core or ASP.NET Core dependencies.
- `src/Finance.Infrastructure` — EF Core persistence, ASP.NET Core Identity, model configuration, and database migrations.
- `src/Finance.Api` — HTTP contracts, MVC controllers, validators, managers, and application services.
- `src/Finance.Localization` — localized API response and validation-message resources.
- `frontend` — Vue 3 single-page application.
- `tests/Finance.Tests` — automated unit and integration tests.

Controllers handle HTTP concerns, validators check request input, managers coordinate use cases and response mapping, and services perform persistence operations.

## Prerequisites

- .NET SDK 10
- Node.js 24 or later
- Docker Desktop, recommended for SQL Server and Mailpit

## Local development

1. Configure local API secrets. Do not commit them to source control:

   ```powershell
   dotnet user-secrets set "ConnectionStrings:FinanceDb" "Server=localhost,1433;Database=Finance;User Id=sa;Password=<your-password>;TrustServerCertificate=True;Encrypt=False" --project src/Finance.Api
   dotnet user-secrets set "Jwt:Key" "<a-random-secret-with-at-least-32-characters>" --project src/Finance.Api
   ```

2. Copy `.env.example` to `.env`, set the corresponding local values, and start the dependencies:

   ```powershell
   docker compose up -d sqlserver mailpit
   ```

3. Trust the development certificate:

   ```powershell
   dotnet dev-certs https --trust
   ```

4. Start the API with the **Debug (HTTPS)** profile (`https://localhost:7180`) and start the frontend:

   ```powershell
   npm run dev --prefix frontend
   ```

## Docker

1. Copy `.env.example` to `.env` and replace every placeholder with a unique secret.
2. Start the application:

   ```powershell
   docker compose up --build
   ```

The frontend is available at `http://localhost:5173`, the API at `http://localhost:5180`, and Mailpit at `http://localhost:8025`.

For HTTPS in Docker, generate a development certificate and use the configuration in `docker-compose.https.yml`.

## Test mode (in-memory database)

Test mode does not require SQL Server or Mailpit. Each application start creates a fresh in-memory database and seeds a demo budget, accounts, categories, and transactions for the current month.

1. Set the required local values in the current PowerShell session:

   ```powershell
   $env:Jwt__Key = "a-random-secret-with-at-least-32-characters"
   $env:Testing__DemoPassword = "DemoPassword123"
   ```

2. Start the API:

   ```powershell
   dotnet run --project src/Finance.Api --launch-profile "Test Mode (InMemory)"
   ```

3. In a second terminal, start the frontend:

   ```powershell
   npm run dev --prefix frontend
   ```

4. Sign in with `demo@grosik.test` and the value configured in `Testing__DemoPassword`.

Test mode is enabled only when `ASPNETCORE_ENVIRONMENT` is `Testing`. All other environments require `ConnectionStrings:FinanceDb` to be configured.

## Localization

The API defaults to English and supports Polish. Validation and response messages use the request culture selected in this order:

1. `?culture=en` or `?culture=pl`
2. The standard ASP.NET Core culture cookie
3. The `Accept-Language` request header

Frontend requests should send the selected language as `Accept-Language` so API validation and response messages match the user interface. Localized texts are maintained in `src/Finance.Localization/Resources`.

## Database migrations

The database is initialized by the migration in `src/Finance.Infrastructure/Data/Migrations`. Apply migrations as a controlled deployment step; the API does not apply migrations when it starts:

```powershell
dotnet ef database update --project src/Finance.Infrastructure --startup-project src/Finance.Api
```

## Verification

Run the backend test suite with:

```powershell
dotnet test tests/Finance.Tests/Finance.Tests.csproj
```
