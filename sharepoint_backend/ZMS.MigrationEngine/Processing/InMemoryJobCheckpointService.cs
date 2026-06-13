using System.Collections.Concurrent;
using ZMS.Core.Interfaces;

namespace ZMS.MigrationEngine.Processing;

public class InMemoryJobCheckpointService : IJobCheckpointService
{
    private readonly ConcurrentDictionary<Guid, string> _checkpoints = new();

    public Task SaveCheckpointAsync(Guid jobId, string checkpointJson, CancellationToken cancellationToken)
    {
        _checkpoints[jobId] = checkpointJson;
        return Task.CompletedTask;
    }

    public Task<string?> GetCheckpointAsync(Guid jobId, CancellationToken cancellationToken)
    {
        _checkpoints.TryGetValue(jobId, out var checkpoint);
        return Task.FromResult(checkpoint);
    }
}
