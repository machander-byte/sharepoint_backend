using ZMS.Core.Models;

namespace ZMS.Core.Interfaces;

public interface IValidationRepository
{
    Task AddRunAsync(
        ValidationRun run,
        IEnumerable<ValidationFinding> findings,
        IEnumerable<ValidationItemResult> items,
        CancellationToken cancellationToken);

    Task<ValidationRun?> GetRunAsync(Guid validationRunId, CancellationToken cancellationToken);
    Task<ValidationRun?> GetLatestForJobAsync(Guid jobId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ValidationFinding>> GetFindingsAsync(Guid validationRunId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ValidationItemResult>> GetItemsAsync(Guid validationRunId, CancellationToken cancellationToken);
}
