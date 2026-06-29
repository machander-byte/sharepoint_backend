# ZMS Demo Video Script

Target length: 6 to 8 minutes.

## 1. Opening

"This project started as a simple Google Drive to SharePoint migration idea. After studying enterprise migration risks, it evolved into ZMS, a SharePoint migration readiness, planning, validation, simulation, and control-plane platform."

"The core idea is simple: ZMS helps teams understand what can go wrong before they migrate content."

## 2. Problem

"Enterprise SharePoint migrations fail for reasons that are not obvious from file counts alone: broken permissions, missing metadata, long paths, large files, duplicate content, restricted folders, archived content, unclear ownership, and no Go/No-Go validation."

"A basic file mover does not solve those problems. ZMS focuses on readiness and orchestration before tenant-changing work."

## 3. Architecture

"The system has a React/Vite frontend and a .NET 8 backend. The backend owns APIs, authentication validation, persistence, Data Protection secret handling, discovery, readiness analysis, planning, validation, simulation, reports, and connector foundations."

"The frontend is now protected with Supabase authentication, and the active app has a single route/layout structure."

## 4. Authenticated Workflow

Show login and app shell.

"The active application is now authenticated. Routes are protected, the topbar shows the signed-in user, and sign out is available from the main shell."

## 5. Environment Builder

Show Environment Builder.

"ZMS can generate a realistic SharePoint test environment model with HR, Finance, IT, PMO, and Operations content. It includes site collections, subsites, libraries, lists, metadata, permission groups, folders, sample content, and migration edge cases."

"Instead of directly changing a tenant from the browser, ZMS generates a safe admin-run package."

## 6. Connections

Show Connections page.

"Connections are now backend-connected. The UI loads persisted connection profiles, creates new profiles, updates existing profiles, and tests them through the backend API."

"Secrets are handled by the backend, not by browser-only state."

## 7. Discovery And Readiness

Show Discovery, then readiness/dashboard.

"Discovery collects the content and risk picture: sites, libraries, files, folders, metadata, permissions, long path risks, large file risks, and restricted content."

"Readiness analysis turns discovery into scores, blockers, risk findings, remediation actions, and migration wave suggestions."

## 8. Planning And Validation

Show Planner.

"ZMS converts readiness into a migration plan with waves, included and excluded items, checklist items, approvals, risks, remediation prerequisites, and a generated runbook."

"Before execution, the pre-migration validation produces a Go, Conditional Go, or No-Go decision."

## 9. Simulation And Jobs

Show Jobs page.

"The current submission focuses on safe simulation. ZMS can estimate duration, warnings, failures, and wave-by-wave execution without changing the tenant."

"The Jobs command center tracks simulated lifecycle actions such as start, pause, resume, cancel, and retry."

## 10. Transfer Preview And Safety

Show transfer preview/pilot area.

"ZMS includes a SharePoint migration adapter foundation. It can produce a transfer preview with eligible items, blocked items, target paths, metadata mapping preview, and permission mapping preview."

"Live pilot migration is intentionally locked behind safety gates and is not claimed as production-ready in this submission."

## 11. Operator And Reports

Show Operator Control Center and Reports.

"The Operator Control Center validates the full workflow chain: discovery, readiness, planning, validation, simulation, execution job, transfer preview, and reports."

"Reports are generated across inventory, permissions, metadata, readiness, remediation, migration plans, runbooks, validation, execution simulation, transfer preview, and workflow validation."

## 12. Closing

"The strongest point of ZMS is that it does not blindly migrate content. It prepares enterprises for safe migration by discovering risks, planning waves, validating readiness, simulating execution, and generating reports before real SharePoint tenant changes."

"For future work, the major roadmap items are real locked pilot copy, metadata writeback, permission preservation, and additional connectors. Those are intentionally kept out of this submission to keep the demo safe and reliable."
