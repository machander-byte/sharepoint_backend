# ZMS Render Deployment Report

Status date: 2026-06-14

## Scope

Backend deployment target for `sharepoint_backend/Zettalogix.MigrationSuite.sln` and `sharepoint_backend/Dockerfile.api`.

## Render Service

| Item | Result |
| --- | --- |
| Service | `sharepoint_backend` |
| Service ID | `srv-d7vg6ujeo5us73emrekg` |
| Runtime | Docker |
| Repository | `machander-byte/sharepoint_backend` |
| Connected branch | `main` |
| Backend subtree commit | `7411998cac1c31cc945bc49b5e5357dd41fc1ab8` |
| Backend URL | `https://sharepoint-backend-g5vc.onrender.com` |
| Final state | Healthy |

## Local Verification

| Check | Result |
| --- | --- |
| `dotnet build .\Zettalogix.MigrationSuite.sln` | Passed, 0 warnings, 0 errors |
| `dotnet test .\Zettalogix.MigrationSuite.sln --no-build` | Passed, 46/46 |

## Endpoint Verification

| Endpoint | Result |
| --- | --- |
| `/api/version` | 200 OK, commit `7411998cac1c31cc945bc49b5e5357dd41fc1ab8` |
| `/api/health` | 200 Healthy, DB connected, schema ready |
| `/api/status` | 200 Healthy, DB connected, schema ready, queue empty |

## Fix Applied

The previous degraded state was caused by heavy schema initialization during normal startup. The backend now:

- Starts without running heavy schema creation.
- Gates controlled schema initialization behind `ZMS_RUN_DB_SCHEMA_INIT`.
- Uses bounded read-only schema readiness checks for health/status.
- Serializes and briefly caches schema readiness to avoid duplicate Supabase pooler probes.
- Reports actual Render deployment commit through `RENDER_GIT_COMMIT`.

## Environment Status

| Variable | Status |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | SET |
| `Database__Provider` | SET |
| `ConnectionStrings__ZmsDatabase` | SET |
| `DataProtection__KeyStorage` | SET |
| `Supabase__Auth__Authority` | SET |
| `Supabase__Auth__Audience` | SET |
| `Authorization__EnforceRoles` | SET false for demo |
| `Cors__AllowedOrigins__0` | SET to `https://zms-migration-suite.vercel.app` |
| `ZMS_RUN_DB_SCHEMA_INIT` | false/default |

Secret values are not printed in this report.

## Decision

Render backend is ready for final project / pre-production review.
