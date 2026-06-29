# ZMS Frontend Web UI

This project is the frontend service for zettalogixmigrationsuite (ZMS).

It owns:

- React/Vite operator UI.
- Connection forms and migration wizard.
- Job monitor and detail views.
- Report download links that call the backend API.
- Public browser-only Google Picker configuration.

Run locally:

```powershell
Set-Location "d:\projects\Shearpoint to google\ZettalogixMigrationSuite\ZMS.WebUI"
Copy-Item .env.example .env -Force
npm install
npm run dev
```

Required frontend API setting:

```env
VITE_API_BASE_URL=http://localhost:5206
VITE_SUPABASE_URL=https://your-project.supabase.co
VITE_SUPABASE_PUBLISHABLE_KEY=your-publishable-key
```

Supabase Auth local settings:

- Site URL: `http://localhost:5173`
- Redirect URLs: `http://localhost:5173/auth/callback`, `http://localhost:5173/*`, `http://127.0.0.1:5173/auth/callback`, `http://127.0.0.1:5173/*`
- Enabled providers: Google OAuth and Email magic links
- OAuth authorization endpoint: `https://your-project.supabase.co/auth/v1/oauth/authorize`
- OAuth token endpoint: `https://your-project.supabase.co/auth/v1/oauth/token`
- JWKS endpoint: `https://your-project.supabase.co/auth/v1/.well-known/jwks.json`
- OIDC discovery: `https://your-project.supabase.co/auth/v1/.well-known/openid-configuration`

Only `VITE_*` values are available in browser code. Do not put backend secrets, database connection strings, SharePoint client secrets, Google client secrets, or refresh tokens in this project.

## Vercel Deployment

Use these project settings:

```text
Root Directory: ZettalogixMigrationSuite/ZMS.WebUI
Framework Preset: Vite
Install Command: npm ci
Build Command: npm run build
Output Directory: dist
```

Set these Vercel environment variables before redeploying:

```env
VITE_API_BASE_URL=https://your-backend-api-host
VITE_SUPABASE_URL=https://your-project.supabase.co
VITE_SUPABASE_PUBLISHABLE_KEY=your-publishable-key
VITE_GOOGLE_CLIENT_ID=
VITE_GOOGLE_API_KEY=
VITE_GOOGLE_APP_ID=
VITE_GOOGLE_DRIVE_SCOPE=https://www.googleapis.com/auth/drive.readonly
```

Add the deployed frontend origin to Supabase Auth redirect URLs:

```text
https://your-vercel-domain/auth/callback
```

If the API is hosted separately, add the same frontend origin to the backend `Cors:AllowedOrigins` configuration.
