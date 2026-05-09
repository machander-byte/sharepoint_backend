using ZMS.API.Contracts.Connections;
using ZMS.API.Contracts.Jobs;
using ZMS.API.Contracts.Reports;
using ZMS.Application.Contracts;
using ZMS.Core.Enums;
using ZMS.Core.Models;

namespace ZMS.API.Contracts;

public static class ApiMappings
{
    private const string GoogleRefreshTokenKey = "RefreshToken";
    private const string SharePointDocumentLibraryNameKey = "DocumentLibraryName";

    public static CreateConnectionRequest ToApplicationRequest(this CreateConnectionRequestDto dto)
    {
        return new CreateConnectionRequest
        {
            Name = dto.Name,
            Type = dto.Type,
            Url = dto.Url,
            Username = dto.Username,
            Password = dto.Password,
            ClientId = dto.ClientId,
            ClientSecret = dto.ClientSecret,
            TenantId = dto.TenantId,
            RootPath = dto.RootPath,
            AdditionalSettings = new Dictionary<string, string>(dto.AdditionalSettings, StringComparer.OrdinalIgnoreCase)
        };
    }

    public static CreateMigrationJobRequest ToApplicationRequest(this CreateMigrationJobRequestDto dto)
    {
        return new CreateMigrationJobRequest
        {
            Name = dto.Name,
            SourceConnectionId = dto.SourceConnectionId,
            TargetConnectionId = dto.TargetConnectionId,
            SourceLocation = dto.SourceLocation,
            SourceLibraryName = dto.SourceLibraryName,
            TargetSiteUrl = dto.TargetSiteUrl,
            TargetLibraryName = dto.TargetLibraryName,
            TargetLibraryUrlSegment = dto.TargetLibraryUrlSegment,
            TargetRootPath = dto.TargetRootPath,
            PreserveMetadata = dto.PreserveMetadata,
            BatchSize = dto.BatchSize,
            MaxRetryCount = dto.MaxRetryCount
        };
    }

    public static ConnectionResponseDto ToResponse(this ConnectionProfile connection)
    {
        return new ConnectionResponseDto
        {
            Id = connection.Id,
            Name = connection.Name,
            Type = connection.Type,
            Url = connection.Url,
            RootPath = connection.RootPath,
            DocumentLibraryName = connection.AdditionalSettings.TryGetValue(SharePointDocumentLibraryNameKey, out var documentLibraryName)
                ? documentLibraryName
                : null,
            HasClientSecret = connection.Type != ConnectionType.GoogleDrive
                && !string.IsNullOrWhiteSpace(connection.ClientSecret),
            HasRefreshToken = connection.Type != ConnectionType.GoogleDrive
                && connection.AdditionalSettings.TryGetValue(GoogleRefreshTokenKey, out var refreshToken)
                && !string.IsNullOrWhiteSpace(refreshToken),
            IsEnabled = connection.IsEnabled,
            CreatedUtc = connection.CreatedUtc,
            UpdatedUtc = connection.UpdatedUtc
        };
    }

    public static ConnectionTestResponseDto ToResponse(this ConnectionTestResult result)
    {
        return new ConnectionTestResponseDto
        {
            IsSuccess = result.IsSuccess,
            Message = result.Message,
            TestedUtc = result.TestedUtc
        };
    }

    public static MigrationJobResponseDto ToResponse(this MigrationJob job)
    {
        return new MigrationJobResponseDto
        {
            Id = job.Id,
            Name = job.Name,
            SourceConnectionId = job.SourceConnectionId,
            TargetConnectionId = job.TargetConnectionId,
            SourceLocation = job.SourceLocation,
            SourceLibraryName = job.SourceLibraryName,
            TargetSiteUrl = job.TargetSiteUrl,
            TargetLibraryName = job.TargetLibraryName,
            TargetLibraryUrlSegment = job.TargetLibraryUrlSegment,
            TargetRootPath = job.TargetRootPath,
            PreserveMetadata = job.PreserveMetadata,
            BatchSize = job.BatchSize,
            MaxRetryCount = job.MaxRetryCount,
            Status = job.Status,
            TotalItems = job.TotalItems,
            CompletedItems = job.CompletedItems,
            FailedItems = job.FailedItems,
            LastError = job.LastError,
            CreatedUtc = job.CreatedUtc,
            StartedUtc = job.StartedUtc,
            FinishedUtc = job.FinishedUtc,
            UpdatedUtc = job.UpdatedUtc
        };
    }

    public static MigrationItemResponseDto ToResponse(this MigrationItem item)
    {
        return new MigrationItemResponseDto
        {
            Id = item.Id,
            JobId = item.JobId,
            FileName = item.FileName,
            SourcePath = item.SourcePath,
            TargetPath = item.TargetPath,
            FileSizeInBytes = item.FileSizeInBytes,
            Metadata = new Dictionary<string, string>(item.Metadata, StringComparer.OrdinalIgnoreCase),
            Status = item.Status,
            RetryCount = item.RetryCount,
            ErrorMessage = item.ErrorMessage,
            CreatedUtc = item.CreatedUtc,
            StartedUtc = item.StartedUtc,
            CompletedUtc = item.CompletedUtc
        };
    }

    public static LogEntryResponseDto ToResponse(this LogEntry logEntry)
    {
        return new LogEntryResponseDto
        {
            Id = logEntry.Id,
            JobId = logEntry.JobId,
            ItemId = logEntry.ItemId,
            Severity = logEntry.Severity,
            Message = logEntry.Message,
            Details = logEntry.Details,
            CreatedUtc = logEntry.CreatedUtc
        };
    }

    public static JobReportResponseDto ToResponse(this JobReport report)
    {
        return new JobReportResponseDto
        {
            JobId = report.JobId,
            JobName = report.JobName,
            Status = report.Status,
            TotalItems = report.TotalItems,
            CompletedItems = report.CompletedItems,
            FailedItems = report.FailedItems,
            RetryQueuedItems = report.RetryQueuedItems,
            ProgressPercentage = report.ProgressPercentage,
            FailedItemsList = report.FailedItemsList.Select(item => item.ToResponse()).ToArray(),
            RecentLogs = report.RecentLogs.Select(log => log.ToResponse()).ToArray()
        };
    }
}
