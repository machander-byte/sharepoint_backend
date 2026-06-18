# ZMS UI V2 Browser Smoke Test

Status date: 2026-06-18

## Scope

Browser smoke for the deployed pre-production demo at `https://zms-migration-suite.vercel.app`.

## Smoke Results

| Page / Flow | Result |
| --- | --- |
| `/login` | Public alias returns 200 OK; browser session redirected to authenticated dashboard because a session already existed |
| Authenticated `/v2` | Passed |
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
| Authenticated `/migrations` | Passed |
| Authenticated `/validation` | Passed |
| Authenticated `/copilot-readiness` | Passed |
| Authenticated `/reports` | Passed |
| Browser console after walkthrough | 0 errors |
| Failed network requests after walkthrough | Not directly exposed by available Playwright tool |
| Subscription/billing UI | Not present |

## Runtime Result

Live backend `/api/status` reports `Healthy`, database connected, schema ready, queue empty. Live backend `/api/version` still reports old Render commit `7411998`; latest backend subtree `53d6f08` is pushed but not live yet.

## Notes

- V2 runtime reads `/api/status` and `/api/version`.
- Historical migration evidence remains labeled separately from current live records.
- Frontend deployment is current; backend redeploy is still required for latest empty-folder backend code.
