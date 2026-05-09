using ZMS.Application.Contracts;
using ZMS.Core.Enums;
using ZMS.Core.Interfaces;
using ZMS.Core.Models;

namespace ZMS.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IConnectionRepository _connectionRepository;
    private readonly IMigrationJobRepository _jobRepository;
    private readonly IMigrationItemRepository _itemRepository;

    public DashboardService(
        IConnectionRepository connectionRepository,
        IMigrationJobRepository jobRepository,
        IMigrationItemRepository itemRepository)
    {
        _connectionRepository = connectionRepository;
        _jobRepository = jobRepository;
        _itemRepository = itemRepository;
    }

    public async Task<DashboardSummary> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var connections = await _connectionRepository.ListAsync(cancellationToken);
        var jobs = await _jobRepository.ListAsync(cancellationToken);

        return new DashboardSummary
        {
            TotalConnections = connections.Count,
            TotalJobs = jobs.Count,
            QueuedJobs = jobs.Count(job => job.Status == JobStatus.Queued),
            RunningJobs = jobs.Count(job => job.Status == JobStatus.Running),
            CompletedJobs = jobs.Count(job => job.Status is JobStatus.Completed or JobStatus.CompletedWithErrors),
            FailedJobs = jobs.Count(job => job.Status == JobStatus.Failed),
            PendingRetryItems = await _itemRepository.CountByStatusAsync(MigrationItemStatus.RetryQueued, cancellationToken),
            GeneratedUtc = DateTimeOffset.UtcNow
        };
    }
}
