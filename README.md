# zettalogixmigrationsuite (ZMS)

zettalogixmigrationsuite (ZMS) is an enterprise migration intelligence platform for Microsoft 365 and SharePoint migration work. It covers discovery, risk analysis, readiness scoring, remediation planning, migration wave planning, runbook generation, execution, validation, report export, governance and Copilot readiness, and AI-assisted recommendations with deterministic fallback behavior when Ollama is unavailable.

## Main Features

- Supabase-backed login with protected React routes and JWT-protected ASP.NET Core APIs.
- Dashboard, connection management, source discovery, inventory, risk, permissions, metadata, modernization, Teams foundation, and Copilot readiness views.
- Readiness analysis, remediation recommendations, migration wave planning, runbook generation, pre-migration validation, execution simulation, job monitoring, timeline events, checkpoints, retry, pause, resume, and cancel flows.
- File share, Google Drive, SharePoint Online, and SharePoint On-Prem connector foundations.
- Validation engine, CSV/JSON/Markdown export paths, audit logging, health/version/status endpoints, and AI advisor with Ollama fallback.

## Tech Stack

- Frontend: React 18, TypeScript, Vite, Vitest, Supabase JS, Tailwind-style CSS utilities.
- Desktop wrapper: Electron shell.
- Backend: ASP.NET Core 8, EF Core 8, Supabase JWT validation, Data Protection, rate limiting, CORS, Sentry optional monitoring.
- Database: Supabase Postgres for production; SQLite is supported for local smoke tests and automated tests.
- Tests: Vitest for frontend shell tests; xUnit for backend service, controller, queue, audit, readiness, risk, execution, validation, and user isolation tests.

## Folder Structure

- `ZettalogixMigrationSuite/ZMS.WebUI`: React/Vite web UI.
- `ZettalogixMigrationSuite/ZMS.DesktopApp`: Electron wrapper.
- `sharepoint_backend/ZMS.API`: ASP.NET Core API host.
- `sharepoint_backend/ZMS.Application`: application services and workflow logic.
- `sharepoint_backend/ZMS.Core`: domain models, enums, interfaces, options, security helpers.
- `sharepoint_backend/ZMS.Infrastructure`: EF Core DbContext and repositories.
- `sharepoint_backend/ZMS.MigrationEngine`: background migration queue and processor.
- `sharepoint_backend/ZMS.Connectors.*`: source and target connector implementations.
- `sharepoint_backend/ZMS.Reporting`: report generation.
- `sharepoint_backend/ZMS.Tests`: backend automated tests.
- `source/ZMS.TestDataGenerator`: synthetic migration data generator.
- `docs`: pre-production and product validation notes.

## Local Setup

Prerequisites:

- .NET 8 SDK.
- Node.js and npm.
- Supabase project for authenticated app use.
- Supabase Postgres or another configured Postgres connection string for production-like backend use.
- Optional: Ollama for local AI responses, Google Drive OAuth settings, Microsoft Graph app credentials.

Backend:

```powershell
Set-Location "D:\projects\Shearpoint to google\sharepoint_backend"
$env:ASPNETCORE_URLS = "http://localhost:5206"
$env:Database__Provider = "Postgres"
$env:ConnectionStrings__ZmsDatabase = "Host=your-supabase-pooler-host;Port=5432;Database=postgres;Username=postgres.your-project-ref;Password=your-password;SSL Mode=Require;Trust Server Certificate=true"
$env:DataProtection__KeyStorage = "Database"
$env:Supabase__Auth__Authority = "https://your-project.supabase.co/auth/v1"
$env:Supabase__Auth__Audience = "authenticated"
dotnet run --project .\ZMS.API\ZMS.API.csproj
```

Frontend:

```powershell
Set-Location "D:\projects\Shearpoint to google\ZettalogixMigrationSuite\ZMS.WebUI"
Copy-Item .env.example .env -Force
npm install
npm run dev
```

Open `http://localhost:5173`.

## Environment Variables

See `.env.example` for safe placeholders. Keep backend secrets out of frontend `.env` files. Frontend `VITE_*` values are browser-visible.

Important backend variables:

- `ConnectionStrings__ZmsDatabase`
- `Database__Provider`
- `DataProtection__KeyStorage`
- `DataProtection__KeyRingPath`
- `Supabase__Auth__Authority`
- `Supabase__Auth__Audience`
- `Cors__AllowedOrigins__0`
- `GOOGLE_CLIENT_ID`
- `GOOGLE_CLIENT_SECRET`
- `GOOGLE_REFRESH_TOKEN`
- `Sentry__Dsn`

Important frontend variables:

- `VITE_API_BASE_URL`
- `VITE_SUPABASE_URL`
- `VITE_SUPABASE_PUBLISHABLE_KEY`
- `VITE_GOOGLE_CLIENT_ID`
- `VITE_GOOGLE_API_KEY`
- `VITE_GOOGLE_APP_ID`

## Database Setup

Production/demo deployments should use Supabase Postgres through `ConnectionStrings__ZmsDatabase`. The backend has EF Core mappings, an additive migration, startup schema readiness checks, and optional controlled schema initialization through `ZMS_RUN_DB_SCHEMA_INIT=true` or `Database__RunSchemaInit=true`.

`dotnet ef` is not installed in the current workstation PATH, so database updates were not run through the EF CLI during this pass. Use the app startup schema path or install the EF CLI before running `dotnet ef database update` in an environment with the real database connection string.

## Build And Test

Frontend:

```powershell
npm install
npm run test
npm run build
npm audit --audit-level=high
```

Backend:

```powershell
dotnet restore Zettalogix.MigrationSuite.sln
dotnet build Zettalogix.MigrationSuite.sln --no-restore
dotnet test Zettalogix.MigrationSuite.sln --no-build
dotnet build Zettalogix.MigrationSuite.sln -c Release --no-restore
dotnet list Zettalogix.MigrationSuite.sln package --vulnerable --include-transitive
```

Test data generator:

```powershell
dotnet restore ZMS.TestDataGenerator.csproj
dotnet build ZMS.TestDataGenerator.csproj --no-restore
dotnet list ZMS.TestDataGenerator.csproj package --vulnerable --include-transitive
```

## Deployment Notes

- Frontend can deploy to Vercel or another static host from `ZettalogixMigrationSuite/ZMS.WebUI`.
- Backend can deploy with `sharepoint_backend/render.yaml` and `Dockerfile.api`.
- Set `ConnectionStrings__ZmsDatabase`, `Supabase__Auth__Authority`, and all backend secrets in the hosting provider secret store.
- Add the frontend origin to `Cors__AllowedOrigins`.
- Use persistent Data Protection keys through `DataProtection__KeyStorage=Database` or a durable key ring path.
- Keep frontend `.env` values limited to browser-safe public identifiers.

## Demo Flow

1. Open ZMS and sign in.
2. Review dashboard status and readiness summary.
3. Open connections and confirm source/target configuration.
4. Run or review discovery, inventory, permissions, metadata, and risks.
5. Run readiness analysis and inspect remediation.
6. Generate a migration plan, waves, checklist, and runbook.
7. Create or review migration execution jobs.
8. Exercise job progress, timeline, pause, resume, cancel, and retry controls.
9. Run validation and export report data.
10. Review AI advisor, governance, and Copilot readiness.

## Known Limitations

- Full authenticated browser walkthrough was not rerun in this pass because no reviewer login credentials were available in the workspace.
- `npm run lint` is not available because the frontend package has no lint script.
- Vite reports a single JavaScript chunk above 500 kB after minification; production build still succeeds.
- SharePoint On-Prem and Teams features are foundations/simulations, not complete production connectors.
- Guarded SharePoint live pilot copy remains preview/blocked unless dedicated tenant safety gates are implemented and tested.
