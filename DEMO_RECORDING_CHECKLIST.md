# ZMS Demo Recording Checklist

Use this checklist before recording the final walkthrough.

## Pre-Recording Setup

- Start backend API on the configured `VITE_API_BASE_URL`.
- Start frontend with `npm run dev`.
- Confirm Supabase auth settings are configured.
- Sign in successfully.
- Confirm the topbar shows the signed-in user.
- Confirm sign out works, then sign back in.
- Confirm no secrets are visible in the browser or recording.
- Use demo/sample data only.
- Keep the message clear: ZMS prepares migrations safely before tenant-changing work.

## Screens To Show

1. Login and authenticated app shell.
2. Dashboard overview.
3. Environment Builder.
4. Package generation and manifest/download flow.
5. Connections page with backend-connected profile management.
6. Discovery page.
7. Readiness analysis.
8. Permissions and metadata findings.
9. Migration Planner and wave plan.
10. Pre-migration validation and Go/No-Go decision.
11. Execution simulation.
12. Jobs command center.
13. Transfer preview and locked pilot safety message.
14. Operator Control Center full workflow validation.
15. Reports page.
16. AI recommendations.
17. Final architecture/roadmap slide or markdown.

## Key Phrases

- "This started as a simple Google Drive to SharePoint migration idea."
- "It evolved into an enterprise migration readiness and orchestration platform."
- "ZMS focuses on discovery, risk analysis, planning, validation, simulation, and reporting before real tenant changes."
- "Live migration is intentionally guarded and out of scope for this submission."
- "The current implementation has authenticated workflow, backend persistence, and passing build/test verification."

## Avoid In Demo

- Do not claim production live migration is complete.
- Do not show real secrets.
- Do not run tenant-changing scripts against a real tenant.
- Do not spend time on future roadmap details before showing implemented flows.
- Do not frame mock/demo data as production tenant data.

## Final Proof To Show

- `PROJECT_IMPLEMENTATION_REPORT.md`.
- `TEST_REPORT.md`.
- Backend test result: 43/43 passing.
- Frontend build passing.
- Future roadmap section.
