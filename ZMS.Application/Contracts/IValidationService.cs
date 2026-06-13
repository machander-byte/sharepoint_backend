using ZMS.Core.Models;

namespace ZMS.Application.Contracts;

public interface IValidationService
{
    Task<ValidationRun> StartAsync(Guid migrationJobId, string userId, CancellationToken cancellationToken);
    Task<ValidationRun?> GetRunAsync(Guid validationRunId, CancellationToken cancellationToken);
    Task<ValidationRun?> GetLatestForJobAsync(Guid migrationJobId, string userId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ValidationFinding>> GetFindingsAsync(Guid validationRunId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ValidationItemResult>> GetItemsAsync(Guid validationRunId, CancellationToken cancellationToken);
    Task<ReportFile?> ExportAsync(Guid validationRunId, string exportType, CancellationToken cancellationToken);
}
