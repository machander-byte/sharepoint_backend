using ZMS.Core.Models;

namespace ZMS.Application.Contracts;

public interface IMigrationService
{
    Task<IReadOnlyCollection<MigrationJob>> ListJobsAsync(CancellationToken cancellationToken);
    Task<MigrationJob?> GetJobAsync(Guid jobId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<MigrationItem>> GetJobItemsAsync(Guid jobId, CancellationToken cancellationToken);
    Task<MigrationJob> CreateJobAsync(CreateMigrationJobRequest request, CancellationToken cancellationToken);
    Task StartJobAsync(Guid jobId, CancellationToken cancellationToken);
    Task PauseJobAsync(Guid jobId, CancellationToken cancellationToken);
    Task ResumeJobAsync(Guid jobId, CancellationToken cancellationToken);
}
