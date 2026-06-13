# ZMS Large-Scale Validation Plan

Status date: 2026-06-09

## Current Evidence

| Scope | Status | Evidence |
| --- | --- | --- |
| Live Google Drive -> SharePoint baseline | Passed | 22/22 files, 0 failures, 0 retries, byte totals match |
| Synthetic generator smoke | Passed | 100 local files, 70.9 MB generated under `test-artifacts/preprod-generator-smoke-20260609` |
| File-share long-path discovery | Fixed and tested | Backend test suite now includes long-path enumeration regression |
| Stage 1 live Drive migration | Passed | Google Drive Folder B migrated 231/231 files, 0 failures, 0 retries, Graph bytes matched |
| Live 1,000/10,000-file migration | Not run | Stage 1 has passed; prepare next approved source |

## Required Live Stages

| Stage | Source | Target | File count | Max size | Approval required | Exit criteria |
| --- | --- | --- | ---: | ---: | --- | --- |
| Stage 0 | Google Drive certification folder | SharePoint Documents | 22 | Existing | Complete | 22/22, 0 failed, source and target byte totals match |
| Stage 1 | Google Drive Folder B | SharePoint test folder | 231 | Existing | Complete | 231/231, 0 failed, source and target byte totals match |
| Stage 2 | Prepared non-production Drive folder | New SharePoint test folder | 1,000 | <= 100 MB | Yes | 1,000/1,000, 0 failed, memory/CPU captured |
| Stage 3 | Prepared non-production Drive folder | New SharePoint test folder | 10,000 | <= 100 MB | Yes | 10,000/10,000, validation and exports complete |
| Stage 4 | Edge-case dataset | SharePoint test folder | 1,000+ | staged | Yes | Long paths, duplicates, corrupt files, special characters, huge folder, and permission anomalies handled |
| Enterprise | Approved benchmark dataset | Dedicated test tenant | 100,000 | staged | Explicit approval | No data loss, recovery proof, benchmark report signed off |

## Metrics To Capture

- Discovery duration.
- Planning duration.
- Validation duration.
- Migration duration.
- Files discovered, queued, migrated, failed, retried.
- Source bytes and target bytes.
- API process memory and CPU.
- Render memory/timeout behavior if hosted.
- Supabase table growth and query latency.
- Microsoft Graph throttling and retry counts.
- Export generation time.

## Resource Checks Before Stage 2+

```text
Free disk space
Supabase pooler limits
Supabase database size
Render memory and timeout limits
Microsoft Graph throttling
Google Drive API quota
SharePoint tenant storage quota
Network stability
```

## Synthetic Dataset Command Set

Run these only on non-production data and only after checking disk space:

```powershell
dotnet run -c Release --project .\source\ZMS.TestDataGenerator\ZMS.TestDataGenerator.csproj -- `
  --files 100 `
  --depth 10 `
  --max-size 1 `
  --output .\test-artifacts\preprod-generator-smoke-20260609
```

Next safe generator stage:

```powershell
dotnet run -c Release --project .\source\ZMS.TestDataGenerator\ZMS.TestDataGenerator.csproj -- `
  --files 1000 `
  --depth 10 `
  --max-size 10 `
  --output .\test-artifacts\preprod-generator-1k
```

Do not run 50k, 100k, or multi-hundred-GB stages without explicit approval.

## Current Blockers

- The configured Render backend still needs a fresh hosted `/api/health` verification before submission.
- Empty source folders are not migrated as first-class migration objects. Stage 1 preserved file-bearing folder paths only.
- The Google OAuth scope available to ZMS is read-only, so Codex cannot seed additional files back into Drive through the connector.
- OneDrive is not validated as a first-class connector.

## Next Action

Prepare and approve a 1,000-file non-production Drive source, run it into a fresh SharePoint target folder, and capture API process memory/CPU plus Microsoft Graph verification.
