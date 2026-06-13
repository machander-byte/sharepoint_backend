# ZMS Error Recovery Test Report

Status date: 2026-06-09

## Current Result

Controlled live interruption testing was not run in this pass.

## Evidence Available

- The 22-file live migration completed successfully after a brief manual pause/resume during automation.
- Backend tests cover queue/state-machine behavior.
- The migration worker includes retry and recovery foundations.
- Backend tests passed 46/46 after this pass.

## Not Yet Proven

- API process restart during active live upload.
- Network disconnect/reconnect during active live upload.
- Duplicate prevention after interruption.
- Resume behavior at 100/1,000/10,000 files.

## Why It Was Not Run

- The backend is currently offline and the current shell does not contain backend-only secrets.
- The available live Drive source has only 22 files, making it too small for a meaningful interruption test.
- Re-running destructive or duplicate live target operations without a fresh target folder would create unclear evidence.

## Required Controlled Test

1. Prepare a 100-file non-production Drive source folder.
2. Start a live migration to a new SharePoint target folder.
3. Stop the API after several files are in progress.
4. Restart the API with the same Supabase/Data Protection configuration.
5. Confirm queued/running items recover.
6. Resume or let the queue process.
7. Verify no duplicate completed files and no byte mismatch.

## Expected Evidence

- Before/after screenshots.
- Job timeline CSV.
- Item CSV showing completed/retried status.
- API logs with secrets redacted.
- Final source/target byte verification.
