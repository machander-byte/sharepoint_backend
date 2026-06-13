# ZMS UI V2 Design Plan

Status date: 2026-06-09

## Goal

Make the first-screen product feel like an operations console for real migration validation, not a demo dashboard.

## V2 Navigation

| Section | Purpose |
| --- | --- |
| Command Center | Current system health, running jobs, blockers, and next safe action |
| Connections | Google, SharePoint, file share, and future OneDrive setup/testing |
| Discovery | Source inventory, risks, and export controls |
| Plan | Waves, target mappings, readiness blockers, runbook |
| Migrate | Live migration wizard, dry run, start/pause/resume/retry |
| Validate | Source/target verification, byte totals, item diffs |
| Evidence | Reports, screenshots, benchmark records, company submission package |
| Security | OAuth status, secret rotation checklist, audit logs, RBAC |

## Live Migration Wizard

1. Select source connection.
2. Select one or more source folders/files where the connector supports it.
3. Select SharePoint target site/library/folder.
4. Run discovery preview.
5. Review risks and estimated size.
6. Require confirmation for live upload.
7. Show progress, retries, pause/resume, and verification.
8. Export evidence package.

## Evidence Center

Each completed run should show:

- Files migrated.
- Failed/retried files.
- Source and target bytes.
- Validation status.
- Graph verification status.
- Export links.
- Screenshots or generated proof cards.
- Company-ready summary text.

## Prototype Status

No `/v2` prototype was added in this pass. The V2 plan is documented so implementation can happen after the current validation docs and safety fixes are complete.
