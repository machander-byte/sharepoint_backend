# ZMS UI V2 Browser Smoke Test

Status date: 2026-06-14

## Scope

Browser smoke for the deployed final project / pre-production demo:

- `https://zms-migration-suite.vercel.app/login`
- protected `/v2` routes
- authenticated V2 reviewer pages
- backend runtime status shown in V2

## Smoke Results

| Page / Flow | Result |
| --- | --- |
| `/login` | Passed; latest V2 design and fingerprint `694069a` |
| `/v2` unauthenticated | Redirects to `/login` |
| `/v2/tutorial` unauthenticated | Redirects to `/login` |
| `/v2/monitor` unauthenticated | Redirects to `/login` |
| Google/Supabase login | Passed in browser |
| Authenticated `/v2` | Passed; runtime `Healthy` |
| Authenticated `/v2/command-center` | Passed |
| Authenticated `/v2/sources` | Passed |
| Authenticated `/v2/destinations` | Passed |
| Authenticated `/v2/assess` | Passed |
| Authenticated `/v2/plan` | Passed |
| Authenticated `/v2/migrate` | Passed |
| Authenticated `/v2/monitor` | Passed |
| Authenticated `/v2/validate` | Passed |
| Authenticated `/v2/reports` | Passed |
| Authenticated `/v2/ai-advisor` | Passed |
| Authenticated `/v2/governance` | Passed |
| Authenticated `/v2/settings` | Passed |
| Authenticated `/v2/tutorial` | Passed |
| Browser console after walkthrough | 0 errors |
| Subscription/billing UI | Not present |

## Runtime Result

V2 displays:

- Runtime: `Healthy`
- Queue: `Queue empty`
- API: `1.0.0.0`
- Database startup: `Skipped`
- Data source: `Live API`

## Notes

- V2 runtime reads `/api/status` and `/api/version`.
- V2 no longer blocks on optional latest-record endpoints.
- Live domain record counts are shown as zero unless loaded by a dedicated feature page.
- Historical migration evidence remains labeled separately from current live records.
