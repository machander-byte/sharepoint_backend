# ZMS UI V2 Browser Smoke Test

Status date: 2026-06-10

## Scope

This pass focused on:

- Merging the `/login` screen into the UI V2 visual design.
- Verifying unauthenticated `/v2` route protection.
- Verifying direct V2 subpath protection with `/v2/monitor`.
- Confirming frontend build integrity after V2 read-only adapter wiring.

Authenticated `/v2` page-by-page browser testing is blocked in this environment because no real Supabase browser session was available. A fake local browser session was not accepted by the Supabase client, and the auth guard was not weakened.

## Browser Environment

| Item | Value |
| --- | --- |
| Frontend URL | `http://127.0.0.1:5173` |
| Viewports checked | 1440x1000, 390x844 |
| Browser console | 0 errors; React Router future-flag warnings only |
| Backend | Not required for login smoke; V2 adapter preserves fallback if API is offline |

## Smoke Results

| Page / Flow | Status | Issue found | Fix applied | Screenshot path | Remaining notes |
| --- | --- | --- | --- | --- | --- |
| `/login` desktop | Passed | Previous login did not match V2 dark premium design | Restyled login as dark V2 access screen with validation evidence panel | Playwright snapshot only | No console errors |
| `/login` mobile | Passed | Previous narrow-layout overflow was already fixed; rechecked after redesign | Kept responsive grid and full-width inputs/buttons | Playwright snapshot only | Page scrolls vertically as expected, no horizontal overflow |
| `/v2` unauthenticated | Passed | None | Existing auth guard preserved | Not captured | Redirects to `/login` |
| `/v2/monitor` unauthenticated | Passed | Exact V2 subpaths were not previously supported | Changed route to `/v2/*` and added V2 subpath page mapping | Not captured | Redirects to redesigned `/login` |
| Command Center authenticated | Blocked | Real Supabase session unavailable | Not applicable | Not captured | Build verifies component compiles |
| Sources authenticated | Blocked | Real Supabase session unavailable | Not applicable | Not captured | Build verifies component compiles |
| Destinations authenticated | Blocked | Real Supabase session unavailable | Not applicable | Not captured | Build verifies component compiles |
| Assess authenticated | Blocked | Real Supabase session unavailable | Not applicable | Not captured | Build verifies component compiles |
| Plan authenticated | Blocked | Real Supabase session unavailable | Not applicable | Not captured | Build verifies component compiles |
| Migrate authenticated | Blocked | Real Supabase session unavailable | Not applicable | Not captured | Build verifies component compiles |
| Monitor authenticated | Blocked | Real Supabase session unavailable | Added read-only snapshot area for live/fallback API data | Not captured | Needs real session for authenticated API reads |
| Validate authenticated | Blocked | Real Supabase session unavailable | Not applicable | Not captured | Build verifies component compiles |
| Reports authenticated | Blocked | Real Supabase session unavailable | Not applicable | Not captured | Build verifies component compiles |
| AI Advisor authenticated | Blocked | Real Supabase session unavailable | Not applicable | Not captured | Build verifies component compiles |
| Governance authenticated | Blocked | Real Supabase session unavailable | Not applicable | Not captured | Build verifies component compiles |
| Settings authenticated | Blocked | Real Supabase session unavailable | Not applicable | Not captured | Build verifies component compiles |

## UI Bugs Fixed

- Login screen now matches the UI V2 dark premium visual language.
- Login screen now shows current evidence: 231/231 files, 0 failures, 0 retries, backend tests 46/46.
- Login screen now displays: `File migration integrity passed. Empty-folder preservation is a known gap.`
- `/v2/*` subpaths are supported; direct links such as `/v2/monitor` route through the V2 shell when authenticated.
- V2 read-only adapter now calls safe APIs through existing frontend services and keeps fallback data if APIs fail.

## APIs Wired Into V2 Adapter

The V2 adapter attempts:

- `/api/health`
- `/api/status`
- `/api/version`
- Connections through existing `zmsApi.getConnections()`
- Latest migration execution job through existing `zmsApi.getLatestMigrationExecutionJob()`
- Latest readiness result through existing `zmsApi.getLatestReadinessAssessment()`
- Latest migration plan through existing `zmsApi.getLatestMigrationPlan()`
- Latest workflow validation through existing `zmsApi.getLatestWorkflowValidation()`
- Reports through existing `zmsApi.getReports()`
- AI recommendations through existing `zmsApi.getAIRecommendations()`

Fallback adapter data remains active when the backend is offline or authenticated calls are unavailable.

## Verification Commands

```powershell
npm run build
dotnet build .\Zettalogix.MigrationSuite.sln
dotnet test .\Zettalogix.MigrationSuite.sln --no-build
```

## Results

| Check | Result |
| --- | --- |
| Frontend build | Passed |
| Backend build | Passed, 0 warnings, 0 errors |
| Backend tests | Passed, 46/46 |

## Remaining Blockers

- Real authenticated Supabase session is required for page-by-page `/v2` browser smoke testing.
- Authenticated V2 read-only API behavior still needs browser verification.
- Route-level lazy loading is still recommended because the Vite chunk-size warning remains.

## 2026-06-13 Deployed Smoke Addendum

| Page / Flow | Result |
| --- | --- |
| `https://zms-migration-suite.vercel.app/login` | Loads |
| Unauthenticated `/v2` | Redirected to `/login` before Supabase browser auth completed |
| Authenticated `/v2` | Loads after browser auth completed |
| Authenticated `/v2/command-center` | Loads, but deployed shell appears stale versus local UI V2 build |
| Backend API data | Blocked because Render backend is failing |
| Console | No warning/error messages captured on the login redirect check |

Deployment note: the local build contains the current UI V2 route integration and was pushed to GitHub `master`; the application source commit is `8f1663d`. The production Vercel deployment remains stale because the current Vercel Git integration cannot see the correct repository, `machander-byte/sharepoint_backend`, and the Vercel CLI token is invalid. Redeploy the frontend after Render backend health is restored and Vercel is connected to the correct repository/root directory.
