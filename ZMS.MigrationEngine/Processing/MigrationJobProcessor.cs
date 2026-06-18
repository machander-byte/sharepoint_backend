using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZMS.Application.Services;
using ZMS.Core.Enums;
using ZMS.Core.Interfaces;
using ZMS.Core.Models;
using ZMS.Core.Options;

namespace ZMS.MigrationEngine.Processing;

public class MigrationJobProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IJobQueue _jobQueue;
    private readonly ILogger<MigrationJobProcessor> _logger;
    private readonly MigrationEngineOptions _options;

    public MigrationJobProcessor(
        IServiceScopeFactory serviceScopeFactory,
        IJobQueue jobQueue,
        IOptions<MigrationEngineOptions> options,
        ILogger<MigrationJobProcessor> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _jobQueue = jobQueue;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RecoverInterruptedJobsAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var jobId = await _jobQueue.DequeueAsync(stoppingToken);
                await ProcessJobAsync(jobId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unexpected error in the migration engine loop.");
            }
        }
    }

    private async Task ProcessJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var services = scope.ServiceProvider;

        var jobRepository = services.GetRequiredService<IMigrationJobRepository>();
        var itemRepository = services.GetRequiredService<IMigrationItemRepository>();
        var connectionRepository = services.GetRequiredService<IConnectionRepository>();
        var logRepository = services.GetRequiredService<ILogRepository>();
        var jobEventRepository = services.GetRequiredService<IMigrationJobEventRepository>();
        var connectorResolver = services.GetRequiredService<ConnectorResolver>();
        var secretProtector = services.GetRequiredService<ISecretProtector>();

        var job = await jobRepository.GetByIdAsync(jobId, cancellationToken);
        if (job is null || job.Status == JobStatus.Paused)
        {
            return;
        }

        try
        {
            var sourceConnection = await connectionRepository.GetByIdAsync(job.SourceConnectionId, cancellationToken)
                ?? throw new InvalidOperationException("The source connection could not be found.");
            sourceConnection = sourceConnection.WithUnprotectedSecrets(secretProtector);

            var targetConnection = await connectionRepository.GetByIdAsync(job.TargetConnectionId, cancellationToken)
                ?? throw new InvalidOperationException("The target connection could not be found.");
            targetConnection = targetConnection.WithUnprotectedSecrets(secretProtector);

            var sourceConnector = connectorResolver.ResolveSource(sourceConnection);
            var targetConnector = connectorResolver.ResolveTarget(targetConnection);

            var sourceTest = await sourceConnector.TestConnectionAsync(sourceConnection, cancellationToken);
            var targetTest = await targetConnector.TestConnectionAsync(targetConnection, cancellationToken);

            if (!sourceTest.IsSuccess)
            {
                throw new InvalidOperationException(sourceTest.Message);
            }

            if (!targetTest.IsSuccess)
            {
                throw new InvalidOperationException(targetTest.Message);
            }

            var previousState = job.EnterpriseState;
            job.Status = JobStatus.Running;
            job.EnterpriseState = EnterpriseJobState.MIGRATING;
            job.StartedUtc ??= DateTimeOffset.UtcNow;
            job.UpdatedUtc = DateTimeOffset.UtcNow;
            await jobRepository.UpdateAsync(job, cancellationToken);
            await WriteLogAsync(logRepository, job.Id, null, LogSeverity.Information, "Migration job processing started.", null, cancellationToken);
            await WriteJobEventAsync(
                jobEventRepository,
                job,
                "JobMigrating",
                previousState,
                EnterpriseJobState.MIGRATING,
                "Migration worker started processing the job.",
                EnterpriseSeverity.Info,
                cancellationToken);

            await targetConnector.EnsureTargetSiteAsync(targetConnection, job.TargetSiteUrl, cancellationToken);
            await targetConnector.EnsureTargetLibraryAsync(
                targetConnection,
                job.TargetSiteUrl,
                job.TargetLibraryName,
                job.TargetLibraryUrlSegment,
                cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                job = await jobRepository.GetByIdAsync(jobId, cancellationToken)
                    ?? throw new InvalidOperationException("The migration job was deleted while processing.");

                if (job.Status == JobStatus.Paused)
                {
                    await WriteLogAsync(logRepository, job.Id, null, LogSeverity.Warning, "Migration job processing paused.", null, cancellationToken);
                    return;
                }

                var batch = await itemRepository.GetNextBatchAsync(job.Id, Math.Max(1, job.BatchSize), cancellationToken);
                if (batch.Count == 0)
                {
                    break;
                }

                foreach (var item in batch)
                {
                    job = await jobRepository.GetByIdAsync(jobId, cancellationToken)
                        ?? throw new InvalidOperationException("The migration job was deleted while processing.");

                    if (job.Status == JobStatus.Paused)
                    {
                        await UpdateJobSummaryAsync(jobRepository, itemRepository, job, cancellationToken);
                        await WriteLogAsync(logRepository, job.Id, null, LogSeverity.Warning, "Migration job processing paused.", null, cancellationToken);
                        return;
                    }

                    item.Status = MigrationItemStatus.InProgress;
                    item.StartedUtc = DateTimeOffset.UtcNow;
                    await itemRepository.UpdateAsync(item, cancellationToken);

                    try
                    {
                        var targetPath = item.IsFolder
                            ? await targetConnector.EnsureFolderAsync(targetConnection, job, item, cancellationToken)
                            : await UploadFileAsync(sourceConnector, targetConnector, sourceConnection, targetConnection, job, item, cancellationToken);

                        item.TargetPath = targetPath;
                        item.Status = MigrationItemStatus.Completed;
                        item.ErrorMessage = null;
                        item.CompletedUtc = DateTimeOffset.UtcNow;
                        await itemRepository.UpdateAsync(item, cancellationToken);

                        await WriteLogAsync(
                            logRepository,
                            job.Id,
                            item.Id,
                            LogSeverity.Information,
                            item.IsFolder
                                ? $"Ensured folder '{item.FileName}' successfully."
                                : $"Copied '{item.FileName}' successfully.",
                            null,
                            cancellationToken);
                    }
                    catch (Exception itemException)
                    {
                        item.RetryCount++;
                        item.ErrorMessage = itemException.Message;
                        item.CompletedUtc = DateTimeOffset.UtcNow;

                        if (item.RetryCount <= job.MaxRetryCount)
                        {
                            item.Status = MigrationItemStatus.RetryQueued;
                            await WriteLogAsync(
                                logRepository,
                                job.Id,
                                item.Id,
                                LogSeverity.Warning,
                                $"Queued '{item.FileName}' for retry attempt {item.RetryCount}.",
                                itemException.Message,
                                cancellationToken);
                        }
                        else
                        {
                            item.Status = MigrationItemStatus.Failed;
                            await WriteLogAsync(
                                logRepository,
                                job.Id,
                                item.Id,
                                LogSeverity.Error,
                                $"Failed to migrate '{item.FileName}'.",
                                itemException.Message,
                                cancellationToken);
                        }

                        await itemRepository.UpdateAsync(item, cancellationToken);

                        var retryDelay = item.Status == MigrationItemStatus.RetryQueued
                            ? CalculateRetryDelay(item.RetryCount)
                            : TimeSpan.Zero;
                        if (retryDelay > TimeSpan.Zero)
                        {
                            await Task.Delay(retryDelay, cancellationToken);
                        }
                    }
                }

                await UpdateJobSummaryAsync(jobRepository, itemRepository, job, cancellationToken);

                if (_options.BatchDelayMilliseconds > 0)
                {
                    await Task.Delay(_options.BatchDelayMilliseconds, cancellationToken);
                }
            }

            job = await jobRepository.GetByIdAsync(jobId, cancellationToken)
                ?? throw new InvalidOperationException("The migration job was deleted before completion.");

            await FinalizeJobAsync(jobRepository, itemRepository, logRepository, jobEventRepository, job, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Migration job '{JobId}' failed.", jobId);

            var failedJob = await jobRepository.GetByIdAsync(jobId, cancellationToken);
            if (failedJob is not null)
            {
                var previousState = failedJob.EnterpriseState;
                failedJob.Status = JobStatus.Failed;
                failedJob.EnterpriseState = EnterpriseJobState.FAILED_MIGRATION;
                failedJob.LastError = exception.Message;
                failedJob.FailureReason = exception.Message;
                failedJob.UpdatedUtc = DateTimeOffset.UtcNow;
                failedJob.FinishedUtc = DateTimeOffset.UtcNow;
                await jobRepository.UpdateAsync(failedJob, cancellationToken);
                await WriteJobEventAsync(
                    jobEventRepository,
                    failedJob,
                    "JobFailed",
                    previousState,
                    EnterpriseJobState.FAILED_MIGRATION,
                    "Migration job failed.",
                    EnterpriseSeverity.Error,
                    cancellationToken);
            }

            await WriteLogAsync(logRepository, jobId, null, LogSeverity.Error, "Migration job failed.", exception.Message, cancellationToken);
        }
    }

    private async Task RecoverInterruptedJobsAsync(CancellationToken cancellationToken)
    {
        if (!_options.ResumeQueuedJobsOnStartup)
        {
            return;
        }

        try
        {
            await using var scope = _serviceScopeFactory.CreateAsyncScope();
            var services = scope.ServiceProvider;
            var jobRepository = services.GetRequiredService<IMigrationJobRepository>();
            var itemRepository = services.GetRequiredService<IMigrationItemRepository>();
            var logRepository = services.GetRequiredService<ILogRepository>();

            var jobs = await jobRepository.ListAsync(cancellationToken);
            var interruptedJobs = jobs
                .Where(job => job.Status is JobStatus.Queued or JobStatus.Running)
                .ToArray();

            foreach (var job in interruptedJobs)
            {
                var items = await itemRepository.GetByJobIdAsync(job.Id, cancellationToken);
                foreach (var item in items.Where(item => item.Status == MigrationItemStatus.InProgress))
                {
                    item.Status = MigrationItemStatus.RetryQueued;
                    item.ErrorMessage = "The API restarted while this item was in progress; it was queued for a safe retry.";
                    item.CompletedUtc = null;
                    await itemRepository.UpdateAsync(item, cancellationToken);
                }

                job.Status = JobStatus.Queued;
                job.EnterpriseState = EnterpriseJobState.QUEUED;
                job.UpdatedUtc = DateTimeOffset.UtcNow;
                await jobRepository.UpdateAsync(job, cancellationToken);

                await WriteLogAsync(
                    logRepository,
                    job.Id,
                    null,
                    LogSeverity.Warning,
                    "Recovered queued/running migration after API startup.",
                    "Any in-progress items were returned to the retry queue.",
                    cancellationToken);

                await _jobQueue.EnqueueAsync(job.Id, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to recover interrupted migration jobs during startup.");
        }
    }

    private static async Task<string> UploadFileAsync(
        ISourceConnector sourceConnector,
        ITargetConnector targetConnector,
        ConnectionProfile sourceConnection,
        ConnectionProfile targetConnection,
        MigrationJob job,
        MigrationItem item,
        CancellationToken cancellationToken)
    {
        await using var content = await sourceConnector.OpenReadAsync(sourceConnection, item, cancellationToken);
        return await targetConnector.UploadFileAsync(targetConnection, job, item, content, cancellationToken);
    }

    private static async Task UpdateJobSummaryAsync(
        IMigrationJobRepository jobRepository,
        IMigrationItemRepository itemRepository,
        MigrationJob job,
        CancellationToken cancellationToken)
    {
        var items = await itemRepository.GetByJobIdAsync(job.Id, cancellationToken);
        job.TotalItems = items.Count;
        job.CompletedItems = items.Count(item => item.Status == MigrationItemStatus.Completed);
        job.FailedItems = items.Count(item => item.Status == MigrationItemStatus.Failed);
        job.UpdatedUtc = DateTimeOffset.UtcNow;
        await jobRepository.UpdateAsync(job, cancellationToken);
    }

    private static async Task FinalizeJobAsync(
        IMigrationJobRepository jobRepository,
        IMigrationItemRepository itemRepository,
        ILogRepository logRepository,
        IMigrationJobEventRepository jobEventRepository,
        MigrationJob job,
        CancellationToken cancellationToken)
    {
        var items = await itemRepository.GetByJobIdAsync(job.Id, cancellationToken);
        job.TotalItems = items.Count;
        job.CompletedItems = items.Count(item => item.Status == MigrationItemStatus.Completed);
        job.FailedItems = items.Count(item => item.Status == MigrationItemStatus.Failed);
        job.UpdatedUtc = DateTimeOffset.UtcNow;
        job.FinishedUtc = DateTimeOffset.UtcNow;
        var previousState = job.EnterpriseState;
        job.Status = job.FailedItems > 0 ? JobStatus.CompletedWithErrors : JobStatus.Completed;
        job.EnterpriseState = job.FailedItems > 0 ? EnterpriseJobState.PARTIALLY_FAILED : EnterpriseJobState.COMPLETED;

        await jobRepository.UpdateAsync(job, cancellationToken);
        await WriteLogAsync(
            logRepository,
            job.Id,
            null,
            LogSeverity.Information,
            $"Migration job finished with status '{job.Status}'.",
            null,
            cancellationToken);
        await WriteJobEventAsync(
            jobEventRepository,
            job,
            "JobFinished",
            previousState,
            job.EnterpriseState,
            $"Migration job finished with status '{job.Status}'.",
            job.FailedItems > 0 ? EnterpriseSeverity.Warning : EnterpriseSeverity.Info,
            cancellationToken);
    }

    private static Task WriteLogAsync(
        ILogRepository logRepository,
        Guid jobId,
        Guid? itemId,
        LogSeverity severity,
        string message,
        string? details,
        CancellationToken cancellationToken)
    {
        return logRepository.AddAsync(new LogEntry
        {
            JobId = jobId,
            ItemId = itemId,
            Severity = severity,
            Message = message,
            Details = details,
            CreatedUtc = DateTimeOffset.UtcNow
        }, cancellationToken);
    }

    private static Task WriteJobEventAsync(
        IMigrationJobEventRepository jobEventRepository,
        MigrationJob job,
        string eventType,
        EnterpriseJobState previousState,
        EnterpriseJobState nextState,
        string message,
        EnterpriseSeverity severity,
        CancellationToken cancellationToken)
    {
        return jobEventRepository.AddAsync(new MigrationJobEvent
        {
            JobId = job.Id,
            EventType = eventType,
            PreviousState = previousState.ToString(),
            NewState = nextState.ToString(),
            Message = message,
            Severity = severity,
            CreatedAt = DateTimeOffset.UtcNow,
            CorrelationId = job.CorrelationId,
            MetadataJson = "{}"
        }, cancellationToken);
    }

    private TimeSpan CalculateRetryDelay(int retryCount)
    {
        if (retryCount <= 0 || _options.RetryBaseDelayMilliseconds <= 0)
        {
            return TimeSpan.Zero;
        }

        var cappedRetryCount = Math.Min(retryCount - 1, 6);
        var delayMilliseconds = _options.RetryBaseDelayMilliseconds * Math.Pow(2, cappedRetryCount);
        var maxDelayMilliseconds = _options.RetryMaxDelayMilliseconds <= 0
            ? _options.RetryBaseDelayMilliseconds
            : _options.RetryMaxDelayMilliseconds;

        return TimeSpan.FromMilliseconds(Math.Min(delayMilliseconds, maxDelayMilliseconds));
    }
}
