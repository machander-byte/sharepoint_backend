# Zettalogix Migration Suite Backend

This repository contains the backend service for Zettalogix Migration Suite.

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
$env:Database__Provider = "Sqlite"
$env:ConnectionStrings__ZmsDatabase = "Data Source=.codex-run/local-dev.db"
$env:DataProtection__KeyRingPath = ".codex-run/keys"
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

The backend supports `Sqlite`, `SqlServer`, and `Postgres`. For Supabase, configure these on the backend host:

```text
Database__Provider=Postgres
ConnectionStrings__ZmsDatabase=Host=your-supabase-pooler-host;Port=5432;Database=postgres;Username=postgres.your-project-ref;Password=your-password;SSL Mode=Require;Trust Server Certificate=true
```

Use the Supabase Session pooler for long-running ASP.NET Core deployments unless your host supports direct IPv6 database connections.

## Production Secrets

Configure these on the backend host only:

```text
GOOGLE_CLIENT_ID
GOOGLE_CLIENT_SECRET
GOOGLE_REFRESH_TOKEN
ConnectionStrings__ZmsDatabase
DataProtection__KeyRingPath
Cors__AllowedOrigins__0
```

Do not put backend secrets in frontend `.env` files.

## Render Deployment

When deploying to Render with the `render.yaml` configuration, you **must manually set** the `ConnectionStrings__ZmsDatabase` environment variable in Render's dashboard:

1. Go to your web service on Render
2. Navigate to **Environment** settings
3. Add or update the `ConnectionStrings__ZmsDatabase` variable with your Postgres connection string
4. For Supabase: `Host=your-supabase-pooler-host;Port=5432;Database=postgres;Username=postgres.your-project-ref;Password=your-password;SSL Mode=Require;Trust Server Certificate=true`

This variable is marked `sync: false` to prevent the password from being committed to git. The deployment will fail with a clear error if this environment variable is not configured.
