# ZMS Supabase Auth Deployment Report

Status date: 2026-06-14

## Supabase Project

| Item | Result |
| --- | --- |
| Project ref | Redacted; configure with `Supabase__Auth__Authority` and frontend `VITE_SUPABASE_URL`. |
| Frontend Site URL | `https://zms-migration-suite.vercel.app` |
| Redirect URL | `https://zms-migration-suite.vercel.app/auth/callback` present |

## Browser Auth Result

| Check | Result |
| --- | --- |
| Login page | Loaded |
| Google/Supabase sign-in | Passed in browser |
| Authenticated app route | Opened |
| Authenticated `/v2` | Opened |
| Authenticated `/v2/command-center` | Opened |
| Authenticated `/v2/monitor` | Opened |

Reviewer credentials must be shared separately. No passwords are included in this report.

## Security Notes

- Frontend uses publishable/browser-safe Supabase configuration.
- Backend uses database connection only in Render.
- Previously pasted credentials remain `ROTATE REQUIRED` before broader company submission.

## Decision

Supabase Auth is ready for final project / pre-production review.
