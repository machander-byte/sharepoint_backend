# ZMS Backend API

This project is the backend service for zettalogixmigrationsuite.

It owns:

- ASP.NET Core API endpoints under `/api`.
- EF Core persistence.
- Data Protection secret encryption.
- SharePoint, Google Drive, and file-share connector execution.
- Background migration processing.
- Historical report and log CSV downloads.

Run locally:

```powershell
Set-Location "d:\projects\Shearpoint to google\sharepoint_backend"
$env:ASPNETCORE_URLS = "http://localhost:5206"
$env:Database__Provider = "Postgres"
$env:ConnectionStrings__ZmsDatabase = "Host=your-supabase-pooler-host;Port=5432;Database=postgres;Username=postgres.your-project-ref;Password=your-password;SSL Mode=Require;Trust Server Certificate=true"
$env:DataProtection__KeyStorage = "Database"
dotnet run --project .\ZMS.API\ZMS.API.csproj
```

Production defaults use `DataProtection__KeyStorage=Database` so encrypted connection secrets survive restarts on ephemeral hosts. If the original key ring was already lost, recreate the affected saved connections after redeploying.

Do not commit the real Supabase database password. Keep `ConnectionStrings__ZmsDatabase` in Render environment variables, local PowerShell environment variables, or .NET user secrets.

Supabase JWT validation:

```powershell
$env:Supabase__Auth__Authority = "https://hxptmbphcdyzhmwnimwh.supabase.co/auth/v1"
$env:Supabase__Auth__Audience = "authenticated"
```

The API allows `/`, `/api/health`, `/api/version`, and `/api/status` anonymously for deployment probes. Other `/api` endpoints require a Supabase `Authorization: Bearer <access_token>` header from the frontend login session.

Authorization roles are available as `Viewer`, `Operator`, and `Admin`. Role enforcement is off by default for local compatibility; enable it in production after adding Supabase role claims:

```powershell
$env:Authorization__EnforceRoles = "true"
```

Accepted role claim shapes include standard `role` / `roles` claims and Supabase-style `app_metadata.role`, `app_metadata.roles`, `user_metadata.role`, or `user_metadata.roles`.

Request bodies are capped at 50 MB by default:

```powershell
$env:RequestLimits__MaxBodyBytes = "50000000"
```

Optional Sentry backend monitoring:

```powershell
$env:Sentry__Dsn = "https://examplePublicKey@o0.ingest.sentry.io/0"
$env:Sentry__TracesSampleRate = "0.0"
```

Mutating API calls (`POST`, `PUT`, `PATCH`, `DELETE`) are written to `AuditLogs` with user id, action, status code, IP address, correlation id, and UTC timestamp. Audit writes are best-effort and never block the original request.

Production secrets belong here or in the hosting provider secret store, never in `ZMS.WebUI/.env`.
