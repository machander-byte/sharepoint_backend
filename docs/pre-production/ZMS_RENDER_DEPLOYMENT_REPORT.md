# ZMS Render Deployment Report

Status date: 2026-06-13

## Scope

Backend deployment target for `sharepoint_backend/Zettalogix.MigrationSuite.sln` and `sharepoint_backend/Dockerfile.api`.

The full current project was pushed to GitHub `master` at `af0ae68`. Because Render is connected to `main` and expects the backend at repository root, the current `sharepoint_backend` subtree was also pushed to GitHub `main` at `9de89db`.

Deployment fingerprint support is present in the backend at `/api/version` and `/api/status`, but the hosted API cannot expose it yet because the service fails during database startup.

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
| Current backend source commit | `9de89db` |
| Render backend URL | `https://sharepoint-backend-g5vc.onrender.com` |
| Dashboard status | Failed |

## Source Verification

| Check | Result |
| --- | --- |
| Render connected repository | `machander-byte/sharepoint_backend` |
| Render connected branch | `main` |
| Backend root in deployed branch | Repository root from `sharepoint_backend` subtree split |
| Docker build path | `Dockerfile.api` at backend subtree root |
| Latest clean-cache deploy | `dep-d8mmkspo3t8c73c0fm3g` |
| Latest deploy commit shown in Render | `9de89db` (`Add deployment fingerprints`) |
| Render build log source proof | Shows `WORKDIR /src`, `COPY . .`, `dotnet restore Zettalogix.MigrationSuite.sln`, and `dotnet publish ZMS.API/ZMS.API.csproj` |
| Old source references | None observed in the latest deploy log |

## Endpoint Verification

| Endpoint | Result |
| --- | --- |
| `/api/health` | Timed out because service is failing to start |
| `/api/status` | Not reachable because service is failing to start |
| `/api/version` | Not reachable because service is failing to start |

## Failure Evidence

Recent Render logs from deploy `dep-d8mmkspo3t8c73c0fm3g` show the current API exits with status 134 during startup after PostgreSQL authentication fails. No password value is included in this report.

The stale port issue was corrected in Render: the stored `ConnectionStrings__ZmsDatabase` value now uses the Supabase pooler host, port `6543`, and the scoped Supabase database user. The latest failed log also shows the app is now trying the pooler on `6543`, which proves the Render env update took effect.

Remaining blocker: Supabase still returns `28P01 password authentication failed`. This requires rotating or replacing the database password/connection string in Render, then redeploying. The currently pasted password must be treated as exposed and cannot be accepted for final company submission.

Azure option: the backend uses the generic Npgsql/Postgres provider, so Render can use Azure Database for PostgreSQL by replacing `ConnectionStrings__ZmsDatabase` with an Azure PostgreSQL connection string. Azure Blob Storage is not a drop-in replacement for this database connection.

## Environment Status

`render.yaml` defines the required backend variables with secrets set through Render-managed values:

| Variable | Source status |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | SET in `render.yaml` |
| `Database__Provider` | SET in `render.yaml` |
| `ConnectionStrings__ZmsDatabase` | ROTATE REQUIRED / UPDATE REQUIRED in Render |
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
| `ZMS_BUILD_COMMIT` | SET in Render to `9de89db` |
| `ZMS_BUILD_TIME` | SET in Render |

## Code Hardening Added

Startup now enables PostgreSQL row-level security for the public ZMS tables after schema creation and migrations. This is intended to address Supabase Advisor RLS warnings once the backend can connect to the database.

## Decision

Render old-source issue is fixed. Render backend deployment is not ready until the Supabase/Postgres credential is rotated or replaced and the service starts successfully.
