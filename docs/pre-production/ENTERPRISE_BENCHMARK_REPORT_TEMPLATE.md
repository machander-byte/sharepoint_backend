# Enterprise Benchmark Report Template

Use this file as the basis for `ZMS Enterprise Scale Validation Report v1.0`.

## Environment

| Field | Value |
| --- | --- |
| Date | |
| ZMS commit | |
| Frontend URL | |
| API URL | |
| Supabase project ref | |
| Render service plan | |
| API CPU/RAM | |
| Test data disk | |
| Google/Microsoft test tenant | |

## Stage Commands

Generate source test data from `source/ZMS.TestDataGenerator`:

```powershell
dotnet run -c Release --project .\source\ZMS.TestDataGenerator\ZMS.TestDataGenerator.csproj -- --files 1000 --depth 10 --max-size 50 --output .\TestTenant-Stage1
dotnet run -c Release --project .\source\ZMS.TestDataGenerator\ZMS.TestDataGenerator.csproj -- --files 10000 --depth 15 --max-size 100 --output .\TestTenant-Stage2
dotnet run -c Release --project .\source\ZMS.TestDataGenerator\ZMS.TestDataGenerator.csproj -- --files 50000 --depth 20 --max-size 250 --output .\TestTenant-Stage3
dotnet run -c Release --project .\source\ZMS.TestDataGenerator\ZMS.TestDataGenerator.csproj -- --files 100000 --depth 20 --max-size 500 --output .\TestTenant-Stage4
```

Do not run Stage 4 until disk space, Render limits, Supabase limits, and tenant throttling limits are confirmed.

## Benchmark Results

| Stage | Files | Depth | Max Size MB | Dataset Size GB | Discovery Time | Planning Time | Validation Time | Migration Time | Peak CPU | Peak RAM | Supabase Errors | Result |
| --- | ---: | ---: | ---: | ---: | --- | --- | --- | --- | ---: | ---: | ---: | --- |
| Stage 1 | 1,000 | 10 | 50 | | | | | | | | | |
| Stage 2 | 10,000 | 15 | 100 | | | | | | | | | |
| Stage 3 | 50,000 | 20 | 250 | | | | | | | | | |
| Stage 4 | 100,000 | 20 | 500 | | | | | | | | | |

## Validation Summary

| Check | Stage 1 | Stage 2 | Stage 3 | Stage 4 |
| --- | --- | --- | --- | --- |
| Source files discovered | | | | |
| Target files copied | | | | |
| Zero-byte mismatch | | | | |
| Metadata preserved | | | | |
| Folder paths preserved | | | | |
| Permission risks reported | | | | |
| Corrupted files detected | | | | |
| Long paths detected | | | | |
| Duplicate names detected | | | | |
| Huge single folder handled | | | | |

## Evidence Links

Add links or file paths to:

- Generation summary JSON.
- Discovery inventory CSV.
- Permission risk CSV.
- Migration risk CSV.
- Readiness report.
- Migration plan CSV.
- Migration runbook markdown.
- Job report.
- Validation report.
- Sentry events.
- Supabase audit query output.

## Executive Result

```text
ZMS Enterprise Scale Validation Report v1.0

Files tested:
Data size:
Hierarchy depth:
Migration path:
Source:
Target:
Success rate:
Discovery failures:
Planning failures:
Migration failures:
Validation failures:
Data loss:
Final decision:
```

