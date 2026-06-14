# ZMS Vercel Deployment Report

Status date: 2026-06-14

## Scope

Frontend deployment target for `ZettalogixMigrationSuite/ZMS.WebUI`.

## Vercel Status

| Item | Result |
| --- | --- |
| Project | `zms-migration-suite` |
| Frontend URL | `https://zms-migration-suite.vercel.app` |
| Source folder | `ZettalogixMigrationSuite/ZMS.WebUI` |
| Deployment method | Vercel CLI/manual source deploy |
| Final deployment | `zms-migration-suite-mmrudivb2-badugujashwanth-4113s-projects.vercel.app` |
| Public alias | `https://zms-migration-suite.vercel.app` |
| Frontend fingerprint | `694069a` |
| Backend API base | `https://sharepoint-backend-g5vc.onrender.com` |

## Build Verification

| Check | Result |
| --- | --- |
| `npm run build` | Passed |
| Remote Vercel `npm ci` | Passed |
| Remote Vercel build | Passed |
| Vite chunk warning | Present, accepted for demo |
| npm audit | 6 findings: 5 moderate, 1 high |

## Browser Verification

| Check | Result |
| --- | --- |
| `/login` | 200 OK; latest V2 login bundle |
| Frontend bundle | Contains fingerprint `694069a` |
| Frontend bundle | Contains Render backend URL |
| Unauthenticated `/v2` | Redirects to `/login` |
| Unauthenticated `/v2/tutorial` | Redirects to `/login` |
| Unauthenticated `/v2/monitor` | Redirects to `/login` |
| Authenticated `/v2` | Loaded and showed backend runtime `Healthy` |
| Authenticated V2 pages | Loaded |
| Browser console | 0 errors after final walkthrough |

## Required Public Vite Values

| Variable | Status |
| --- | --- |
| `VITE_API_BASE_URL` | SET to final Render backend URL for deployment |
| `VITE_SUPABASE_URL` | SET |
| `VITE_SUPABASE_PUBLISHABLE_KEY` | SET |
| `VITE_GOOGLE_CLIENT_ID` | SET |
| `VITE_GOOGLE_API_KEY` | SET |
| `VITE_GOOGLE_APP_ID` | SET |
| `VITE_GOOGLE_DRIVE_SCOPE` | SET |
| `VITE_APP_COMMIT` | `694069a` |

No backend secrets were added to Vercel by this pass.

## Decision

Vercel frontend is ready for final project / pre-production review.
