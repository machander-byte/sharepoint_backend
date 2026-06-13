# ZMS Full Feature Test Matrix

Status date: 2026-06-10

This matrix records current evidence from automated tests, builds, browser smoke checks, and live migration documents. It does not claim production readiness.

| Feature | Test case | Expected result | Actual result | Status | Issue found | Fix applied | Evidence | Remaining gap |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Auth | `/login` renders | Login page loads with usable OAuth/email controls | Login loads on desktop and mobile with 0 console errors | Passed | Login did not match V2 design | Restyled login to V2 dark design | Browser smoke, `npm run build` | Real Google/Supabase sign-in not executed |
| Auth | `/v2` protected route | Unauthenticated user is redirected to `/login` | `/v2` redirects to `/login` | Passed | None | None | Browser smoke | Authenticated V2 walkthrough still required |
| Auth | `/v2/monitor` direct route | Direct V2 subpath honors auth guard | `/v2/monitor` redirects to `/login` when unauthenticated | Passed | Exact V2 subpaths were not supported before this pass | Changed route to `/v2/*` and mapped V2 page ids | Browser smoke | Authenticated direct-subpath test pending |
| RBAC | Viewer/operator/admin route/API behavior | Protected APIs enforce auth and role policies | Backend auth/RBAC policies compile; unauthenticated protected APIs previously returned 401 | Partial | Real role-claim matrix not run | None | `Program.cs`, controller policies, build | Real JWT role matrix needed |
| Connections | List/test/create/update/delete connections | Connections work with credential redaction | Code and APIs present; user isolation tests pass | Partial | Authenticated API/browser flow not rerun | None | `UserIsolationTests`, `SecretRedactorTests` | Real authenticated connection CRUD/test needed |
| Command Center / Dashboard | Existing dashboard and V2 command center compile | Dashboards load without broken imports | Frontend build passed; V2 Command Center component compiles | Partial | Authenticated V2 visual smoke blocked | None | `npm run build` | Real session browser walkthrough |
| Discovery | Config/live/import discovery and exports | Inventory, folders, permissions, metadata, risks are available | Discovery services/controllers compile; tests cover live scanner fallback and throttling | Partial | No authenticated discovery browser/API run in this pass | None | `LiveGraphDiscoveryScannerTests`, build | Real discovery run/export open check |
| Readiness | Analyze readiness and risk scoring | Score, blockers, warnings, and risk categories generated | Readiness tests pass | Passed by automated tests | None | None | `ReadinessAnalysisTests`, `RiskScoringTests` | Real browser/API smoke still needed |
| Remediation | Remediation suggestions grouped by category | Suggestions include priority, owner/action fields | Remediation planner tests pass | Passed by automated tests | None | None | `ReadinessAnalysisTests.RemediationPlanner_GroupsFindingsByCategory` | Export/open verification pending |
| Migration Planner | Create plan, validate plan, generate waves | Plan and waves generated from readiness | Planner tests pass | Passed by automated tests | None | None | `MigrationPlanTests` | Authenticated UI flow pending |
| Runbook generation | Generate markdown runbook | Runbook markdown is produced | Runbook generator test passes | Passed by automated tests | None | None | `MigrationPlanTests.MigrationRunbookGenerator_ProducesPlanningRunbookMarkdown` | Open/export verification pending |
| Pre-migration validation | Go/Conditional Go/No-Go behavior | Required failures create No-Go | Pre-migration tests pass | Passed by automated tests | None | None | `PreMigrationTests` | Authenticated validation export pending |
| Live migration reports | Stage 0 and Stage 1 evidence | File counts, retries, failures, and bytes are accurate | Stage 0 22/22 and Stage 1 231/231 passed with 0 failures and 0 retries | Passed for completed stages | Empty folders are not first-class migration objects | Limitation displayed in V2/login/docs | Stage 0/Stage 1 docs | Stage 2 1,000-file run pending |
| Reports/export | Export reports and open files | CSV/Markdown/JSON exports open and contain no secrets | Export code/build present; frontend CSV utility emits BOM/CRLF | Partial | Authenticated download/open not run | CSV utility hardened | `npm run build`, report code review | Download/open each export from real session |
| AI Advisor | Recommendation labels and fallback behavior | Rule-based/fallback/real-AI labels are honest | V2 labels real AI as not claimed by default; service fallback exists | Partial | Real Ollama/backend AI not verified | V2 wording kept conservative | V2 AI page, `AiAdvisorService` | AI backend availability test pending |
| Governance | Oversharing/external/broken inheritance/Copilot readiness | Governance risks visible as readiness inputs | V2 Governance page compiles; Copilot readiness API surface exists | Partial | Authenticated browser/API test not run | None | `npm run build`, controllers | Real governance data validation needed |
| Operator Control Center | Workflow timeline/artifacts/warnings | Workflow artifacts and issues displayed | Workflow validation tests pass | Passed by automated tests | None | None | `WorkflowValidationTests` | Authenticated OCC browser test pending |
| Monitoring/Sentry | Health/status/version and controlled error capture | Health endpoints work; Sentry captures safe test event when configured | V2 adapter calls health/status/version; controlled Sentry capture not run | Partial | No Sentry DSN/test event in this pass | V2 adapter fallback added | `npm run build`, health controller | Controlled Sentry capture pending |
| Audit logs | Mutating API audit log | User/action/resource fields recorded; no secrets logged | Audit middleware tests pass | Passed by automated tests | None | None | `AuditLoggingMiddlewareTests` | Query/export audit records in live environment |
| Internal safety limits | Live pilot gates and max file limit | Live migration disabled unless safety gates pass | Safety gate tests pass; V2 uses internal safety-limit wording | Passed by automated tests | Subscription/billing wording avoided in V2 source | V2 wording scan/fix | `SharePointMigrationAdapterTests`, V2 source scan | Real live-pilot dry safety check pending |
| UI V2 | `/v2` and V2 pages compile | V2 route exists and current UI remains unchanged | `/v2/*` route added; build passed | Partial | Authenticated V2 page walkthrough blocked | Added route and subpath mapping | `npm run build`, browser auth-guard smoke | Real session page-by-page test pending |
| Security/secret scan | No secrets exposed in frontend/V2 | No backend secrets in V2 or frontend env | No V2 secrets added; public Supabase publishable key remains browser config | Partial | Prior validation notes still require credential rotation | No secrets printed/added | Code review and docs | Run formal secret scan before submission |
| Error recovery | Failed item retry and no duplicate completed files | Recovery resumes safely without duplicate completed files | Enterprise queue/retry tests pass | Partial | Controlled live interruption not run | None | `EnterpriseQueueTests`, `MigrationExecutionTests` | Small live interruption test pending |

## Current Decision

The project is ready for a controlled company demo of verified file migration evidence and the UI V2 preview. It is not ready for a production-readiness claim.

## Next Required Work

1. Run authenticated `/v2` browser smoke test with a real Supabase session.
2. Download/open report exports from an authenticated run.
3. Run controlled recovery test.
4. Run Stage 2 1,000-file migration into a fresh SharePoint target.
5. Keep empty-folder preservation listed as a known gap until implemented and verified.
