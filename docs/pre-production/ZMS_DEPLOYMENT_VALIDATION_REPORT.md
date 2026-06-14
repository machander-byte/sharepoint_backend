# ZMS Deployment Validation Report

Status date: 2026-06-14

## Summary

The deployed ZMS final project / pre-production demo is now reachable and healthy.

- Render backend is deployed from the current backend subtree and reports `Healthy`.
- Vercel frontend is deployed from `ZettalogixMigrationSuite/ZMS.WebUI`.
- Public frontend alias points to the latest Vercel deployment.
- Supabase/Google login was verified in the browser.
- Authenticated V2 reviewer pages loaded with 0 browser console errors.

## Deployment Results

| Item | Result |
| --- | --- |
| Full repo branch | `master` pushed |
| Full repo latest commit | `694069a` |
| Backend source latest commit | `669aba1` |
| Render backend subtree commit | `7411998cac1c31cc945bc49b5e5357dd41fc1ab8` |
| Frontend fingerprint | `694069a` |
| Render backend URL | `https://sharepoint-backend-g5vc.onrender.com` |
| Vercel frontend URL | `https://zms-migration-suite.vercel.app` |
| Vercel source folder | `ZettalogixMigrationSuite/ZMS.WebUI` |
| Render old source issue | Fixed |
| Vercel old source issue | Fixed |
| Backend schema startup timeout | Fixed by removing heavy init from normal startup |
| `ZMS_RUN_DB_SCHEMA_INIT` production default | False |

## Backend Endpoint Results

| Endpoint | Result |
| --- | --- |
| `/api/version` | 200 OK, commit `7411998cac1c31cc945bc49b5e5357dd41fc1ab8` |
| `/api/health` | 200 Healthy, DB connected, schema ready |
| `/api/status` | 200 Healthy, schema `Ready`, queue empty |

## Frontend Results

| Check | Result |
| --- | --- |
| `/login` | 200 OK, latest bundle fingerprint `694069a` |
| `/v2` unauthenticated | Redirects to `/login` |
| `/v2/tutorial` unauthenticated | Redirects to `/login` |
| `/v2/monitor` unauthenticated | Redirects to `/login` |
| Login | Passed with Google/Supabase browser flow |
| Authenticated `/v2` | Loaded and showed runtime `Healthy` |
| V2 pages | Passed walkthrough |
| Browser console | 0 errors after final walkthrough |
| CORS | Passed for final Vercel origin |

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

## Known Limitations

- Empty-folder preservation is not complete.
- Stage 2 1,000-file migration is pending.
- Stage 3 10,000-file migration is pending.
- Subscription/payment is not implemented.
- OneDrive, Teams, Exchange, and Box are roadmap items.
- Permission writeback is not certified.
- Metadata writeback is not certified.
- Full ShareGate parity is not claimed.
- Production-scale certification is not claimed.

## Security Notes

- No secret values were added to docs, source, or frontend code in this pass.
- Previously pasted credentials remain `ROTATE REQUIRED` before broader company submission.
- Vercel build was explicitly deployed with the public backend URL `VITE_API_BASE_URL=https://sharepoint-backend-g5vc.onrender.com`.

## Final Deployment Decision

Ready for final project review as a pre-production demo, with the documented limitations.
