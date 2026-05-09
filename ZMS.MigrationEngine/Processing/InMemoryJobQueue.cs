using System.Threading.Channels;
using ZMS.Core.Interfaces;

namespace ZMS.MigrationEngine.Processing;

public class InMemoryJobQueue : IJobQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>();
    private readonly HashSet<Guid> _scheduledJobs = [];
    private readonly object _lock = new();

    public async ValueTask EnqueueAsync(Guid jobId, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            if (!_scheduledJobs.Add(jobId))
            {
                return;
            }
        }

        await _channel.Writer.WriteAsync(jobId, cancellationToken);
    }

    public async ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken)
    {
        var jobId = await _channel.Reader.ReadAsync(cancellationToken);

        lock (_lock)
        {
            _scheduledJobs.Remove(jobId);
        }

        return jobId;
    }
}
