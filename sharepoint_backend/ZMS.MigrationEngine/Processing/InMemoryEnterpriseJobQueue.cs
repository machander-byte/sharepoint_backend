using System.Threading.Channels;
using ZMS.Core.Interfaces;
using ZMS.Core.Models;

namespace ZMS.MigrationEngine.Processing;

public class InMemoryEnterpriseJobQueue : IMigrationJobQueue, IQueueDiagnostics
{
    private const int MaxAttempts = 3;
    private readonly Channel<MigrationJobLease> _channel = Channel.CreateUnbounded<MigrationJobLease>();
    private readonly Dictionary<Guid, MigrationJobLease> _activeLeases = new();
    private readonly Dictionary<Guid, int> _attempts = new();
    private readonly HashSet<Guid> _deadLettered = [];
    private readonly IJobLeaseService _leaseService;
    private readonly object _lock = new();
    private int _pendingCount;

    public InMemoryEnterpriseJobQueue(IJobLeaseService leaseService)
    {
        _leaseService = leaseService;
    }

    public async ValueTask EnqueueAsync(Guid jobId, string? correlationId, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            if (_deadLettered.Contains(jobId))
            {
                return;
            }

            _pendingCount++;
        }

        await _channel.Writer.WriteAsync(CreateLease(jobId, correlationId), cancellationToken);
    }

    public async ValueTask<MigrationJobLease?> DequeueAsync(CancellationToken cancellationToken)
    {
        var lease = await _channel.Reader.ReadAsync(cancellationToken);
        lock (_lock)
        {
            _pendingCount = Math.Max(0, _pendingCount - 1);
            _activeLeases[lease.JobId] = lease;
        }

        return lease;
    }

    public ValueTask RenewLeaseAsync(Guid jobId, string leaseId, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            if (_activeLeases.TryGetValue(jobId, out var lease) && lease.LeaseId == leaseId)
            {
                lease.LeasedUntilUtc = _leaseService.GetLeaseExpiry(DateTimeOffset.UtcNow);
            }
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask CompleteAsync(Guid jobId, string leaseId, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            if (_activeLeases.TryGetValue(jobId, out var lease) && lease.LeaseId == leaseId)
            {
                _activeLeases.Remove(jobId);
                _attempts.Remove(jobId);
            }
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask FailAsync(Guid jobId, string leaseId, string reason, CancellationToken cancellationToken)
    {
        return RetryAsync(jobId, leaseId, cancellationToken);
    }

    public async ValueTask RetryAsync(Guid jobId, string leaseId, CancellationToken cancellationToken)
    {
        MigrationJobLease? nextLease = null;
        lock (_lock)
        {
            if (!_activeLeases.TryGetValue(jobId, out var lease) || lease.LeaseId != leaseId)
            {
                return;
            }

            _activeLeases.Remove(jobId);
            var nextAttempt = lease.Attempt + 1;

            if (nextAttempt > MaxAttempts)
            {
                _deadLettered.Add(jobId);
                return;
            }

            _attempts[jobId] = nextAttempt - 1;
            nextLease = CreateLease(jobId, lease.CorrelationId);
            nextLease.Attempt = nextAttempt;
            _pendingCount++;
        }

        await _channel.Writer.WriteAsync(nextLease, cancellationToken);
    }

    public ValueTask DeadLetterAsync(Guid jobId, string leaseId, string reason, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            if (_activeLeases.TryGetValue(jobId, out var lease) && lease.LeaseId == leaseId)
            {
                _activeLeases.Remove(jobId);
            }

            _deadLettered.Add(jobId);
        }

        return ValueTask.CompletedTask;
    }

    private MigrationJobLease CreateLease(Guid jobId, string? correlationId)
    {
        return new MigrationJobLease
        {
            JobId = jobId,
            LeaseId = _leaseService.CreateLeaseId(),
            CorrelationId = correlationId,
            Attempt = _attempts.GetValueOrDefault(jobId) + 1,
            LeasedUntilUtc = _leaseService.GetLeaseExpiry(DateTimeOffset.UtcNow)
        };
    }

    public string Provider => "Local";
    public bool IsConfigured => true;

    public int PendingCount
    {
        get
        {
            lock (_lock)
            {
                return _pendingCount;
            }
        }
    }

    public int ActiveLeaseCount
    {
        get
        {
            lock (_lock)
            {
                return _activeLeases.Count;
            }
        }
    }

    public int DeadLetterCount
    {
        get
        {
            lock (_lock)
            {
                return _deadLettered.Count;
            }
        }
    }

    public string StatusMessage => "Local in-memory enterprise queue is configured.";
}
