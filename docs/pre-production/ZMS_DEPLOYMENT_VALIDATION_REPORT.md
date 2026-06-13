# ZMS Deployment Validation Report

Status date: 2026-06-13

## Summary

Local build validation passed and the current project source was pushed to GitHub. Render is now building from the current backend split and the old-source issue is fixed, but the hosted API is still failing at startup because Supabase/Postgres authentication is rejected. Vercel production has been redeployed from the correct frontend folder and the old-source issue is fixed there as well.

## Required Final Response Fields

| Item | Result |
| --- | --- |
| Local backend build | Passed, 0 warnings, 0 errors |
| Local backend tests | Passed, 46/46 |
| Local frontend build | Passed |
| Render deployment status | Failed |
| Render backend URL | `https://sharepoint-backend-g5vc.onrender.com` |
| Current full-project Git branch | `master` pushed |
| Application source commit | `af0ae68` |
| Current Render backend split push | `main` at `9de89db` |
| Latest Render deploy checked | `dep-d8mmkspo3t8c73c0fm3g` |
| Render build log latest commit | Yes, `9de89db` |
| Render old source issue | Fixed |
| Render DB issue | Not fixed; `28P01` authentication failure remains |
| Backend `/api/health` | Timed out / unreachable |
| Backend `/api/status` | Timed out / unreachable |
| Backend `/api/version` | Timed out / unreachable; commit cannot be returned until API starts |
| Vercel deployment status | Production redeployed by Vercel CLI/manual source deploy |
| Vercel frontend URL | `https://zms-migration-suite.vercel.app` |
| Vercel build/source folder | `ZettalogixMigrationSuite/ZMS.WebUI` |
| Vercel build log correct source | Yes, Vite build ran from the frontend project |
| Vercel old source issue | Fixed |
| Login test | Passed; clean session shows V2 login and fingerprint `af0ae68` |
| Unauthenticated `/v2` | Redirects to `/login` |
| Unauthenticated `/v2/monitor` | Redirects to `/login` |
| Authenticated `/v2` test | Loads current UI V2 shell when a Supabase session is present |
| CORS result | Not verifiable while backend is down |
| Supabase Auth result | Login page loads; OAuth flow not completed in this pass |
| Google config result | Drive API and Picker API enabled |
| Microsoft/SharePoint result | Not verified; Microsoft sign-in required |
| Security check result | No secrets added to source; pasted DB password must be rotated before submission; dependency audit has 6 findings |
| UI smoke result | Frontend latest source verified; API-backed pages blocked by Render |

## Known Limitations

- Empty-folder preservation is not complete.
- Stage 2 1,000-file migration is pending.
- Stage 3 10,000-file migration is pending.
- Subscription is not implemented.
- OneDrive, Teams, Exchange, and Box are not implemented as completed migration paths.
- Permission writeback is not certified.
- Metadata writeback is not certified.
- Full ShareGate parity is not claimed.
- Production-scale certification is not claimed.

## Remaining Blockers

- Rotate or replace Render `ConnectionStrings__ZmsDatabase`; current shape is correct for Supabase pooler host, port `6543`, and scoped user, but authentication is rejected.
- Alternative database path: set Render `ConnectionStrings__ZmsDatabase` to a valid Azure Database for PostgreSQL connection string if Supabase Postgres should be replaced. Supabase Auth can remain configured separately for login.
- Redeploy Render backend and verify `/api/health`, `/api/status`, and `/api/version`.
- Grant Vercel Git access to `machander-byte/sharepoint_backend` if future deploys should be Git-triggered. Current production frontend deploy was performed manually through Vercel CLI.
- Recheck CORS after backend and frontend are both live.
- Verify Microsoft Entra Graph permissions and SharePoint target access.
- Resolve or explicitly accept current npm audit findings before company submission.
- Confirm Supabase Advisor RLS findings clear after the backend RLS hardening runs against production.
- Rotate every backend credential pasted during validation before company submission.

## Final Deployment Decision

Not ready.

## Exact Next Action

Rotate the Supabase database password or replace the Render database connection with a valid Azure Database for PostgreSQL connection string, redeploy Render, and verify `/api/version` returns `ZMS` with commit `9de89db`.
