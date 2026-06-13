using ZMS.Core.Interfaces;

namespace ZMS.MigrationEngine.Processing;

public sealed class NotConfiguredMigrationJobQueue : IMigrationJobQueue, IQueueDiagnostics
{
    private readonly string _provider;
    private readonly string _message;

    public NotConfiguredMigrationJobQueue(string provider, string message)
    {
        _provider = provider;
        _message = message;
    }

    public ValueTask EnqueueAsync(Guid jobId, string? correlationId, CancellationToken cancellationToken)
        => throw CreateException();

    public ValueTask<MigrationJobLease?> DequeueAsync(CancellationToken cancellationToken)
        => throw CreateException();

    public ValueTask RenewLeaseAsync(Guid jobId, string leaseId, CancellationToken cancellationToken)
        => throw CreateException();

    public ValueTask CompleteAsync(Guid jobId, string leaseId, CancellationToken cancellationToken)
        => throw CreateException();

    public ValueTask FailAsync(Guid jobId, string leaseId, string reason, CancellationToken cancellationToken)
        => throw CreateException();

    public ValueTask RetryAsync(Guid jobId, string leaseId, CancellationToken cancellationToken)
        => throw CreateException();

    public ValueTask DeadLetterAsync(Guid jobId, string leaseId, string reason, CancellationToken cancellationToken)
        => throw CreateException();

    public string Provider => _provider;
    public bool IsConfigured => false;
    public int PendingCount => 0;
    public int ActiveLeaseCount => 0;
    public int DeadLetterCount => 0;
    public string StatusMessage => _message;

    private InvalidOperationException CreateException() => new(_message);
}
