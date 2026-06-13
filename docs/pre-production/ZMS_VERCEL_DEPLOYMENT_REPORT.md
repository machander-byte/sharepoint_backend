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
| Vercel CLI auth | Available as `badugujashwanth-4113` after device auth |
| Production frontend URL | `https://zms-migration-suite.vercel.app` |
| Production URL HTTP check | 200 OK |
| Current full-project Git branch | `master` pushed |
| Application source commit | `af0ae68` |
| Frontend source folder | `ZettalogixMigrationSuite/ZMS.WebUI` |
| Deployment method | Vercel CLI/manual source deploy from the frontend folder |
| Latest production deployment | `zms-migration-suite-jdr5tzaol-badugujashwanth-4113s-projects.vercel.app` |
| Production alias | `https://zms-migration-suite.vercel.app` |
| Build fingerprint shown on clean `/login` | `ZMS frontend build af0ae68` |
| `/login` clean session | Loads V2 login design |
| `/v2` unauthenticated | Redirects to `/login` |
| `/v2/monitor` unauthenticated | Redirects to `/login` |
| `/v2`, session-bearing browser | Loads current UI V2 shell |
| `/v2/command-center`, session-bearing browser | Loads current UI V2 shell |
| Backend API connectivity | Blocked by Render backend failure |
| CORS result | Not verifiable while backend is failing |

## Deployment Gap

The deployed frontend bundle references `https://sharepoint-backend-g5vc.onrender.com` as its backend API base. That backend is failing to start, so deployed API calls cannot be verified.

The Vercel dashboard shows `zms-migration-suite`. The project was briefly connected to the wrong GitHub repository, `badugujashwanth-create/Cricket_chatbot_Backend`; that wrong connection has been removed.

The correct GitHub repository for this working tree is `machander-byte/sharepoint_backend`, but it was not visible in the Vercel Git picker for the current Vercel account/installation. A production deployment was therefore performed with the Vercel CLI from `ZettalogixMigrationSuite/ZMS.WebUI`.

Required Vercel Git target when access is granted:

| Setting | Value |
| --- | --- |
| Repository | `machander-byte/sharepoint_backend` |
| Branch | `master` |
| Root directory | `ZettalogixMigrationSuite/ZMS.WebUI` |
| Install command | `npm ci` |
| Build command | `npm run build` |
| Output directory | `dist` |

## Latest Deployment Verification

| Check | Result |
| --- | --- |
| `vercel pull --yes --environment=production` | Passed from `ZettalogixMigrationSuite/ZMS.WebUI` |
| Production env presence | Required Vite env keys set as encrypted Vercel Production env values |
| Remote production build | Passed |
| Alias update | `https://zms-migration-suite.vercel.app` points to the latest production deployment |
| Old source issue | Fixed for the aliased production frontend |
| Supabase browser client issue | Fixed by switching the SPA client to `@supabase/supabase-js` |
| Browser page errors during latest V2 shell check | 0 page errors observed |

## Required Frontend Env

| Variable | Status |
| --- | --- |
| `VITE_API_BASE_URL` | Deployed bundle points to `https://sharepoint-backend-g5vc.onrender.com` |
| `VITE_SUPABASE_URL` | SET in Vercel Production, value not printed |
| `VITE_SUPABASE_PUBLISHABLE_KEY` | SET in Vercel Production, value not printed |
| `VITE_GOOGLE_CLIENT_ID` | SET in Vercel Production, value not printed |
| `VITE_GOOGLE_API_KEY` | SET in Vercel Production, value not printed |
| `VITE_GOOGLE_APP_ID` | SET in Vercel Production, value not printed |
| `VITE_GOOGLE_DRIVE_SCOPE` | SET to Drive readonly scope |
| `VITE_APP_COMMIT` | SET to `af0ae68` |
| `VITE_APP_BUILD_TIME` | SET in Vercel Production |

## Decision

Vercel old-source issue is fixed. The frontend is deployment-validated for the unauthenticated login route, protected-route redirects, and UI V2 shell rendering. Full API-backed validation remains blocked until the Render backend is healthy.
