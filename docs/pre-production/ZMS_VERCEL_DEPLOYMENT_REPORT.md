# ZMS Vercel Deployment Report

Status date: 2026-06-13

## Scope

Frontend deployment target for `ZettalogixMigrationSuite/ZMS.WebUI`.

## Local Verification

| Check | Result |
| --- | --- |
| `npm ci` | Passed |
| `npm run build` | Passed |
| Vite chunk warning | Present, accepted for this pass |
| `vercel.json` install command | `npm ci` |
| `vercel.json` build command | `npm run build` |
| `vercel.json` output directory | `dist` |
| SPA rewrite | Present |

`npm ci` and `npm audit` reported 6 dependency vulnerabilities: 5 moderate and 1 high. Fixes require dependency updates, including a Vite/esbuild major-version path for the high advisory.

## Vercel Status

| Item | Result |
| --- | --- |
| Vercel dashboard access | Available in browser |
| Vercel CLI | Available through `npx vercel` |
| Vercel CLI auth | Blocked: configured token is invalid |
| Production frontend URL | `https://zms-migration-suite.vercel.app` |
| Production URL HTTP check | 200 OK |
| `/login` | Loads |
| `/v2` authenticated | Loads, but deployed shell appears stale versus local UI V2 build |
| `/v2/command-center` authenticated | Loads, but deployed shell appears stale versus local UI V2 build |
| Backend API connectivity | Blocked by Render backend failure |
| CORS result | Not verifiable while backend is failing |

## Deployment Gap

The deployed frontend bundle currently references `https://sharepoint-backend-g5vc.onrender.com` as its backend API base. That backend is failing to start, so deployed API calls cannot be verified.

The Vercel dashboard shows `zms-migration-suite`, but CLI deployment is blocked by an invalid token. The dashboard also shows Git connection work is still needed for that project. A new production deployment was not performed from this session.

## Required Frontend Env

| Variable | Status |
| --- | --- |
| `VITE_API_BASE_URL` | Deployed bundle points to `https://sharepoint-backend-g5vc.onrender.com` |
| `VITE_SUPABASE_URL` | Runtime auth worked in browser, value not printed |
| `VITE_SUPABASE_PUBLISHABLE_KEY` | Runtime auth worked in browser, value not printed |
| `VITE_GOOGLE_CLIENT_ID` | Not verified in dashboard |
| `VITE_GOOGLE_API_KEY` | Not verified in dashboard |
| `VITE_GOOGLE_APP_ID` | Not verified in dashboard |
| `VITE_GOOGLE_DRIVE_SCOPE` | Present in bundled code as Drive readonly scope |

## Decision

Vercel frontend is reachable but not deployment-validated. Redeploy after the Render backend is healthy and either fix Vercel CLI auth or connect the project to the correct Git repository/branch.
