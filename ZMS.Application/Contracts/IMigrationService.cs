using ZMS.Core.Models;

namespace ZMS.Application.Contracts;

public interface IMigrationService
{
    Task<IReadOnlyCollection<MigrationJob>> ListJobsAsync(string userId, CancellationToken cancellationToken);
    Task<MigrationJob?> GetJobAsync(Guid jobId, string userId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<MigrationItem>> GetJobItemsAsync(Guid jobId, string userId, CancellationToken cancellationToken);
    Task<MigrationJob> CreateJobAsync(CreateMigrationJobRequest request, string userId, CancellationToken cancellationToken);
    Task StartJobAsync(Guid jobId, string userId, CancellationToken cancellationToken);
    Task PauseJobAsync(Guid jobId, string userId, CancellationToken cancellationToken);
    Task ResumeJobAsync(Guid jobId, string userId, CancellationToken cancellationToken);
}
