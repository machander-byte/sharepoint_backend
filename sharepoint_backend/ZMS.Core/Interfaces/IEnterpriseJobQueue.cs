using ZMS.Core.Models;

namespace ZMS.Core.Interfaces;

public interface IMigrationJobQueue
{
    ValueTask EnqueueAsync(Guid jobId, string? correlationId, CancellationToken cancellationToken);
    ValueTask<MigrationJobLease?> DequeueAsync(CancellationToken cancellationToken);
    ValueTask RenewLeaseAsync(Guid jobId, string leaseId, CancellationToken cancellationToken);
    ValueTask CompleteAsync(Guid jobId, string leaseId, CancellationToken cancellationToken);
    ValueTask FailAsync(Guid jobId, string leaseId, string reason, CancellationToken cancellationToken);
    ValueTask RetryAsync(Guid jobId, string leaseId, CancellationToken cancellationToken);
    ValueTask DeadLetterAsync(Guid jobId, string leaseId, string reason, CancellationToken cancellationToken);
}

public interface IQueueDiagnostics
{
    string Provider { get; }
    bool IsConfigured { get; }
    int PendingCount { get; }
    int ActiveLeaseCount { get; }
    int DeadLetterCount { get; }
    string StatusMessage { get; }
}

public interface IMigrationWorker
{
    Task ProcessAsync(MigrationJobLease lease, CancellationToken cancellationToken);
}

public interface IJobLeaseService
{
    string CreateLeaseId();
    DateTimeOffset GetLeaseExpiry(DateTimeOffset now);
}

public interface IJobCheckpointService
{
    Task SaveCheckpointAsync(Guid jobId, string checkpointJson, CancellationToken cancellationToken);
    Task<string?> GetCheckpointAsync(Guid jobId, CancellationToken cancellationToken);
}

public sealed class MigrationJobLease
{
    public Guid JobId { get; set; }
    public string LeaseId { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public int Attempt { get; set; }
    public DateTimeOffset LeasedUntilUtc { get; set; }
}
