# ZMS Render Deployment Report

Status date: 2026-06-18

## Scope

Backend deployment target for `sharepoint_backend/Zettalogix.MigrationSuite.sln` and `sharepoint_backend/Dockerfile.api`.

## Render Service

| Item | Result |
| --- | --- |
| Service | `sharepoint_backend` |
| Runtime | Docker |
| Repository | `machander-byte/sharepoint_backend` |
| Connected branch | `main` |
| Backend subtree pushed | `53d6f082c3b1e9618c0e59a4eac54d3a26761a92` |
| Backend live commit | `7411998cac1c31cc945bc49b5e5357dd41fc1ab8` |
| Backend URL | `https://sharepoint-backend-g5vc.onrender.com` |
| Current state | Healthy, but latest backend fix is not live yet |

## Local Backend Verification

| Check | Result |
| --- | --- |
| `dotnet build .\Zettalogix.MigrationSuite.sln` | Passed, 0 warnings, 0 errors |
| `dotnet test .\Zettalogix.MigrationSuite.sln --no-build` | Passed, 49/49 |

## Live Endpoint Verification

| Endpoint | Result |
| --- | --- |
| `/api/version` | 200 OK, commit `7411998cac1c31cc945bc49b5e5357dd41fc1ab8` |
| `/api/health` | 200 Healthy, DB connected, schema ready |
| `/api/status` | 200 Healthy, DB connected, schema ready, queue empty |

## Deployment Gap

Latest backend code was pushed to the Render-connected `main` branch as subtree commit `53d6f08`, but the live service still reports `7411998`. Render needs a manual redeploy or auto-deploy correction before the empty-folder backend implementation can be claimed as live.

Secret values were not printed in this report.
