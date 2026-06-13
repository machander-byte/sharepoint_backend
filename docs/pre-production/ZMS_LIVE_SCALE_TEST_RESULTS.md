# ZMS Live Scale Test Results

Status date: 2026-06-09

## Summary

| Stage | Scope | Result | Notes |
| --- | --- | --- | --- |
| Stage 0 | 22-file Google Drive -> SharePoint live migration | Passed | Completed 2026-06-08 with 0 failures and matching bytes |
| Stage 1 | 231-file Google Drive -> SharePoint live migration | Passed | Completed 2026-06-09 with 0 failures, 0 retries, and matching Graph bytes |
| Stage 2 | 1,000-file live migration | Not run | Do not run before Stage 1 passes |
| Stage 3 | 10,000-file live migration | Not run | Do not run before Stage 2 passes |

## Stage 0 Result

| Metric | Value |
| --- | ---: |
| Files migrated | 22 |
| Failed files | 0 |
| Retries | 0 |
| Source bytes | 13,807,322 |
| Target bytes verified by Microsoft Graph | 13,807,322 |
| Result | Passed |

## Stage 1 Result

| Metric | Value |
| --- | --- |
| Selected source | Google Drive Folder B |
| Folder ID | `13Y1aDYm-9h8vYsPUpWy-jDVrl8WKLVf3` |
| Migration job ID | `56746a75-96eb-4ab1-ad67-5a90f9bf04cb` |
| Files migrated | 231 |
| Failed files | 0 |
| Retries | 0 |
| Source bytes | 2,589,962 |
| Target bytes verified by Microsoft Graph | 2,589,962 |
| Google-native files observed | 0 |
| Target folder | `zms-validation/drive-stage1-231files-20260609-2100` |
| ZMS validation run | `f23f19c9-ddc7-44cd-bf74-3df5162472d0`, PASSED |

## Stage 1 Status

Stage 1 passed for file migration validation.

Independent Microsoft Graph verification returned 231 files, 61 folders, and 2,589,962 bytes in the target folder.

Known limitation: the source public inventory had 568 folders, while the target has 61 folders. Current migration preserves folder paths required by files, but does not migrate empty source folders as separate migration objects.

## Pass Criteria For Next Run

- Backend health/status/version pass.
- Supabase Postgres connected.
- Google Drive source connection passes through backend connector.
- SharePoint target connection passes through backend connector.
- Selected file count is explicitly recorded.
- Failed files = 0.
- Retries = 0 or fully explained.
- Source count and target count match.
- Source bytes and Microsoft Graph target bytes match.
- Target folder is unique and clean.
