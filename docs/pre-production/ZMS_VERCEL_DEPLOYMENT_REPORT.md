# ZMS Vercel Deployment Report

Status date: 2026-06-18

## Scope

Frontend deployment target for `ZettalogixMigrationSuite/ZMS.WebUI`.

## Vercel Status

| Item | Result |
| --- | --- |
| Project | `zms-migration-suite` |
| Frontend URL | `https://zms-migration-suite.vercel.app` |
| Source folder | `ZettalogixMigrationSuite/ZMS.WebUI` |
| Deployment method | Vercel CLI/manual source deploy |
| Deployment ID | `dpl_GBcUFfeiDb9HTUbJGmSUcbiQD616` |
| Production deployment URL | `https://zms-migration-suite-jmyxleza4-badugujashwanth-4113s-projects.vercel.app` |
| Public alias | `https://zms-migration-suite.vercel.app` |
| Frontend source commit pushed | `8afdb8e9cc1817f804a81710aa1ab51b88fca907` |
| Backend API base | `https://sharepoint-backend-g5vc.onrender.com` |

## Build Verification

| Check | Result |
| --- | --- |
| Local `npm ci` | Passed, 0 vulnerabilities |
| Local `npm test` | Passed, 3/3 |
| Local `npm run build` | Passed |
| Local `npm audit --json` | 0 vulnerabilities |
| Remote Vercel `npm ci` | Passed, 0 vulnerabilities |
| Remote Vercel build | Passed |
| Vite chunk warning | Present, accepted for demo |

## Browser Verification

| Check | Result |
| --- | --- |
| `/login` public alias | 200 OK |
| Public alias frontend bundle | Latest asset `assets/index-JcPlGQ7_.js` |
| Frontend bundle | Contains Render backend URL |
| Authenticated `/v2` | Loaded |
| Authenticated V2 pages | Loaded |
| Legacy reviewer routes | Loaded |
| Browser console | 0 errors after walkthrough |
| Direct deployment URL | 401, protected by Vercel deployment protection |

## Required Public Vite Values

| Variable | Status |
| --- | --- |
| `VITE_API_BASE_URL` | SET to final Render backend URL for deployment |
| `VITE_SUPABASE_URL` | SET in Vercel project |
| `VITE_SUPABASE_PUBLISHABLE_KEY` | SET in Vercel project |
| `VITE_GOOGLE_CLIENT_ID` | SET in Vercel project |
| `VITE_GOOGLE_API_KEY` | SET in Vercel project |
| `VITE_GOOGLE_APP_ID` | SET in Vercel project |
| `VITE_GOOGLE_DRIVE_SCOPE` | SET in Vercel project |
| `VITE_APP_COMMIT` | Supplied at build; app does not currently surface this value |
| `VITE_APP_BUILD_TIME` | Supplied at build; app does not currently surface this value |

No backend secrets were added to Vercel by this pass.

## Decision

Vercel frontend is ready for review. Backend-dependent empty-folder proof still depends on Render redeploying the pushed backend subtree.
