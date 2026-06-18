# ZMS Test Report

Generated: 2026-06-18

## Current Result

ZMS is ready to share as a pre-production review demo with limitations. Latest code is pushed, frontend is redeployed, and the live backend is healthy. Render is still serving the previous backend subtree commit, so the empty-folder backend fix is pushed but not live on Render yet.

## Versions And Deployment

| Area | Result |
| --- | --- |
| Latest GitHub `master` commit | `8afdb8e9cc1817f804a81710aa1ab51b88fca907` |
| Backend subtree pushed to `main` | Yes, `53d6f082c3b1e9618c0e59a4eac54d3a26761a92` |
| Render live commit | `7411998cac1c31cc945bc49b5e5357dd41fc1ab8` |
| Render deployment state | Healthy, but redeploy to `53d6f08` still pending/manual |
| Vercel production deployment | Passed, `dpl_GBcUFfeiDb9HTUbJGmSUcbiQD616` |
| Frontend URL | `https://zms-migration-suite.vercel.app` |
| Frontend asset observed | `assets/index-JcPlGQ7_.js` |

## Local Verification

| Check | Result |
| --- | --- |
| `dotnet build .\Zettalogix.MigrationSuite.sln` | Passed, 0 warnings, 0 errors |
| `dotnet test .\Zettalogix.MigrationSuite.sln --no-build` | Passed, 49/49 |
| `npm ci` | Passed, 0 vulnerabilities |
| `npm test` | Passed, 3/3 |
| `npm run build` | Passed |
| `npm audit --json` | Passed, 0 vulnerabilities |
| `git ls-files -u` | Empty |
| `git diff --check` | Passed |

## Live Backend Endpoint Results

| Endpoint | Result |
| --- | --- |
| `/api/version` | 200 OK, still reports commit `7411998cac1c31cc945bc49b5e5357dd41fc1ab8` |
| `/api/health` | 200 Healthy, DB connected, schema ready |
| `/api/status` | 200 Healthy, schema ready, queue empty |
| CORS preflight from Vercel origin | 204, allowed origin returned |
| CORS preflight from unrelated origin | No allow-origin returned |

## Frontend Browser Smoke

Authenticated browser walkthrough passed with 0 console errors.

Routes loaded:

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
- `/migrations`
- `/validation`
- `/copilot-readiness`
- `/reports`

Network request failure count was not directly exposed by the available Playwright tool. Critical public HTTP checks passed except the direct Vercel deployment URL, which returned 401 due deployment protection; the public alias works.

## Empty-Folder Proof

| Proof Type | Result |
| --- | --- |
| Implementation | Complete in commit `473c0e6` |
| Backend tests | Passed, 49/49 |
| File-share folder enumeration tests | Passed |
| Folder validation test | Passed |
| Live Render proof | Blocked until Render redeploys to subtree commit `53d6f08` |

## Demo Artifacts

- Screenshots captured: Yes, under `docs/pre-production/screenshots/`.
- Demo video recorded: No. The available Playwright session does not expose video recording.
- Demo script: `docs/pre-production/ZMS_DEMO_VIDEO_SCRIPT.md`.

## Known Limitations

- Render backend must be manually redeployed or auto-deploy fixed so it runs subtree commit `53d6f08`.
- Live empty-folder validation is blocked until that redeploy is complete and a safe small source/target test is approved.
- Credential rotation remains required before wider sharing.
- 1,000-file and 10,000-file certifications are pending.
- Subscription/payment is not implemented.
- OneDrive, Teams, Exchange, and Box remain roadmap items.
- Permission and metadata writeback are not certified.
- Full ShareGate parity and production-scale certification are not claimed.
