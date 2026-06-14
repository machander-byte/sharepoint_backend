# ZMS Render Deployment Report

Status date: 2026-06-14

## Scope

Backend deployment target for `sharepoint_backend/Zettalogix.MigrationSuite.sln` and `sharepoint_backend/Dockerfile.api`.

The full current project was pushed to GitHub `master` at `7f73891`. Because Render is connected to `main` and expects the backend at repository root, the current `sharepoint_backend` subtree was also pushed to GitHub `main` at `7d7d753`.

Deployment fingerprint support is present in the backend at `/api/version` and `/api/status`. The hosted API now exposes the fingerprint even when database startup is degraded.

## Local Verification

| Check | Result |
| --- | --- |
| `dotnet build .\Zettalogix.MigrationSuite.sln` | Passed, 0 warnings, 0 errors |
| `dotnet test .\Zettalogix.MigrationSuite.sln --no-build` | Passed, 46/46 |
| Dockerfile API entrypoint | Uses `dotnet ZMS.API.dll --urls http://0.0.0.0:${PORT:-10000}` |
| Health check path | `/api/health` configured in `render.yaml` |
| Anonymous probes | `/`, `/api/health`, `/api/status`, `/api/version` exist in code |

## Render Service

| Item | Result |
| --- | --- |
| Service | `sharepoint_backend` |
| Service ID | `srv-d7vg6ujeo5us73emrekg` |
| Runtime | Docker |
| Plan | Free |
| Repository | `machander-byte/sharepoint_backend` |
| Connected branch | `main` |
| Deployed source branch | `main` backend subtree from current project |
| Current backend source commit | `7d7d753` |
| Render backend URL | `https://sharepoint-backend-g5vc.onrender.com` |
| Dashboard status | Deployed, degraded |

## Source Verification

| Check | Result |
| --- | --- |
| Render connected repository | `machander-byte/sharepoint_backend` |
| Render connected branch | `main` |
| Backend root in deployed branch | Repository root from `sharepoint_backend` subtree split |
| Docker build path | `Dockerfile.api` at backend subtree root |
| Latest deploy checked | `dep-d8n5o167r5hc73ae6meg` |
| Latest deploy commit shown in Render | `7d7d753` (`Bound database startup diagnostics`) |
| Render build log source proof | Shows `WORKDIR /src`, `COPY . .`, `dotnet restore Zettalogix.MigrationSuite.sln`, and `dotnet publish ZMS.API/ZMS.API.csproj` |
| Old source references | None observed in the latest deploy log |

## Endpoint Verification

| Endpoint | Result |
| --- | --- |
| `/api/health` | 200 OK, `status=Degraded` |
| `/api/status` | 503 Degraded; database connection reports `healthy=true` |
| `/api/version` | 200 OK, `appName=ZMS`, `commit=7d7d753` |

## Failure Evidence

Earlier Render logs showed PostgreSQL authentication failures. The Supabase database password was reset and Render was updated without printing the new value. The current deployed API connects to Postgres successfully.

The stored `ConnectionStrings__ZmsDatabase` value uses the Supabase pooler host, port `6543`, and the scoped Supabase database user.

Remaining blocker: database startup schema initialization exceeds the bounded startup timeout and reports `TimeoutException`. The API stays online in degraded mode, `/api/version` and `/api/health` respond, and `/api/status` confirms database connectivity. API-backed features still need validation after schema initialization is completed or moved to a controlled migration step.

Azure option: the backend uses the generic Npgsql/Postgres provider, so Render can use Azure Database for PostgreSQL by replacing `ConnectionStrings__ZmsDatabase` with an Azure PostgreSQL connection string. Azure Blob Storage is not a drop-in replacement for this database connection.

## Environment Status

`render.yaml` defines the required backend variables with secrets set through Render-managed values:

| Variable | Source status |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | SET in `render.yaml` |
| `Database__Provider` | SET in `render.yaml` |
| `ConnectionStrings__ZmsDatabase` | SET in Render with rotated password; keep rotated again before company submission because an earlier password was pasted |
| `DataProtection__KeyStorage` | SET in `render.yaml` |
| `Supabase__Auth__Authority` | SET in `render.yaml` |
| `Supabase__Auth__Audience` | SET in `render.yaml` |
| `Authorization__EnforceRoles` | SET in Render to `false` for this deployment pass |
| `Cors__AllowedOrigins__0..4` | SET in `render.yaml` |
| `GOOGLE_CLIENT_ID` | Render secret, not opened |
| `GOOGLE_CLIENT_SECRET` | ROTATE REQUIRED, Render secret not opened |
| `GOOGLE_REFRESH_TOKEN` | ROTATE REQUIRED, Render secret not opened |
| `Sentry__Dsn` | Optional; not opened |
| `Sentry__TracesSampleRate` | SET in Render to `0.0` |
| `ZMS_BUILD_COMMIT` | SET in Render to `7d7d753` |
| `ZMS_BUILD_TIME` | SET in Render |

## Code Hardening Added

Startup now runs database initialization in the background and reports the initialization state through `/api/health`, `/api/status`, and `/api/version`. PostgreSQL row-level security is still enabled during schema initialization, but the current production run times out before the startup state reaches `Succeeded`.

## Decision

Render old-source and DB credential issues are fixed. Render backend is reachable but degraded until database startup initialization completes without timeout and authenticated API workflows are verified.
