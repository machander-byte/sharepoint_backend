# ZMS Backend API

This project is the backend service for Zettalogix Migration Suite.

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
$env:Database__Provider = "Sqlite"
$env:ConnectionStrings__ZmsDatabase = "Data Source=.codex-run/local-dev.db"
$env:DataProtection__KeyRingPath = ".codex-run/keys"
dotnet run --project .\ZMS.API\ZMS.API.csproj
```

Supabase JWT validation:

```powershell
$env:Supabase__Auth__Authority = "https://hxptmbphcdyzhmwnimwh.supabase.co/auth/v1"
$env:Supabase__Auth__Audience = "authenticated"
```

The API allows `/` and `/api/health` anonymously. Other `/api` endpoints require a Supabase `Authorization: Bearer <access_token>` header from the frontend login session.

Production secrets belong here or in the hosting provider secret store, never in `ZMS.WebUI/.env`.
