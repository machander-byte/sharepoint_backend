using ZMS.Application.Contracts;
using ZMS.Core.Enums;
using ZMS.Core.Interfaces;
using ZMS.Core.Models;
using ZMS.Core.Options;
using Microsoft.Extensions.Options;

namespace ZMS.Application.Services;

public class MigrationService : IMigrationService
{
    private const string SharePointDocumentLibraryNameKey = "DocumentLibraryName";

    private readonly IConnectionRepository _connectionRepository;
    private readonly IMigrationJobRepository _jobRepository;
    private readonly IMigrationItemRepository _itemRepository;
    private readonly ILogRepository _logRepository;
    private readonly IJobQueue _jobQueue;
    private readonly ConnectorResolver _connectorResolver;
    private readonly ISecretProtector _secretProtector;
    private readonly MigrationEngineOptions _migrationEngineOptions;

    public MigrationService(
        IConnectionRepository connectionRepository,
        IMigrationJobRepository jobRepository,
        IMigrationItemRepository itemRepository,
        ILogRepository logRepository,
        IJobQueue jobQueue,
        ConnectorResolver connectorResolver,
        ISecretProtector secretProtector,
        IOptions<MigrationEngineOptions> migrationEngineOptions)
    {
        _connectionRepository = connectionRepository;
        _jobRepository = jobRepository;
        _itemRepository = itemRepository;
        _logRepository = logRepository;
        _jobQueue = jobQueue;
        _connectorResolver = connectorResolver;
        _secretProtector = secretProtector;
        _migrationEngineOptions = migrationEngineOptions.Value;
    }

    public Task<IReadOnlyCollection<MigrationJob>> ListJobsAsync(string userId, CancellationToken cancellationToken)
        => _jobRepository.ListAsync(userId, cancellationToken);

    public Task<MigrationJob?> GetJobAsync(Guid jobId, string userId, CancellationToken cancellationToken)
        => _jobRepository.GetByIdAsync(jobId, userId, cancellationToken);

    public async Task<IReadOnlyCollection<MigrationItem>> GetJobItemsAsync(Guid jobId, string userId, CancellationToken cancellationToken)
    {
        await RequireJobAsync(jobId, userId, cancellationToken);
        return await _itemRepository.GetByJobIdAsync(jobId, cancellationToken);
    }

    public async Task<MigrationJob> CreateJobAsync(CreateMigrationJobRequest request, string userId, CancellationToken cancellationToken)
    {
        var sourceConnection = await _connectionRepository.GetByIdAsync(request.SourceConnectionId, userId, cancellationToken)
            ?? throw new KeyNotFoundException($"Source connection '{request.SourceConnectionId}' was not found.");

        var targetConnection = await _connectionRepository.GetByIdAsync(request.TargetConnectionId, userId, cancellationToken)
            ?? throw new KeyNotFoundException($"Target connection '{request.TargetConnectionId}' was not found.");

        if (!_connectorResolver.CanResolveSource(sourceConnection.Type))
        {
            throw new InvalidOperationException("The selected source connection is not backed by a source connector.");
        }

        if (!_connectorResolver.CanResolveTarget(targetConnection.Type))
        {
            throw new InvalidOperationException("The selected target connection is not backed by a target connector.");
        }

        var targetSiteUrl = ResolveTargetSiteUrl(targetConnection, request.TargetSiteUrl);
        var targetLibraryName = ResolveTargetLibraryName(targetConnection, request.TargetLibraryName);
        var sourceLibraryName = string.IsNullOrWhiteSpace(request.SourceLibraryName) ? null : request.SourceLibraryName.Trim();
        var targetRootPath = NormalizeOptionalPath(request.TargetRootPath);
        var targetLibraryUrlSegment = NormalizeOptionalPath(request.TargetLibraryUrlSegment);

        ValidateSameSiteCopy(request, sourceConnection, targetConnection, sourceLibraryName, targetSiteUrl, targetLibraryName, targetRootPath);

        var job = new MigrationJob
        {
            UserId = userId,
            Name = request.Name.Trim(),
            SourceConnectionId = request.SourceConnectionId,
            TargetConnectionId = request.TargetConnectionId,
            SourceLocation = ResolveSourceLocation(sourceConnection, request.SourceLocation),
            SourceLibraryName = sourceLibraryName,
            TargetSiteUrl = targetSiteUrl,
            TargetLibraryName = targetLibraryName,
            TargetLibraryUrlSegment = targetLibraryUrlSegment,
            TargetRootPath = targetRootPath,
            PreserveMetadata = request.PreserveMetadata,
            BatchSize = request.BatchSize > 0 ? request.BatchSize : _migrationEngineOptions.DefaultBatchSize,
            MaxRetryCount = request.MaxRetryCount >= 0 ? request.MaxRetryCount : _migrationEngineOptions.DefaultMaxRetryCount,
            Status = JobStatus.Draft,
            CreatedUtc = DateTimeOffset.UtcNow,
            UpdatedUtc = DateTimeOffset.UtcNow
        };

        await _jobRepository.AddAsync(job, cancellationToken);
        await WriteLogAsync(job.Id, null, LogSeverity.Information, $"Job '{job.Name}' was created.", null, cancellationToken);

        return job;
    }

    public async Task StartJobAsync(Guid jobId, string userId, CancellationToken cancellationToken)
    {
        var job = await RequireJobAsync(jobId, userId, cancellationToken);

        if (job.Status is JobStatus.Running or JobStatus.Queued)
        {
            return;
        }

        await EnsureItemsCreatedAsync(job, cancellationToken);

        if (job.TotalItems == 0)
        {
            job.Status = JobStatus.Completed;
            job.StartedUtc ??= DateTimeOffset.UtcNow;
            job.FinishedUtc = DateTimeOffset.UtcNow;
            job.UpdatedUtc = DateTimeOffset.UtcNow;
            await _jobRepository.UpdateAsync(job, cancellationToken);
            await WriteLogAsync(job.Id, null, LogSeverity.Warning, "The job completed without any discovered files.", null, cancellationToken);
            return;
        }

        job.Status = JobStatus.Queued;
        job.StartedUtc ??= DateTimeOffset.UtcNow;
        job.UpdatedUtc = DateTimeOffset.UtcNow;
        await _jobRepository.UpdateAsync(job, cancellationToken);

        await WriteLogAsync(job.Id, null, LogSeverity.Information, "The job was queued for processing.", null, cancellationToken);
        await _jobQueue.EnqueueAsync(job.Id, cancellationToken);
    }

    public async Task PauseJobAsync(Guid jobId, string userId, CancellationToken cancellationToken)
    {
        var job = await RequireJobAsync(jobId, userId, cancellationToken);
        job.Status = JobStatus.Paused;
        job.UpdatedUtc = DateTimeOffset.UtcNow;
        await _jobRepository.UpdateAsync(job, cancellationToken);
        await WriteLogAsync(job.Id, null, LogSeverity.Warning, "The job was paused.", null, cancellationToken);
    }

    public async Task ResumeJobAsync(Guid jobId, string userId, CancellationToken cancellationToken)
    {
        var job = await RequireJobAsync(jobId, userId, cancellationToken);
        job.Status = JobStatus.Queued;
        job.UpdatedUtc = DateTimeOffset.UtcNow;
        await _jobRepository.UpdateAsync(job, cancellationToken);
        await WriteLogAsync(job.Id, null, LogSeverity.Information, "The job was resumed and queued.", null, cancellationToken);
        await _jobQueue.EnqueueAsync(job.Id, cancellationToken);
    }

    private async Task EnsureItemsCreatedAsync(MigrationJob job, CancellationToken cancellationToken)
    {
        var existingItems = await _itemRepository.GetByJobIdAsync(job.Id, cancellationToken);
        if (existingItems.Count > 0)
        {
            job.TotalItems = existingItems.Count;
            job.CompletedItems = existingItems.Count(item => item.Status == MigrationItemStatus.Completed);
            job.FailedItems = existingItems.Count(item => item.Status == MigrationItemStatus.Failed);
            job.UpdatedUtc = DateTimeOffset.UtcNow;
            await _jobRepository.UpdateAsync(job, cancellationToken);
            return;
        }

        var sourceConnection = await _connectionRepository.GetByIdAsync(job.SourceConnectionId, job.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"Source connection '{job.SourceConnectionId}' was not found.");
        sourceConnection = sourceConnection.WithUnprotectedSecrets(_secretProtector);

        var sourceConnector = _connectorResolver.ResolveSource(sourceConnection);
        var discoveredFiles = await sourceConnector.GetFilesAsync(
            sourceConnection,
            job.SourceLocation,
            job.SourceLibraryName,
            cancellationToken);

        var items = discoveredFiles.Select(file => new MigrationItem
        {
            JobId = job.Id,
            FileName = file.Name,
            SourcePath = file.SourcePath,
            FileSizeInBytes = file.SizeInBytes,
            Metadata = job.PreserveMetadata
                ? new Dictionary<string, string>(file.Metadata, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        }).ToArray();

        if (items.Length > 0)
        {
            await _itemRepository.AddRangeAsync(items, cancellationToken);
        }

        job.TotalItems = items.Length;
        job.CompletedItems = 0;
        job.FailedItems = 0;
        job.UpdatedUtc = DateTimeOffset.UtcNow;
        await _jobRepository.UpdateAsync(job, cancellationToken);

        await WriteLogAsync(
            job.Id,
            null,
            LogSeverity.Information,
            $"Discovered {items.Length} migration item(s) for the job.",
            null,
            cancellationToken);
    }

    private async Task<MigrationJob> RequireJobAsync(Guid jobId, string userId, CancellationToken cancellationToken)
    {
        return await _jobRepository.GetByIdAsync(jobId, userId, cancellationToken)
            ?? throw new KeyNotFoundException($"Migration job '{jobId}' was not found.");
    }

    private static string ResolveSourceLocation(ConnectionProfile sourceConnection, string? requestedLocation)
    {
        if (!string.IsNullOrWhiteSpace(requestedLocation))
        {
            return requestedLocation.Trim();
        }

        return sourceConnection.Type switch
        {
            ConnectionType.FileShare => sourceConnection.RootPath ?? sourceConnection.Url,
            _ => sourceConnection.Url
        };
    }

    private static string ResolveTargetSiteUrl(ConnectionProfile targetConnection, string? requestedSiteUrl)
    {
        var targetSiteUrl = string.IsNullOrWhiteSpace(requestedSiteUrl)
            ? targetConnection.Url
            : requestedSiteUrl.Trim();

        if (string.IsNullOrWhiteSpace(targetSiteUrl))
        {
            throw new InvalidOperationException("SharePoint target site URL is required.");
        }

        return targetSiteUrl;
    }

    private static string ResolveTargetLibraryName(ConnectionProfile targetConnection, string? requestedLibraryName)
    {
        if (!string.IsNullOrWhiteSpace(requestedLibraryName))
        {
            return requestedLibraryName.Trim();
        }

        if (targetConnection.AdditionalSettings.TryGetValue(SharePointDocumentLibraryNameKey, out var savedLibraryName)
            && !string.IsNullOrWhiteSpace(savedLibraryName))
        {
            return savedLibraryName.Trim();
        }

        throw new InvalidOperationException("SharePoint target document library name is required.");
    }

    private static string? NormalizeOptionalPath(string? value)
    {
        var normalized = value?.Trim().Replace('\\', '/').Trim('/') ?? string.Empty;
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static void ValidateSameSiteCopy(
        CreateMigrationJobRequest request,
        ConnectionProfile sourceConnection,
        ConnectionProfile targetConnection,
        string? sourceLibraryName,
        string targetSiteUrl,
        string targetLibraryName,
        string? targetRootPath)
    {
        if (sourceConnection.Type != ConnectionType.SharePointOnline
            || targetConnection.Type != ConnectionType.SharePointOnline)
        {
            return;
        }

        var sourceSiteUrl = string.IsNullOrWhiteSpace(request.SourceLocation)
            ? sourceConnection.Url
            : request.SourceLocation.Trim();

        if (!string.Equals(sourceSiteUrl.TrimEnd('/'), targetSiteUrl.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var sameLibrary = string.IsNullOrWhiteSpace(sourceLibraryName)
            || string.Equals(sourceLibraryName, targetLibraryName, StringComparison.OrdinalIgnoreCase);

        if (sameLibrary && string.IsNullOrWhiteSpace(targetRootPath))
        {
            throw new InvalidOperationException(
                "Same-site SharePoint migrations must use a different target library name or a target folder path.");
        }
    }

    private Task WriteLogAsync(
        Guid jobId,
        Guid? itemId,
        LogSeverity severity,
        string message,
        string? details,
        CancellationToken cancellationToken)
    {
        return _logRepository.AddAsync(new LogEntry
        {
            JobId = jobId,
            ItemId = itemId,
            Severity = severity,
            Message = message,
            Details = details,
            CreatedUtc = DateTimeOffset.UtcNow
        }, cancellationToken);
    }
}
