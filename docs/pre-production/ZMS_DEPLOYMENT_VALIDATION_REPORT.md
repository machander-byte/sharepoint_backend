# ZMS Deployment Validation Report

Status date: 2026-06-13

## Summary

Local build validation passed, but hosted deployment is not ready. Render backend is failing at startup because Supabase/Postgres authentication is rejected. Vercel frontend is reachable, but it is stale versus the local UI V2 build and cannot validate API calls while the backend is down.

## Required Final Response Fields

| Item | Result |
| --- | --- |
| Local backend build | Passed, 0 warnings, 0 errors |
| Local backend tests | Passed, 46/46 |
| Local frontend build | Passed |
| Render deployment status | Failed |
| Render backend URL | `https://sharepoint-backend-g5vc.onrender.com` |
| Backend `/api/health` | Timed out / unreachable |
| Backend `/api/status` | Unreachable |
| Backend `/api/version` | Unreachable |
| Vercel deployment status | Existing deployment reachable, new deployment not performed |
| Vercel frontend URL | `https://zms-migration-suite.vercel.app` |
| Login test | Passed |
| Authenticated `/v2` test | Loads, but deployed shell appears stale versus local UI V2 build |
| CORS result | Not verifiable while backend is down |
| Supabase Auth result | Site URL and redirect URLs verified; project healthy |
| Google config result | Drive API and Picker API enabled |
| Microsoft/SharePoint result | Not verified; Microsoft sign-in required |
| Security check result | Secrets not printed; dependency audit has 6 findings; RLS hardening added |
| UI smoke result | Frontend shell loads; API-backed pages not fully verified |

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

- Rotate or update Render `ConnectionStrings__ZmsDatabase`.
- Redeploy Render backend and verify `/api/health`, `/api/status`, and `/api/version`.
- Fix Vercel CLI token or connect `zms-migration-suite` to the correct Git repository/branch.
- Redeploy Vercel from the current local build.
- Recheck CORS after backend and frontend are both live.
- Verify Microsoft Entra Graph permissions and SharePoint target access.
- Resolve or explicitly accept current npm audit findings before company submission.
- Confirm Supabase Advisor RLS findings clear after the backend RLS hardening runs against production.

## Final Deployment Decision

Not ready.

## Exact Next Action

Update/rotate the Supabase pooler password in Render `ConnectionStrings__ZmsDatabase`, redeploy the Render backend, and verify `/api/health` before redeploying the frontend.
