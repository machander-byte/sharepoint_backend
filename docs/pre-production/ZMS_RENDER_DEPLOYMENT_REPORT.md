# ZMS Render Deployment Report

Status date: 2026-06-13

## Scope

Backend deployment target for `sharepoint_backend/Zettalogix.MigrationSuite.sln` and `sharepoint_backend/Dockerfile.api`.

The full current project was pushed to GitHub `master` at `8f1663d`. Because Render is connected to `main` and expects the backend at repository root, the current `sharepoint_backend` subtree was also pushed to GitHub `main` at `10c3533`.

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
| Current backend source commit | `10c3533` |
| Render backend URL | `https://sharepoint-backend-g5vc.onrender.com` |
| Dashboard status | Failed |

## Endpoint Verification

| Endpoint | Result |
| --- | --- |
| `/api/health` | Timed out because service is failing to start |
| `/api/status` | Not reachable because service is failing to start |
| `/api/version` | Not reachable because service is failing to start |

## Failure Evidence

Recent Render logs show the current API exits with status 134 during startup after PostgreSQL authentication fails for the configured database user. No password value was printed or copied.

Required action: update or rotate `ConnectionStrings__ZmsDatabase` in Render, then redeploy.

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
| `Authorization__EnforceRoles` | SET in `render.yaml` |
| `Cors__AllowedOrigins__0..4` | SET in `render.yaml` |
| `GOOGLE_CLIENT_ID` | Render secret, not opened |
| `GOOGLE_CLIENT_SECRET` | ROTATE REQUIRED, Render secret not opened |
| `GOOGLE_REFRESH_TOKEN` | ROTATE REQUIRED, Render secret not opened |
| `Sentry__Dsn` | Render secret, not opened |

## Code Hardening Added

Startup now enables PostgreSQL row-level security for the public ZMS tables after schema creation and migrations. This is intended to address Supabase Advisor RLS warnings once the backend can connect to the database.

## Decision

Render backend deployment is not ready until the Supabase/Postgres connection string is updated and the service starts successfully.
