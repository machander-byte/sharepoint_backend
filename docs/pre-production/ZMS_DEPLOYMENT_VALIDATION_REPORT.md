# ZMS Deployment Validation Report

Status date: 2026-06-29

## Summary

Latest code is pushed and both frontend and backend are redeployed. Render reports backend commit `03573c7`, and hosted readiness confirms PostgreSQL connectivity and the required schema.

## Deployment Results

| Item | Result |
| --- | --- |
| Frontend app source `master` push | Passed, `8afdb8e9cc1817f804a81710aa1ab51b88fca907` |
| Backend subtree `main` push | Passed, `03573c7911a9d875d61f98285e2442592692fcde` |
| Render live commit | `03573c7911a9d875d61f98285e2442592692fcde` |
| Render backend URL | `https://sharepoint-backend-g5vc.onrender.com` |
| Vercel deployment | Passed, `dpl_GBcUFfeiDb9HTUbJGmSUcbiQD616` |
| Vercel frontend URL | `https://zms-migration-suite.vercel.app` |
| Vercel source folder | `ZettalogixMigrationSuite/ZMS.WebUI` |
| `ZMS_RUN_DB_SCHEMA_INIT` production default | False/skipped on live backend |

## Backend Endpoint Results

| Endpoint | Result |
| --- | --- |
| `/api/version` | 200 OK, commit `03573c7911a9d875d61f98285e2442592692fcde` |
| `/api/health` | 200 Healthy, DB connected, schema ready |
| `/api/status` | 200 Healthy, schema ready, queue empty |
| CORS preflight from Vercel origin | 204, allow-origin returned |
| CORS preflight from unrelated origin | No allow-origin returned |

## Frontend Results

| Check | Result |
| --- | --- |
| `/login` | 200 OK |
| Authenticated V2 pages | Passed walkthrough |
| Legacy `/migrations` | Passed |
| Legacy `/validation` | Passed |
| Legacy `/copilot-readiness` | Passed |
| Legacy `/reports` | Passed |
| Browser console | 0 errors after walkthrough |
| Failed network requests | Not directly captured by available Playwright tool |

## V2 Pages Tested

- `/v2`
- `/v2/command-center`
- `/v2/sources`
- `/v2/destinations`
- `/v2/assess`
- `/v2/plan`
- `/v2/migrate`
- `/v2/monitor`
- `/v2/validate`
- `/v2/reports`
- `/v2/ai-advisor`
- `/v2/governance`
- `/v2/settings`
- `/v2/tutorial`

## Demo Walkthrough Artifacts

| Artifact | Result |
| --- | --- |
| Reviewer walkthrough | Passed for loaded authenticated routes |
| Demo video | Not recorded; current Playwright session has no video recorder |
| Demo script | `docs/pre-production/ZMS_DEMO_VIDEO_SCRIPT.md` |
| Screenshots | Captured under `docs/pre-production/screenshots/` |

## Known Limitations

- Render redeploy is complete.
- Live empty-folder validation requires safe live test approval.
- Stage 2 1,000-file migration is pending.
- Stage 3 10,000-file migration is pending.
- Subscription/payment is not implemented.
- OneDrive, Teams, Exchange, and Box are roadmap items.
- Permission writeback is not certified.
- Metadata writeback is not certified.
- Full ShareGate parity and production-scale certification are not claimed.

## Decision

Ready with limitations for review. Not ready to claim latest backend deployment or live empty-folder certification.
