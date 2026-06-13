# OAuth And Environment Readiness

This project uses Supabase for user login and Supabase Postgres for backend persistence.

Do not move back to SQL Server for production validation.

## Frontend Environment

Configure these in Vercel and local `ZMS.WebUI/.env` as needed:

```text
VITE_API_BASE_URL=https://your-render-api-host
VITE_SUPABASE_URL=https://your-project.supabase.co
VITE_SUPABASE_PUBLISHABLE_KEY=your-public-publishable-key
```

Google Picker values are browser-safe but should still be restricted in Google Cloud:

```text
VITE_GOOGLE_CLIENT_ID=your-google-oauth-client-id
VITE_GOOGLE_API_KEY=your-google-picker-api-key
VITE_GOOGLE_APP_ID=your-google-cloud-project-number
VITE_GOOGLE_DRIVE_SCOPE=https://www.googleapis.com/auth/drive.readonly
```

## Backend Environment

Configure these in Render or local PowerShell for live connector tests:

```text
ASPNETCORE_ENVIRONMENT=Production
Database__Provider=Postgres
ConnectionStrings__ZmsDatabase=Host=your-supabase-pooler-host;Port=5432;Database=postgres;Username=postgres.your-project-ref;Password=your-password;SSL Mode=Require;Trust Server Certificate=true
DataProtection__KeyStorage=Database
Supabase__Auth__Authority=https://your-project.supabase.co/auth/v1
Supabase__Auth__Audience=authenticated
Cors__AllowedOrigins__0=https://zms-migration-suite.vercel.app
```

Google Drive backend connector credentials:

```text
GOOGLE_CLIENT_ID=your-google-oauth-client-id
GOOGLE_CLIENT_SECRET=your-google-oauth-client-secret
GOOGLE_REFRESH_TOKEN=your-offline-refresh-token
```

Optional production hardening:

```text
Authorization__EnforceRoles=true
Sentry__Dsn=your-sentry-dsn
Sentry__TracesSampleRate=0.0
```

Only set this for approved test-tenant pilot runs:

```text
ZMS_ENABLE_LIVE_MIGRATION=true
```

## Supabase Auth

In Supabase:

- Set the production Site URL to the Vercel URL.
- Add `https://zms-migration-suite.vercel.app/auth/callback` to allowed redirect URLs.
- Add local callback URLs for development only when needed.
- If role enforcement is enabled, add `Viewer`, `Operator`, or `Admin` in user metadata or app metadata.

The frontend sends the Supabase JWT as `Authorization: Bearer <token>` to the backend.

## Google Configuration

For Google Drive source migration:

- Enable the Google Drive API.
- Configure the OAuth consent screen.
- Add test users while the Google app is in testing.
- Publish the Google OAuth app before company review if external users will authenticate.
- Generate an offline refresh token for the backend connector account.
- Share the source Google Drive folder with the account represented by the refresh token.

For Google Picker in the browser:

- Add the Vercel origin under Authorized JavaScript origins.
- Restrict the API key to the Vercel origin and Google Picker/Drive APIs.
- For local validation, add `http://127.0.0.1:5173` and `http://localhost:5173` as authorized browser origins/referrers.
- Enable both `Google Drive API` and `Google Picker API` in the Google Cloud project.

The current backend does not implement `/api/auth/google/callback`. Do not configure that redirect URI unless an OAuth callback endpoint is added.

## Current Validation Status - 2026-06-09

Browser-side Google Drive folder selection is validated locally:

- Google OAuth origin mismatch for `http://127.0.0.1:5173` was resolved.
- A restricted Google browser API key was configured for the frontend.
- `Google Picker API` was enabled in Google Cloud.
- The key is restricted to Google Drive/Picker APIs and local/Vercel browser referrers.
- The Google Picker opens, lists Drive folders, and populates the ZMS Google Drive connection form with the selected folder URL.

Backend live connection validation was proven during the 2026-06-08 live migration:

- Google Drive source connection worked.
- SharePoint Online target connection worked.
- Supabase-backed job state worked.
- SharePoint upload and Microsoft Graph target verification worked.

The current 2026-06-09 shell cannot restart the backend because these backend-only variables are not present in the process environment:

```text
ConnectionStrings__ZmsDatabase
GOOGLE_CLIENT_ID
GOOGLE_CLIENT_SECRET
GOOGLE_REFRESH_TOKEN
MICROSOFT_TENANT_ID
MICROSOFT_CLIENT_ID
MICROSOFT_CLIENT_SECRET
```

The local frontend currently points at:

```text
VITE_API_BASE_URL=http://localhost:5206
```

If `http://localhost:5206/api/health` is not reachable, the Connections page will show backend fetch failures even though browser-side Google Picker is working.

Do not place refresh tokens, Google client secrets, Supabase database passwords, or Microsoft client secrets in `ZMS.WebUI/.env`.

## Secret Rotation Required

Several backend-only credentials and tokens were pasted into the validation conversation while debugging. Treat those values as exposed and rotate them before company submission:

- Google OAuth client secret.
- Google refresh token.
- Google access token.
- Supabase database password.
- Microsoft client secret.

After rotation, update only Render/local backend environment variables and stored connection profiles. Do not write rotated values into frontend env files, source code, docs, screenshots, or generated reports.

## Microsoft Configuration

For SharePoint Online app-only migration:

- Create an Entra ID app registration.
- Create a client secret and record the secret value securely.
- Grant Microsoft Graph application permissions:
  - `Files.Read.All`
  - `Files.ReadWrite.All`
  - `Sites.Read.All`
  - `Sites.ReadWrite.All`
- Grant admin consent.
- Use the tenant ID, client ID, client secret, SharePoint site URL, and document library name when creating the SharePoint Online connection in ZMS.

`offline_access` and `User.Read` are delegated OAuth permissions. They are not required for the current app-only SharePoint Online connector unless delegated Microsoft login is added later.

## Local Key Check

From PowerShell, check whether the current process has live-test environment variables:

```powershell
$names = 'GOOGLE_CLIENT_ID','GOOGLE_CLIENT_SECRET','GOOGLE_REFRESH_TOKEN','ConnectionStrings__ZmsDatabase','Supabase__Auth__Authority','Supabase__Auth__Audience','Sentry__Dsn'
foreach ($name in $names) {
  $value = [Environment]::GetEnvironmentVariable($name)
  if ([string]::IsNullOrWhiteSpace($value)) { "$name=MISSING" } else { "$name=SET" }
}
```

Do not paste secret values into chat or commit them to source.
