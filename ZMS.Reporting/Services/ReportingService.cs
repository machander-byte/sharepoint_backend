using ZMS.Core.Enums;
using ZMS.Core.Interfaces;
using ZMS.Core.Models;
using System.Text;

namespace ZMS.Reporting.Services;

public class ReportingService : IReportingService
{
    private readonly IMigrationJobRepository _jobRepository;
    private readonly IMigrationItemRepository _itemRepository;
    private readonly ILogRepository _logRepository;

    public ReportingService(
        IMigrationJobRepository jobRepository,
        IMigrationItemRepository itemRepository,
        ILogRepository logRepository)
    {
        _jobRepository = jobRepository;
        _itemRepository = itemRepository;
        _logRepository = logRepository;
    }

    public async Task<JobReport?> GetJobReportAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await _jobRepository.GetByIdAsync(jobId, cancellationToken);
        if (job is null)
        {
            return null;
        }

        var items = await _itemRepository.GetByJobIdAsync(jobId, cancellationToken);
        var logs = await _logRepository.ListByJobIdAsync(jobId, 100, cancellationToken);
        var totalItems = Math.Max(1, job.TotalItems);

        return new JobReport
        {
            JobId = job.Id,
            JobName = job.Name,
            Status = job.Status,
            TotalItems = job.TotalItems,
            CompletedItems = job.CompletedItems,
            FailedItems = job.FailedItems,
            RetryQueuedItems = items.Count(item => item.Status == MigrationItemStatus.RetryQueued),
            ProgressPercentage = Math.Round((double)job.CompletedItems / totalItems * 100, 2),
            FailedItemsList = items.Where(item => item.Status == MigrationItemStatus.Failed).ToArray(),
            RecentLogs = logs
        };
    }

    public async Task<ReportFile> ExportJobsCsvAsync(CancellationToken cancellationToken)
    {
        var jobs = await _jobRepository.ListAsync(cancellationToken);
        var csv = new CsvBuilder(
            "JobId",
            "Name",
            "Status",
            "SourceLocation",
            "SourceLibraryName",
            "TargetSiteUrl",
            "TargetLibraryName",
            "TargetLibraryUrlSegment",
            "TargetRootPath",
            "TotalItems",
            "CompletedItems",
            "FailedItems",
            "PreserveMetadata",
            "CreatedUtc",
            "StartedUtc",
            "FinishedUtc",
            "UpdatedUtc",
            "LastError");

        foreach (var job in jobs)
        {
            csv.AddRow(
                job.Id,
                job.Name,
                job.Status,
                job.SourceLocation,
                job.SourceLibraryName,
                job.TargetSiteUrl,
                job.TargetLibraryName,
                job.TargetLibraryUrlSegment,
                job.TargetRootPath,
                job.TotalItems,
                job.CompletedItems,
                job.FailedItems,
                job.PreserveMetadata,
                job.CreatedUtc,
                job.StartedUtc,
                job.FinishedUtc,
                job.UpdatedUtc,
                job.LastError);
        }

        return CreateCsvFile("migration-runs.csv", csv);
    }

    public async Task<ReportFile?> ExportJobSummaryCsvAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await _jobRepository.GetByIdAsync(jobId, cancellationToken);
        if (job is null)
        {
            return null;
        }

        var items = await _itemRepository.GetByJobIdAsync(jobId, cancellationToken);
        var logs = await _logRepository.ListByJobIdAsync(jobId, 0, cancellationToken);
        var totalBytes = items.Sum(item => item.FileSizeInBytes);
        var csv = new CsvBuilder("Field", "Value");

        csv.AddRow("JobId", job.Id);
        csv.AddRow("Name", job.Name);
        csv.AddRow("Status", job.Status);
        csv.AddRow("SourceLocation", job.SourceLocation);
        csv.AddRow("SourceLibraryName", job.SourceLibraryName);
        csv.AddRow("TargetSiteUrl", job.TargetSiteUrl);
        csv.AddRow("TargetLibraryName", job.TargetLibraryName);
        csv.AddRow("TargetLibraryUrlSegment", job.TargetLibraryUrlSegment);
        csv.AddRow("TargetRootPath", job.TargetRootPath);
        csv.AddRow("TotalItems", job.TotalItems);
        csv.AddRow("CompletedItems", job.CompletedItems);
        csv.AddRow("FailedItems", job.FailedItems);
        csv.AddRow("RetryQueuedItems", items.Count(item => item.Status == MigrationItemStatus.RetryQueued));
        csv.AddRow("TotalBytes", totalBytes);
        csv.AddRow("PreserveMetadata", job.PreserveMetadata);
        csv.AddRow("CreatedUtc", job.CreatedUtc);
        csv.AddRow("StartedUtc", job.StartedUtc);
        csv.AddRow("FinishedUtc", job.FinishedUtc);
        csv.AddRow("UpdatedUtc", job.UpdatedUtc);
        csv.AddRow("LastError", job.LastError);
        csv.AddRow("LogEntries", logs.Count);

        return CreateCsvFile($"{SafeFilePart(job.Name)}-summary.csv", csv);
    }

    public async Task<ReportFile?> ExportJobItemsCsvAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await _jobRepository.GetByIdAsync(jobId, cancellationToken);
        if (job is null)
        {
            return null;
        }

        var items = await _itemRepository.GetByJobIdAsync(jobId, cancellationToken);
        var csv = new CsvBuilder(
            "ItemId",
            "FileName",
            "SourcePath",
            "TargetPath",
            "FileSizeInBytes",
            "Status",
            "RetryCount",
            "ErrorMessage",
            "CreatedUtc",
            "StartedUtc",
            "CompletedUtc",
            "Metadata");

        foreach (var item in items)
        {
            csv.AddRow(
                item.Id,
                item.FileName,
                item.SourcePath,
                item.TargetPath,
                item.FileSizeInBytes,
                item.Status,
                item.RetryCount,
                item.ErrorMessage,
                item.CreatedUtc,
                item.StartedUtc,
                item.CompletedUtc,
                string.Join("; ", item.Metadata.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}={pair.Value}")));
        }

        return CreateCsvFile($"{SafeFilePart(job.Name)}-items.csv", csv);
    }

    public async Task<ReportFile?> ExportJobLogsCsvAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await _jobRepository.GetByIdAsync(jobId, cancellationToken);
        if (job is null)
        {
            return null;
        }

        var logs = await _logRepository.ListByJobIdAsync(jobId, 0, cancellationToken);
        var csv = new CsvBuilder("LogId", "JobId", "ItemId", "Severity", "Message", "Details", "CreatedUtc");

        foreach (var log in logs.OrderBy(log => log.CreatedUtc))
        {
            csv.AddRow(log.Id, log.JobId, log.ItemId, log.Severity, log.Message, log.Details, log.CreatedUtc);
        }

        return CreateCsvFile($"{SafeFilePart(job.Name)}-logs.csv", csv);
    }

    private static ReportFile CreateCsvFile(string fileName, CsvBuilder csv)
    {
        return new ReportFile
        {
            FileName = fileName,
            ContentType = "text/csv; charset=utf-8",
            Content = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(csv.ToString())
        };
    }

    private static string SafeFilePart(string value)
    {
        var cleaned = new string(value
            .Select(character => Path.GetInvalidFileNameChars().Contains(character) ? '-' : character)
            .ToArray());

        return string.IsNullOrWhiteSpace(cleaned) ? "migration-report" : cleaned.Trim();
    }

    private sealed class CsvBuilder
    {
        private readonly StringBuilder _content = new();

        public CsvBuilder(params string[] columns)
        {
            AddRow(columns);
        }

        public void AddRow(params object?[] values)
        {
            _content.AppendLine(string.Join(',', values.Select(Escape)));
        }

        public override string ToString() => _content.ToString();

        private static string Escape(object? value)
        {
            var text = value switch
            {
                null => string.Empty,
                DateTimeOffset dateTimeOffset => dateTimeOffset.ToUniversalTime().ToString("O"),
                _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty
            };

            if (!text.Contains('"') && !text.Contains(',') && !text.Contains('\n') && !text.Contains('\r'))
            {
                return text;
            }

            return $"\"{text.Replace("\"", "\"\"")}\"";
        }
    }
}
