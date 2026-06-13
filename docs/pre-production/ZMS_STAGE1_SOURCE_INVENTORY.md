# ZMS Stage 1 Source Inventory

Status date: 2026-06-09

## Backend Preflight

| Check | Result | Notes |
| --- | --- | --- |
| `GET /api/health` | Passed | `localhost:5206` returned Healthy |
| `GET /api/status` | Passed | Database provider: Npgsql.EntityFrameworkCore.PostgreSQL |
| `GET /api/version` | Passed | Local API available on `localhost:5206` |
| Supabase Postgres | Passed | Backend connected through Supabase Postgres connection string |
| Queue clear | Passed | Local queue configured; 0 pending before run |
| Authenticated API access | Passed | Browser Supabase session accessed protected APIs |
| Audit logging active | Not rechecked | Migration and validation records were written; audit table query was not separately captured |
| Render backend | Failed | `https://sharepoint-backend-g5vc.onrender.com/api/health` timed out after 180 seconds |
| Local backend startup | Passed | Local API started after backend-only secrets were set in user-secrets |
| Backend env check | Passed locally | Required backend-only values were present through ASP.NET user-secrets; no secret values were written to docs |

Secret values were not printed or written. Local backend-only configuration was sufficient for the 231-file run.

## Google Drive Folder A

| Field | Value |
| --- | --- |
| Folder URL | `https://drive.google.com/drive/folders/1G-2XloVvGAlqiwxdg3Tgd1Ba_XYD9p6T?usp=sharing` |
| Folder ID | `1G-2XloVvGAlqiwxdg3Tgd1Ba_XYD9p6T` |
| Inventory method | Public Drive API recursive inventory using frontend-safe API access, not backend connector |
| Accessible | Yes |
| File count | 65 |
| Folder count | 110 |
| Total bytes | 49,298,275 |
| Google-native files | 0 |
| Unsupported files | 0 observed |
| Traversal errors | 0 |
| Stage 1 suitability | Not enough files for 100-file Stage 1 |

## Google Drive Folder B

| Field | Value |
| --- | --- |
| Folder URL | `https://drive.google.com/drive/folders/13Y1aDYm-9h8vYsPUpWy-jDVrl8WKLVf3?usp=sharing` |
| Folder ID | `13Y1aDYm-9h8vYsPUpWy-jDVrl8WKLVf3` |
| Inventory method | Public Drive API recursive inventory using frontend-safe API access, not backend connector |
| Accessible | Yes |
| File count | 231 |
| Folder count | 568 |
| Total bytes | 2,589,962 |
| Google-native files | 0 |
| Unsupported files | 0 observed |
| Traversal errors | 0 |
| Stage 1 suitability | Best candidate; contains 100+ files |

## Local Project Source Folder Inventory

Only one approved local generated test-data folder was found. Application source folders were not considered migration sources.

| Folder path | File count | Total bytes | Max depth | Special cases | Safe to use |
| --- | ---: | ---: | ---: | --- | --- |
| `test-artifacts/preprod-generator-smoke-20260609` | 104 total files, including 100 generated data files | 74,398,197 total bytes, 74,340,360 generated data bytes | 8 | Huge folder, special characters, empty DOCX, duplicate case collisions, long paths, broken ZIP, invalid PDF | Yes, as local synthetic test data |

## Selected Stage 1 Source

Selected candidate: Google Drive Folder B.

Reason:

- It has 231 accessible files, exceeding the 100-file Stage 1 requirement.
- No Google-native files were observed, so byte comparison should be direct.
- No public inventory traversal errors were observed.
- It is cleaner for Stage 1 than Folder A, which has only 65 files.

## Planned Target

The actual Stage 1 run used:

```text
zms-validation/drive-stage1-231files-20260609-2100
```

## Current Decision

Stage 1 has now run successfully against the full Folder B inventory.

Result:

- 231/231 files migrated.
- 0 failed files.
- 0 retries.
- 2,589,962 source bytes matched 2,589,962 target bytes verified by Microsoft Graph.
- ZMS validation run `f23f19c9-ddc7-44cd-bf74-3df5162472d0` passed with 231/231 items.

Known limitation: empty Google Drive folders are not migrated as first-class items. Microsoft Graph found 61 target folders because only folder paths required for migrated files were created.
