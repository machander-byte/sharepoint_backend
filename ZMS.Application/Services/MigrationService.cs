using ZMS.Application.Contracts;
using ZMS.Core.Enums;
using ZMS.Core.Interfaces;
using ZMS.Core.Models;
using ZMS.Core.Options;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using ZMS.Core.Security;

namespace ZMS.Application.Services;

public class MigrationService : IMigrationService
{
    private const string SharePointDocumentLibraryNameKey = "DocumentLibraryName";

    private readonly IConnectionRepository _connectionRepository;
    private readonly IMigrationJobRepository _jobRepository;
    private readonly IMigrationItemRepository _itemRepository;
    private readonly ILogRepository _logRepository;
    private readonly IMigrationJobEventRepository _jobEventRepository;
    private readonly IJobQueue _jobQueue;
    private readonly ConnectorResolver _connectorResolver;
    private readonly ISecretProtector _secretProtector;
    private readonly MigrationEngineOptions _migrationEngineOptions;
    private readonly IEnterpriseJobStateMachine _stateMachine;
    private readonly ILogger<MigrationService> _logger;

    public MigrationService(
        IConnectionRepository connectionRepository,
        IMigrationJobRepository jobRepository,
        IMigrationItemRepository itemRepository,
        ILogRepository logRepository,
        IMigrationJobEventRepository jobEventRepository,
        IJobQueue jobQueue,
        ConnectorResolver connectorResolver,
        ISecretProtector secretProtector,
        IOptions<MigrationEngineOptions> migrationEngineOptions,
        IEnterpriseJobStateMachine stateMachine,
        ILogger<MigrationService> logger)
    {
        _connectionRepository = connectionRepository;
        _jobRepository = jobRepository;
        _itemRepository = itemRepository;
        _logRepository = logRepository;
        _jobEventRepository = jobEventRepository;
        _jobQueue = jobQueue;
        _connectorResolver = connectorResolver;
        _secretProtector = secretProtector;
        _migrationEngineOptions = migrationEngineOptions.Value;
        _stateMachine = stateMachine;
        _logger = logger;
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
            EnterpriseState = EnterpriseJobState.CREATED,
            CorrelationId = Guid.NewGuid().ToString("N"),
            CreatedUtc = DateTimeOffset.UtcNow,
            UpdatedUtc = DateTimeOffset.UtcNow
        };

        await _jobRepository.AddAsync(job, cancellationToken);
        await WriteLogAsync(job.Id, null, LogSeverity.Information, $"Job '{job.Name}' was created.", null, cancellationToken);
        await WriteJobEventAsync(job, "JobCreated", null, job.EnterpriseState, $"Job '{job.Name}' was created.", EnterpriseSeverity.Info, cancellationToken);
        _logger.LogInformation("Migration job {MigrationJobId} created with correlation {CorrelationId}.", job.Id, job.CorrelationId);

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

        await TransitionJobAsync(job, EnterpriseJobState.QUEUED, "JobQueued", "The job was queued for processing.", EnterpriseSeverity.Info, cancellationToken);
        job.Status = JobStatus.Queued;
        job.StartedUtc ??= DateTimeOffset.UtcNow;
        job.UpdatedUtc = DateTimeOffset.UtcNow;
        await _jobRepository.UpdateAsync(job, cancellationToken);

        await WriteLogAsync(job.Id, null, LogSeverity.Information, "The job was queued for processing.", null, cancellationToken);
        _logger.LogInformation("Migration job {MigrationJobId} queued with state {EnterpriseState}.", job.Id, job.EnterpriseState);
        await _jobQueue.EnqueueAsync(job.Id, cancellationToken);
    }

    public async Task PauseJobAsync(Guid jobId, string userId, CancellationToken cancellationToken)
    {
        var job = await RequireJobAsync(jobId, userId, cancellationToken);
        await TransitionJobAsync(job, EnterpriseJobState.PAUSED, "JobPaused", "The job was paused.", EnterpriseSeverity.Warning, cancellationToken);
        job.Status = JobStatus.Paused;
        job.UpdatedUtc = DateTimeOffset.UtcNow;
        await _jobRepository.UpdateAsync(job, cancellationToken);
        await WriteLogAsync(job.Id, null, LogSeverity.Warning, "The job was paused.", null, cancellationToken);
        _logger.LogWarning("Migration job {MigrationJobId} paused.", job.Id);
    }

    public async Task ResumeJobAsync(Guid jobId, string userId, CancellationToken cancellationToken)
    {
        var job = await RequireJobAsync(jobId, userId, cancellationToken);
        await TransitionJobAsync(job, EnterpriseJobState.QUEUED, "JobResumed", "The job was resumed and queued.", EnterpriseSeverity.Info, cancellationToken);
        job.Status = JobStatus.Queued;
        job.UpdatedUtc = DateTimeOffset.UtcNow;
        await _jobRepository.UpdateAsync(job, cancellationToken);
        await WriteLogAsync(job.Id, null, LogSeverity.Information, "The job was resumed and queued.", null, cancellationToken);
        _logger.LogInformation("Migration job {MigrationJobId} resumed and queued.", job.Id);
        await _jobQueue.EnqueueAsync(job.Id, cancellationToken);
    }

    public async Task CancelJobAsync(Guid jobId, string userId, CancellationToken cancellationToken)
    {
        var job = await RequireJobAsync(jobId, userId, cancellationToken);
        await TransitionJobAsync(job, EnterpriseJobState.CANCELLED, "JobCancelled", "The job was cancelled.", EnterpriseSeverity.Warning, cancellationToken);
        job.Status = JobStatus.Failed;
        job.FailureReason = "Cancelled by operator.";
        job.FinishedUtc = DateTimeOffset.UtcNow;
        job.UpdatedUtc = DateTimeOffset.UtcNow;
        await _jobRepository.UpdateAsync(job, cancellationToken);
        await WriteLogAsync(job.Id, null, LogSeverity.Warning, "The job was cancelled.", null, cancellationToken);
        _logger.LogWarning("Migration job {MigrationJobId} cancelled.", job.Id);
    }

    public async Task RetryJobAsync(Guid jobId, string userId, CancellationToken cancellationToken)
    {
        var job = await RequireJobAsync(jobId, userId, cancellationToken);
        await TransitionJobAsync(job, EnterpriseJobState.QUEUED, "JobRetryQueued", "The job was queued for retry.", EnterpriseSeverity.Warning, cancellationToken);
        job.Status = JobStatus.Queued;
        job.RetryCount++;
        job.FailureReason = null;
        job.LastError = null;
        job.FinishedUtc = null;
        job.UpdatedUtc = DateTimeOffset.UtcNow;
        await _jobRepository.UpdateAsync(job, cancellationToken);
        await WriteLogAsync(job.Id, null, LogSeverity.Warning, "The job was queued for retry.", null, cancellationToken);
        _logger.LogWarning("Migration job {MigrationJobId} queued for retry {RetryCount}.", job.Id, job.RetryCount);
        await _jobQueue.EnqueueAsync(job.Id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<MigrationJobEvent>> GetTimelineAsync(Guid jobId, string userId, CancellationToken cancellationToken)
    {
        await RequireJobAsync(jobId, userId, cancellationToken);
        return await _jobEventRepository.GetByJobIdAsync(jobId, cancellationToken);
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
        var discoveredFolders = await sourceConnector.GetFoldersAsync(
            sourceConnection,
            job.SourceLocation,
            job.SourceLibraryName,
            cancellationToken);

        var discoveredFiles = await sourceConnector.GetFilesAsync(
            sourceConnection,
            job.SourceLocation,
            job.SourceLibraryName,
            cancellationToken);

        var folderItems = discoveredFolders
            .Select(folder => CreateFolderMigrationItem(job, folder))
            .Where(item => item.Metadata.TryGetValue(MigrationItemMetadataKeys.RelativePath, out var relativePath)
                && !string.IsNullOrWhiteSpace(relativePath))
            .GroupBy(item => item.Metadata[MigrationItemMetadataKeys.RelativePath], StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First());

        var fileItems = discoveredFiles.Select(file => CreateFileMigrationItem(job, file));
        var items = folderItems.Concat(fileItems).ToArray();

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
            $"Discovered {items.Length} migration item(s) for the job: {discoveredFolders.Count} folder(s), {discoveredFiles.Count} file(s).",
            null,
            cancellationToken);
    }

    private static MigrationItem CreateFolderMigrationItem(MigrationJob job, FolderItem folder)
    {
        var metadata = new Dictionary<string, string>(folder.Metadata, StringComparer.OrdinalIgnoreCase);

        var relativePath = NormalizeItemPath(
            string.IsNullOrWhiteSpace(folder.RelativePath)
                ? folder.Name
                : folder.RelativePath);

        metadata[MigrationItemMetadataKeys.ItemType] = MigrationItemMetadataKeys.ItemTypeFolder;
        metadata[MigrationItemMetadataKeys.RelativePath] = relativePath;

        return new MigrationItem
        {
            JobId = job.Id,
            FileName = string.IsNullOrWhiteSpace(folder.Name) ? Path.GetFileName(relativePath) : folder.Name,
            SourcePath = string.IsNullOrWhiteSpace(folder.SourcePath) ? relativePath : folder.SourcePath,
            FileSizeInBytes = 0,
            Metadata = metadata
        };
    }

    private static MigrationItem CreateFileMigrationItem(MigrationJob job, FileItem file)
    {
        var metadata = new Dictionary<string, string>(file.Metadata, StringComparer.OrdinalIgnoreCase);

        metadata[MigrationItemMetadataKeys.ItemType] = MigrationItemMetadataKeys.ItemTypeFile;

        if (!metadata.ContainsKey(MigrationItemMetadataKeys.RelativePath))
        {
            metadata[MigrationItemMetadataKeys.RelativePath] = NormalizeItemPath(file.Name);
        }

        return new MigrationItem
        {
            JobId = job.Id,
            FileName = file.Name,
            SourcePath = file.SourcePath,
            FileSizeInBytes = file.SizeInBytes,
            Metadata = metadata
        };
    }

    private static string NormalizeItemPath(string value)
        => value.Trim().Replace('\\', '/').Trim('/');

    private async Task<MigrationJob> RequireJobAsync(Guid jobId, string userId, CancellationToken cancellationToken)
    {
        return await _jobRepository.GetByIdAsync(jobId, userId, cancellationToken)
            ?? throw new KeyNotFoundException($"Migration job '{jobId}' was not found.");
    }

    private async Task TransitionJobAsync(
        MigrationJob job,
        EnterpriseJobState nextState,
        string eventType,
        string message,
        EnterpriseSeverity severity,
        CancellationToken cancellationToken)
    {
        var previousState = job.EnterpriseState;
        _stateMachine.ValidateTransition(previousState, nextState);

        job.EnterpriseState = nextState;
        job.UpdatedUtc = DateTimeOffset.UtcNow;
        await _jobRepository.UpdateAsync(job, cancellationToken);
        await WriteJobEventAsync(job, eventType, previousState, nextState, message, severity, cancellationToken);
    }

    private Task WriteJobEventAsync(
        MigrationJob job,
        string eventType,
        EnterpriseJobState? previousState,
        EnterpriseJobState nextState,
        string message,
        EnterpriseSeverity severity,
        CancellationToken cancellationToken)
    {
        return _jobEventRepository.AddAsync(new MigrationJobEvent
        {
            JobId = job.Id,
            EventType = eventType,
            PreviousState = previousState?.ToString(),
            NewState = nextState.ToString(),
            Message = SecretRedactor.Redact(message),
            Severity = severity,
            CreatedAt = DateTimeOffset.UtcNow,
            CorrelationId = job.CorrelationId,
            MetadataJson = "{}"
        }, cancellationToken);
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
            Message = SecretRedactor.Redact(message),
            Details = string.IsNullOrWhiteSpace(details) ? details : SecretRedactor.Redact(details),
            CreatedUtc = DateTimeOffset.UtcNow
        }, cancellationToken);
    }
}
