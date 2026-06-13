# ZMS Report Export Verification

Status date: 2026-06-09

## Current Result

Report/export implementation is present and compiles, but live download/open verification was not completed in this pass because the backend API is offline in the current shell.

## Export Surface Verified By Build/Code Review

| Export | Status |
| --- | --- |
| Discovery inventory CSV | Implemented |
| Permission risk CSV | Implemented |
| Metadata CSV | Implemented |
| Migration risk CSV | Implemented |
| Readiness JSON/CSV/Markdown | Implemented |
| Migration plan JSON/CSV/Markdown | Implemented |
| Migration runbook Markdown | Implemented |
| Pre-migration validation JSON/CSV/Markdown | Implemented |
| Execution simulation JSON/Markdown | Implemented |
| Migration execution job JSON/CSV/Markdown | Implemented |
| SharePoint transfer preview JSON/CSV | Implemented |
| Live pilot report JSON/CSV/Markdown | Implemented foundation |
| Workflow validation JSON/Markdown | Implemented |
| Migration job summary/items/logs CSV | Implemented |

## Live Verification Still Required

For the next 100-file run, download and open:

- Discovery Inventory CSV.
- Permission Risk CSV.
- Migration Risk CSV.
- Readiness Report.
- Migration Plan CSV.
- Migration Runbook Markdown.
- Migration Job Summary CSV.
- Migration Job Items CSV.
- Migration Job Logs CSV.
- Validation Report.

## Pass Criteria

- Files open in Excel, text editor, or markdown preview.
- UTF-8 characters render correctly.
- No secrets, tokens, or passwords are present.
- Counts match the migration job record.
- Failed/retry items are visible when present.
