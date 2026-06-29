# ZMS Test Data Generator

Enterprise-scale file-share dataset generator for ZMS discovery, readiness, planning, validation, and reporting benchmarks.

## Safe Start

Do not start with 100,000 files and large file sizes. Run staged tests and check disk space first.

```powershell
dotnet run -c Release --project .\source\ZMS.TestDataGenerator\ZMS.TestDataGenerator.csproj -- `
  --files 1000 `
  --depth 10 `
  --max-size 50 `
  --output .\TestTenant
```

## Stages

```powershell
# Stage 1
--files 1000 --depth 10 --max-size 50 --output .\TestTenant

# Stage 2
--files 10000 --depth 15 --max-size 100 --output .\TestTenant-10k

# Stage 3
--files 50000 --depth 20 --max-size 250 --output .\TestTenant-50k

# Stage 4, only after checking disk/RAM/CPU/Supabase/Render limits
--files 100000 --depth 20 --max-size 500 --huge-folder-files 10000 --output .\EnterpriseBenchmark
```

## Edge Cases

Edge cases are enabled by default:

```text
--edge-cases true
--long-path-files 10
--long-path-chars 320
--duplicate-name-sets 3
--corrupt-files 6
--special-char-files 10
--huge-folder-files 100
--permission-edge-files 10
```

Generated edge cases include:

- 300+ character relative paths.
- Case-collision duplicate names: `Report.docx`, `report.docx`, `REPORT.docx`.
- Broken ZIP files, invalid PDFs, and empty DOCX files.
- Filenames with `₹`, `&`, `#`, `@`, parentheses, `%`, `_`, and `-`.
- Many files under `HR/HugeSingleFolder`.
- Missing users, broken groups, and orphan permissions in `_metadata/permissions-simulation.json`.

Outputs:

- `_metadata/file-manifest.jsonl`
- `_metadata/permissions-simulation.json`
- `_reports/generation-summary.json`
- `_reports/generation-summary.txt`
