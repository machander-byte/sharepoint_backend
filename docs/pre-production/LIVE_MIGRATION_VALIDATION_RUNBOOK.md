# Live Migration Validation Runbook

Run this only against approved test tenants and test folders.

## Objective

Prove that ZMS can perform real file movement with no data loss across implemented connector paths:

- Google Drive -> SharePoint Online.
- SharePoint Online -> SharePoint Online.

`Google Drive -> OneDrive` is not part of the current implementation because OneDrive is not a registered connection type.

## Preflight

Confirm these before a live run:

- Backend API is deployed and healthy: `GET /api/health`.
- Frontend origin is allowed in backend CORS.
- Supabase JWT auth is working from the frontend session.
- `ConnectionStrings__ZmsDatabase` points to Supabase Postgres.
- `DataProtection__KeyStorage=Database` is configured.
- Google Drive backend credentials are configured on the backend host if using Google Drive.
- SharePoint Online connection has tenant ID, app client ID, client secret, and document library name.
- Microsoft Graph admin consent is granted.
- Source folders contain only approved test data.
- Target library is empty or clearly named for the test run.

## Test Matrix

| Test | Source | Target | File Count | Required Result |
| --- | --- | --- | ---: | --- |
| GDSPO-100 | Google Drive folder | SharePoint Online library | 100 | 100 copied, 0 corrupt |
| GDSPO-1000 | Google Drive folder | SharePoint Online library | 1,000 | 1,000 copied, 0 corrupt |
| GDSPO-10000 | Google Drive folder | SharePoint Online library | 10,000 | 10,000 copied, 0 corrupt |
| SPOSPO-100 | SharePoint Online library | SharePoint Online library | 100 | 100 copied, 0 corrupt |
| SPOSPO-1000 | SharePoint Online library | SharePoint Online library | 1,000 | 1,000 copied, 0 corrupt |
| SPOSPO-10000 | SharePoint Online library | SharePoint Online library | 10,000 | 10,000 copied, 0 corrupt |

## Execution Steps

1. Create or confirm the source connection.
2. Click `Test` on the source connection and save the message.
3. Create or confirm the SharePoint Online target connection.
4. Click `Test` on the target connection and save the message.
5. Create a migration job from the Migrations page.
6. Start the job.
7. Monitor job status, completed items, failed items, retry count, and timeline.
8. Run validation for the completed job.
9. Export job report, validation report, and item CSV.
10. Manually spot-check at least 20 files in the target location for each run.

## Required Evidence

For every run, record:

- Run ID.
- Source connection ID and type.
- Target connection ID and target library.
- File count requested.
- File count discovered.
- File count copied.
- Failed item count.
- Retry count.
- Total bytes copied.
- Start time UTC.
- Finish time UTC.
- Duration.
- Peak API memory.
- Peak API CPU.
- Supabase connection errors, if any.
- Sentry event ID for any controlled or unexpected error.

## Pass Criteria

A run passes only when:

- Source and target connection tests pass before the run.
- Job reaches `Completed`.
- Failed items equals `0`.
- Validation failed count equals `0`.
- Source file count equals target file count.
- Source total size equals target total size, except for Google native files exported to Office/PDF formats.
- Folder structure matches expected relative paths.
- Metadata fields that ZMS claims to preserve are present in the target evidence.
- Permission handling matches the current product claim: discovered and reported, not silently claimed as applied.

## Failure Handling

If the internet or API process is interrupted:

- Confirm the job returns to `Queued` or `RetryQueued` on API restart.
- Confirm in-progress items are retried, not duplicated.
- Confirm completed items are not copied again unless overwrite is explicitly enabled.
- Export the timeline and logs before retrying a failed run.

## Known Product Gap

OneDrive validation requires one of these changes:

- Add `OneDrive` as a first-class `ConnectionType` and implement source/target connectors.
- Extend the SharePoint Graph connector to support `/users/{id}/drive` or `/me/drive` in a clearly named OneDrive mode.

Until then, do not include OneDrive in the completion percentage for real migration validation.

