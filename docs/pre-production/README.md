# ZMS Pre-Production Validation Package

This folder is the company submission evidence package for ZMS pre-production validation.

Use it to prove the product with real connectors, real cloud credentials, Supabase-backed persistence, exportable reports, recovery behavior, and monitoring.

## Current implementation boundary

Implemented connector paths in the current codebase:

- Google Drive source to SharePoint Online target.
- SharePoint Online source to SharePoint Online target.
- File share source to SharePoint Online target.
- SharePoint On-Prem source abstraction.

Not implemented as a first-class connector yet:

- OneDrive source or OneDrive target.

Do not mark `Google Drive -> OneDrive` as validated until a OneDrive connector is added or the existing Graph target connector is explicitly extended for user drives.

Current permission status:

- Permission discovery, risk reporting, and preview mapping are supported.
- Permission writeback is intentionally disabled or preview-only in the adapter flow unless a dedicated implementation is added and tested.

## Required validation artifacts

Before company review, collect these files and screenshots:

- Live connection test screenshots for Google Drive and SharePoint Online.
- Migration job logs for 100, 1,000, and 10,000 file runs.
- Validation exports proving file count, size, paths, and metadata checks.
- Discovery inventory CSV.
- Permission risk CSV.
- Migration risk CSV.
- Readiness report.
- Migration plan CSV.
- Migration runbook markdown.
- Sentry event screenshots for controlled failures.
- Supabase table screenshots or SQL query output showing job, item, log, and audit records.

## Document map

- `LIVE_MIGRATION_VALIDATION_RUNBOOK.md`: how to validate real connector migration paths.
- `ZMS_LIVE_MIGRATION_VALIDATION_2026-06-08.md`: successful 22-file live Google Drive -> SharePoint validation evidence.
- `ZMS_LIVE_MIGRATION_STAGE1_231FILES_20260609.md`: successful 231-file Google Drive Folder B -> SharePoint validation evidence.
- `ZMS_LARGE_SCALE_VALIDATION_PLAN.md`: staged 100/1k/10k/enterprise validation plan and current blockers.
- `ZMS_STAGE1_SOURCE_INVENTORY.md`: Drive Folder A/B and local generated dataset inventory for the next Stage 1 run.
- `ZMS_LIVE_SCALE_TEST_RESULTS.md`: live scale stage status and current Stage 1 result.
- `OAUTH_AND_ENV_READINESS.md`: required Render, Vercel, Supabase, Google, and Microsoft settings.
- `ENTERPRISE_BENCHMARK_REPORT_TEMPLATE.md`: report template for staged 1k, 10k, 50k, and 100k file benchmarks.
- `SECURITY_AND_SUBMISSION_CHECKLIST.md`: secrets, RBAC, audit, report, recovery, and monitoring checks.
- `ZMS_AI_FEATURE_INVENTORY.md`: implemented AI/ETA/remediation features and safety boundaries.
- `ZMS_AI_FEATURE_TEST_REPORT.md`: AI test status and blockers.
- `ZMS_SHAREGATE_FEATURE_GAP_MATRIX.md`: internal gap matrix against ShareGate-like expectations.
- `ZMS_REPORT_EXPORT_VERIFICATION.md`: export surface and pending live verification.
- `ZMS_ERROR_RECOVERY_TEST_REPORT.md`: recovery evidence and pending controlled interruption test.
- `ZMS_MONITORING_VALIDATION_REPORT.md`: health, Sentry, and audit validation status.
- `CODEX_VALIDATION_CHECKPOINT.md`: 2026-06-09 pass summary and safe resume point.

## Current Status - 2026-06-09

- 22-file live Google Drive -> SharePoint migration: passed.
- 231-file live Google Drive -> SharePoint migration: passed, 231/231 files, 0 failures, 0 retries, Microsoft Graph bytes matched.
- Backend build/tests: passed, 46/46.
- Frontend build: passed with known chunk-size warning.
- Synthetic generator smoke: passed, 100 files and 70.9 MB.
- Stage 1 source candidate: Google Drive Folder B, completed as full 231-file run.
- Live 1,000/10,000-file certification: pending.
- Current backend API: local backend passed health/status/version during Stage 1; Render backend health still needs recheck before hosted submission.
- Known migration gap: empty source folders are not preserved as first-class objects; only file-bearing folder paths were created in SharePoint.
