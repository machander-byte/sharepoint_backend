# ZMS Live Migration Stage 1 - 231 Files - 2026-06-09

## Summary

Status: Passed for file migration and byte-level target verification.

At the user's request, Stage 1 was run against the full Google Drive Folder B inventory instead of limiting the run to 100 files.

```text
Google Drive -> SharePoint Online

Files migrated: 231/231
Failed files: 0
Retries: 0

Source bytes: 2,589,962
Target bytes verified by Microsoft Graph: 2,589,962

ZMS validation: PASSED
```

## Run Details

| Field | Value |
| --- | --- |
| Migration job ID | `56746a75-96eb-4ab1-ad67-5a90f9bf04cb` |
| Job name | `Drive Folder B Stage 1 - 231 files - 2026-06-09 2100 IST` |
| Source | Google Drive Folder B |
| Source folder ID | `13Y1aDYm-9h8vYsPUpWy-jDVrl8WKLVf3` |
| Target site | `https://zettalogix.sharepoint.com/sites/ZMSTeam` |
| Target library | `Documents` |
| Target folder | `zms-validation/drive-stage1-231files-20260609-2100` |
| Backend | Local `ZMS.API` on `http://localhost:5206` |
| Database | Supabase Postgres via backend connection string |
| Queue | Local in-memory queue |

## Preflight

| Check | Result |
| --- | --- |
| `GET /api/health` | Healthy |
| `GET /api/status` | Healthy |
| Supabase Postgres | Connected |
| Queue | Local, configured, 0 pending before run |
| Authenticated API access | Passed through browser Supabase session |
| Google Drive connection test | Passed |
| SharePoint connection test | Passed |

Connection test timestamps:

- Google Drive: `2026-06-09T15:29:56.9833268Z`
- SharePoint Online: `2026-06-09T15:30:00.0955759Z`

## Timing

| Event | UTC time | IST time |
| --- | --- | --- |
| Job created | `2026-06-09T15:30:28.250813Z` | `2026-06-09 21:00:28 IST` |
| Job queued after discovery | `2026-06-09T15:35:01.474314Z` | `2026-06-09 21:05:01 IST` |
| Worker started migration | `2026-06-09T15:35:05.927388Z` | `2026-06-09 21:05:05 IST` |
| Job finished | `2026-06-09T15:50:41.989403Z` | `2026-06-09 21:20:41 IST` |
| Microsoft Graph verification | `2026-06-09T15:53:04.6071773Z` | `2026-06-09 21:23:04 IST` |
| ZMS validation run | `2026-06-09T15:53:33.7534815Z` | `2026-06-09 21:23:33 IST` |

Observed timing:

- Discovery and queue preparation: about 4 minutes 33 seconds.
- Transfer processing: about 15 minutes 36 seconds.
- End-to-end from job creation to completion: about 20 minutes 13 seconds.

## Migration Result

| Metric | Value |
| --- | ---: |
| Items discovered by backend | 231 |
| Items completed | 231 |
| Items failed | 0 |
| Retries | 0 |
| Source bytes recorded by ZMS | 2,589,962 |
| Target bytes verified by Microsoft Graph | 2,589,962 |
| Job status | Completed |
| Enterprise state | COMPLETED |

## Independent Microsoft Graph Verification

Microsoft Graph was used after completion to recursively inspect:

```text
Site: https://zettalogix.sharepoint.com/sites/ZMSTeam
Library: Documents
Folder: zms-validation/drive-stage1-231files-20260609-2100
```

Graph result:

| Metric | Value |
| --- | ---: |
| Target files | 231 |
| Target folders | 61 |
| Target bytes | 2,589,962 |

Sample verified target paths:

- `.dart_tool/dartpad/web_plugin_registrant.dart`
- `.dart_tool/extension_discovery/vs_code.json`
- `.dart_tool/package_config.json`
- `.idea/.idea/.gitignore`
- `.idea/.idea/modules.xml`

## ZMS Validation Result

| Field | Value |
| --- | --- |
| Validation run ID | `f23f19c9-ddc7-44cd-bf74-3df5162472d0` |
| Status | PASSED |
| Source item count | 231 |
| Target item count | 231 |
| Passed items | 231 |
| Warnings | 0 |
| Failures | 0 |
| Findings | 0 |

Validation summary:

```text
Validated 231 migrated item records using path, status, size, metadata, and permission availability checks.
```

## Important Limitation Found

The public source inventory counted 568 Google Drive folders, while Microsoft Graph found 61 folders in the target.

This is not data loss for files: all 231 files and all file bytes were migrated and verified. It does mean the current migration engine preserves folder paths required by files, but does not migrate empty source folders as first-class migration objects.

Before claiming full folder-structure preservation for enterprise customers, add and test explicit empty-folder migration support.

## Decision

Stage 1 is complete for file migration validation.

Next validation stages:

1. Run a 1,000-file live migration source.
2. Run a 10,000-file live migration source.
3. Add empty-folder preservation support or clearly document that empty folders are excluded.
4. Run controlled interruption/resume testing.
