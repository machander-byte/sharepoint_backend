# ZMS ShareGate-Like Roadmap

Status date: 2026-06-09

## Phase 1: Internal Company Review

- Keep Supabase as the only production database.
- Preserve the 22-file live migration evidence.
- Run 100-file Google Drive -> SharePoint certification.
- Export inventory, job, item, validation, and runbook reports.
- Rotate all secrets pasted during validation.
- Capture Sentry and audit-log evidence.

## Phase 2: Scale Certification

- Run 1,000-file and 10,000-file live migrations.
- Run synthetic edge-case discovery using the test data generator.
- Validate resume after API restart.
- Validate retries after transient Graph/Drive errors.
- Record CPU, memory, API duration, and DB growth.

## Phase 3: Connector Completion

- Certify SharePoint -> SharePoint.
- Decide whether OneDrive is in scope for company submission.
- Add or explicitly defer Google Drive target/writeback.
- Harden file-share long-path and invalid-character handling through more tests.

## Phase 4: Enterprise Evidence Center

- Create a first-class Evidence Center in the UI.
- Bundle migration screenshots, CSVs, markdown runbooks, validation summaries, and Graph verification into one submission package.
- Add benchmark comparison views by run.

## Phase 5: Production Operations

- Configure Sentry, uptime checks, and structured audit exports.
- Document tenant onboarding.
- Add deployment runbooks for Vercel, Render, and Supabase.
- Add incident response and recovery procedures.

## Not In This Scope

Subscription, billing, and pricing implementation are intentionally out of scope for this validation pass.
