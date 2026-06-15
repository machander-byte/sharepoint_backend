# ZMS UI V2 Browser Smoke Test

Status date: 2026-06-15

## Scope

Browser smoke for the deployed final project / pre-production demo:

- `https://zms-migration-suite.vercel.app/login`
- protected `/v2` routes
- authenticated V2 reviewer pages
- polished legacy reviewer routes
- backend runtime status shown in V2

## Smoke Results

| Page / Flow | Result |
| --- | --- |
| `/login` | Passed; clean final demo login |
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
| Authenticated `/migrations` | Passed; no raw concatenated labels |
| Authenticated `/validation` | Passed; cards and styled empty tables |
| Authenticated `/copilot-readiness` | Passed; clean discovery-required empty state |
| Authenticated `/reports` | Passed |
| Authenticated `/ai` | Passed |
| Browser console after walkthrough | 0 errors |
| Failed network requests after walkthrough | 0 |
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
- Legacy dashboard, reports, AI, and Copilot pages no longer auto-probe unsupported optional latest-record endpoints on load.
- Live domain record counts are shown as zero unless loaded by a dedicated feature page.
- Historical migration evidence remains labeled separately from current live records.
