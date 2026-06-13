# zettalogixmigrationsuite Backend

This repository contains the backend service for zettalogixmigrationsuite.

It includes:

- ASP.NET Core API under `ZMS.API`.
- EF Core persistence and repositories.
- Background migration engine.
- SharePoint, Google Drive, file-share, and SharePoint On-Prem connector projects.
- Report and log CSV export endpoints.
- Docker and Render backend deployment config.

The React/Vite frontend lives in the separate frontend repository and calls this API through `VITE_API_BASE_URL`.

## Run Locally

```powershell
dotnet restore .\Zettalogix.MigrationSuite.sln
dotnet build .\Zettalogix.MigrationSuite.sln

$env:ASPNETCORE_URLS = "http://localhost:5206"
$env:Database__Provider = "Postgres"
$env:ConnectionStrings__ZmsDatabase = "Host=your-supabase-pooler-host;Port=5432;Database=postgres;Username=postgres.your-project-ref;Password=your-password;SSL Mode=Require;Trust Server Certificate=true"
$env:DataProtection__KeyStorage = "Database"
$env:Cors__AllowedOrigins__0 = "http://localhost:5173"
dotnet run --project .\ZMS.API\ZMS.API.csproj
```

## Migration Reliability Defaults

The API is configured to use Microsoft Graph upload sessions for files larger than 10 MB, with 6.25 MB chunks. This keeps demos and production runs on the resumable upload path before the Graph simple-upload 250 MB ceiling.

Useful backend settings: 

```text
MigrationEngine__LargeFileUploadThresholdBytes=10485760
MigrationEngine__UploadChunkSizeBytes=6553600
MigrationEngine__RetryBaseDelayMilliseconds=1000
MigrationEngine__RetryMaxDelayMilliseconds=30000
MigrationEngine__ResumeQueuedJobsOnStartup=true
```

When the API restarts, queued/running jobs are re-queued and in-progress items are returned to the retry queue. SharePoint Online app-only connections must have Microsoft Graph application permissions `Sites.ReadWrite.All` and `Files.ReadWrite.All` with admin consent.

## Supabase Postgres

The backend runtime is configured for Supabase Postgres. Configure these on the backend host:

```text
Database__Provider=Postgres
ConnectionStrings__ZmsDatabase=Host=your-supabase-pooler-host;Port=5432;Database=postgres;Username=postgres.your-project-ref;Password=your-password;SSL Mode=Require;Trust Server Certificate=true
```

Use the Supabase Session pooler for long-running ASP.NET Core deployments unless your host supports direct IPv6 database connections.

Do not commit the real Supabase database password. Put `ConnectionStrings__ZmsDatabase` in Render environment variables, local PowerShell environment variables, or .NET user secrets.

## Production Secrets

Configure these on the backend host only:

```text
GOOGLE_CLIENT_ID
GOOGLE_CLIENT_SECRET
GOOGLE_REFRESH_TOKEN
ConnectionStrings__ZmsDatabase
DataProtection__KeyStorage
Cors__AllowedOrigins__0
Sentry__Dsn
```

Do not put backend secrets in frontend `.env` files.

Use `DataProtection__KeyStorage=Database` on ephemeral hosts such as Render free services. If you choose filesystem key storage instead, set `DataProtection__KeyRingPath` to a durable folder shared by every API/worker instance. Do not point the key ring at `/tmp`, because losing those keys makes existing saved connection secrets unreadable.

## Security And Monitoring

Public operational endpoints:

```text
GET /api/health
GET /api/version
GET /api/status
```

All other API routes require Supabase JWT authentication. The backend supports `Viewer`, `Operator`, and `Admin` role checks. Set `Authorization__EnforceRoles=true` after adding role claims to Supabase user metadata or app metadata.

Role hierarchy:

```text
Viewer: read dashboards, discovery results, plans, reports, validation, status.
Operator: Viewer plus create/import/analyze/plan/execute/retry workflow actions.
Admin: Operator plus destructive/admin demo and connection delete actions.
```

Structured request logging is enabled with correlation IDs and without request bodies, query strings, headers, or tokens. Sentry is optional and activates only when `Sentry__Dsn` or `SENTRY_DSN` is configured.

## Render Deployment

When deploying to Render with the `render.yaml` configuration, you **must manually set** the `ConnectionStrings__ZmsDatabase` environment variable in Render's dashboard:

1. Go to your web service on Render
2. Navigate to **Environment** settings
3. Add or update the `ConnectionStrings__ZmsDatabase` variable with your Postgres connection string
4. For Supabase: `Host=your-supabase-pooler-host;Port=5432;Database=postgres;Username=postgres.your-project-ref;Password=your-password;SSL Mode=Require;Trust Server Certificate=true`

This variable is marked `sync: false` to prevent the password from being committed to git. The deployment will fail with a clear error if this environment variable is not configured.

The Render blueprint sets `DataProtection__KeyStorage=Database` so ASP.NET Core Data Protection keys are stored in the configured database. If a previous deployment used `/tmp/dataprotection-keys`, redeploying fixes future secrets, but connections saved with the lost key ring must be recreated.
