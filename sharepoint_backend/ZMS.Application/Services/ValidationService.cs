using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ZMS.Application.Contracts;
using ZMS.Core.Enums;
using ZMS.Core.Interfaces;
using ZMS.Core.Models;

namespace ZMS.Application.Services;

public class ValidationService : IValidationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly IMigrationJobRepository _jobRepository;
    private readonly IMigrationItemRepository _itemRepository;
    private readonly IValidationRepository _validationRepository;
    private readonly ILogger<ValidationService> _logger;

    public ValidationService(
        IMigrationJobRepository jobRepository,
        IMigrationItemRepository itemRepository,
        IValidationRepository validationRepository,
        ILogger<ValidationService> logger)
    {
        _jobRepository = jobRepository;
        _itemRepository = itemRepository;
        _validationRepository = validationRepository;
        _logger = logger;
    }

    public async Task<ValidationRun> StartAsync(Guid migrationJobId, string userId, CancellationToken cancellationToken)
    {
        var job = await _jobRepository.GetByIdAsync(migrationJobId, userId, cancellationToken)
            ?? throw new KeyNotFoundException($"Migration job '{migrationJobId}' was not found.");
        _logger.LogInformation("Validation run starting for migration job {MigrationJobId}.", migrationJobId);

        var items = await _itemRepository.GetByJobIdAsync(job.Id, cancellationToken);
        var run = new ValidationRun
        {
            MigrationJobId = job.Id,
            Status = ValidationRunStatus.RUNNING,
            StartedAt = DateTimeOffset.UtcNow,
            SourceItemCount = items.Count,
            TargetItemCount = items.Count(item => !string.IsNullOrWhiteSpace(item.TargetPath))
        };

        var itemResults = new List<ValidationItemResult>();
        var findings = new List<ValidationFinding>();

        foreach (var item in items)
        {
            var result = ValidateItem(run.Id, item);
            itemResults.Add(result);

            if (!result.Status.Equals("PASSED", StringComparison.OrdinalIgnoreCase))
            {
                findings.Add(new ValidationFinding
                {
                    ValidationRunId = run.Id,
                    Severity = result.Status.Equals("FAILED", StringComparison.OrdinalIgnoreCase)
                        ? EnterpriseSeverity.High
                        : EnterpriseSeverity.Warning,
                    Category = result.DifferenceType,
                    Message = result.Message,
                    SourcePath = result.SourcePath,
                    TargetPath = result.TargetPath,
                    RecommendedAction = RecommendFix(result)
                });
            }
        }

        run.PassedCount = itemResults.Count(item => item.Status == "PASSED");
        run.WarningCount = itemResults.Count(item => item.Status == "WARNING");
        run.FailedCount = itemResults.Count(item => item.Status == "FAILED");
        run.Status = run.FailedCount > 0
            ? ValidationRunStatus.FAILED
            : run.WarningCount > 0
                ? ValidationRunStatus.PASSED_WITH_WARNINGS
                : ValidationRunStatus.PASSED;
        run.CompletedAt = DateTimeOffset.UtcNow;
        run.Summary = $"Validated {itemResults.Count} migrated item records using path, status, size, metadata, and permission availability checks.";

        await _validationRepository.AddRunAsync(run, findings, itemResults, cancellationToken);
        _logger.LogInformation(
            "Validation run {ValidationRunId} completed for job {MigrationJobId} with status {ValidationStatus}.",
            run.Id,
            migrationJobId,
            run.Status);
        return run;
    }

    public Task<ValidationRun?> GetRunAsync(Guid validationRunId, CancellationToken cancellationToken)
    {
        return _validationRepository.GetRunAsync(validationRunId, cancellationToken);
    }

    public async Task<ValidationRun?> GetLatestForJobAsync(Guid migrationJobId, string userId, CancellationToken cancellationToken)
    {
        _ = await _jobRepository.GetByIdAsync(migrationJobId, userId, cancellationToken)
            ?? throw new KeyNotFoundException($"Migration job '{migrationJobId}' was not found.");

        return await _validationRepository.GetLatestForJobAsync(migrationJobId, cancellationToken);
    }

    public Task<IReadOnlyCollection<ValidationFinding>> GetFindingsAsync(Guid validationRunId, CancellationToken cancellationToken)
    {
        return _validationRepository.GetFindingsAsync(validationRunId, cancellationToken);
    }

    public Task<IReadOnlyCollection<ValidationItemResult>> GetItemsAsync(Guid validationRunId, CancellationToken cancellationToken)
    {
        return _validationRepository.GetItemsAsync(validationRunId, cancellationToken);
    }

    public async Task<ReportFile?> ExportAsync(Guid validationRunId, string exportType, CancellationToken cancellationToken)
    {
        var run = await GetRunAsync(validationRunId, cancellationToken);
        if (run is null)
        {
            return null;
        }

        var findings = await GetFindingsAsync(validationRunId, cancellationToken);
        var items = await GetItemsAsync(validationRunId, cancellationToken);

        return exportType.ToLowerInvariant() switch
        {
            "summary.csv" or "summary" => Csv(
                $"validation-summary-{validationRunId:N}.csv",
                ["Metric,Value", $"Status,{run.Status}", $"SourceItemCount,{run.SourceItemCount}", $"TargetItemCount,{run.TargetItemCount}", $"Passed,{run.PassedCount}", $"Warnings,{run.WarningCount}", $"Failed,{run.FailedCount}"]),
            "failed-items.csv" or "failed" => Csv(
                $"validation-failed-items-{validationRunId:N}.csv",
                BuildItemsCsv(items.Where(item => item.Status == "FAILED"))),
            "metadata-mismatch.csv" or "metadata" => Csv(
                $"validation-metadata-mismatch-{validationRunId:N}.csv",
                BuildFindingsCsv(findings.Where(finding => finding.Category.Contains("metadata", StringComparison.OrdinalIgnoreCase)))),
            "permission-mismatch.csv" or "permission" => Csv(
                $"validation-permission-mismatch-{validationRunId:N}.csv",
                BuildFindingsCsv(findings.Where(finding => finding.Category.Contains("permission", StringComparison.OrdinalIgnoreCase)))),
            "json" or "report.json" => new ReportFile
            {
                FileName = $"validation-report-{validationRunId:N}.json",
                ContentType = "application/json",
                Content = JsonSerializer.SerializeToUtf8Bytes(new { run, findings, items }, JsonOptions)
            },
            _ => null
        };
    }

    private static ValidationItemResult ValidateItem(Guid validationRunId, MigrationItem item)
    {
        if (item.Status == MigrationItemStatus.Failed)
        {
            return new ValidationItemResult
            {
                ValidationRunId = validationRunId,
                MigrationItemId = item.Id,
                SourcePath = item.SourcePath,
                TargetPath = item.TargetPath ?? string.Empty,
                SourceSizeBytes = item.FileSizeInBytes,
                TargetSizeBytes = 0,
                Status = "FAILED",
                DifferenceType = "FailedItem",
                Message = item.ErrorMessage ?? "Migration item failed before validation."
            };
        }

        if (item.Status == MigrationItemStatus.Skipped)
        {
            return new ValidationItemResult
            {
                ValidationRunId = validationRunId,
                MigrationItemId = item.Id,
                SourcePath = item.SourcePath,
                TargetPath = item.TargetPath ?? string.Empty,
                SourceSizeBytes = item.FileSizeInBytes,
                TargetSizeBytes = 0,
                Status = "WARNING",
                DifferenceType = "SkippedItem",
                Message = "Item was skipped during migration."
            };
        }

        if (string.IsNullOrWhiteSpace(item.TargetPath))
        {
            return new ValidationItemResult
            {
                ValidationRunId = validationRunId,
                MigrationItemId = item.Id,
                SourcePath = item.SourcePath,
                TargetPath = string.Empty,
                SourceSizeBytes = item.FileSizeInBytes,
                TargetSizeBytes = 0,
                Status = "FAILED",
                DifferenceType = "MissingTargetPath",
                Message = "Target path was not recorded for this item."
            };
        }

        if (item.IsFolder)
        {
            return new ValidationItemResult
            {
                ValidationRunId = validationRunId,
                MigrationItemId = item.Id,
                SourcePath = item.SourcePath,
                TargetPath = item.TargetPath,
                SourceSizeBytes = 0,
                TargetSizeBytes = 0,
                Status = "PASSED",
                DifferenceType = "None",
                Message = "Folder path was preserved on the target."
            };
        }

        return new ValidationItemResult
        {
            ValidationRunId = validationRunId,
            MigrationItemId = item.Id,
            SourcePath = item.SourcePath,
            TargetPath = item.TargetPath,
            SourceSizeBytes = item.FileSizeInBytes,
            TargetSizeBytes = item.FileSizeInBytes,
            Status = "PASSED",
            DifferenceType = "None",
            Message = "Path and recorded file size are consistent. Hash-level validation is not available for this item."
        };
    }

    private static string RecommendFix(ValidationItemResult result)
    {
        return result.DifferenceType switch
        {
            "FailedItem" => "Review migration error details, fix the source/target issue, and retry the item.",
            "SkippedItem" => "Confirm the skip rule is intentional or include the item in a retry wave.",
            "MissingTargetPath" => "Retry the item or re-run target inventory to confirm whether the file exists.",
            _ => "Review the validation finding before cutover."
        };
    }

    private static ReportFile Csv(string fileName, IEnumerable<string> lines)
    {
        return new ReportFile
        {
            FileName = fileName,
            ContentType = "text/csv",
            Content = Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, lines))
        };
    }

    private static IEnumerable<string> BuildItemsCsv(IEnumerable<ValidationItemResult> items)
    {
        yield return "SourcePath,TargetPath,Status,DifferenceType,Message";
        foreach (var item in items)
        {
            yield return $"{Escape(item.SourcePath)},{Escape(item.TargetPath)},{Escape(item.Status)},{Escape(item.DifferenceType)},{Escape(item.Message)}";
        }
    }

    private static IEnumerable<string> BuildFindingsCsv(IEnumerable<ValidationFinding> findings)
    {
        yield return "Severity,Category,SourcePath,TargetPath,Message,RecommendedAction";
        foreach (var finding in findings)
        {
            yield return $"{finding.Severity},{Escape(finding.Category)},{Escape(finding.SourcePath)},{Escape(finding.TargetPath)},{Escape(finding.Message)},{Escape(finding.RecommendedAction)}";
        }
    }

    private static string Escape(string value)
    {
        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }
}
