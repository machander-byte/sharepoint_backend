using Microsoft.EntityFrameworkCore;
using ZMS.Core.Interfaces;
using ZMS.Core.Models;
using ZMS.Infrastructure.Persistence;

namespace ZMS.Infrastructure.Repositories;

public class ValidationRepository : IValidationRepository
{
    private readonly ZmsDbContext _dbContext;

    public ValidationRepository(ZmsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddRunAsync(
        ValidationRun run,
        IEnumerable<ValidationFinding> findings,
        IEnumerable<ValidationItemResult> items,
        CancellationToken cancellationToken)
    {
        _dbContext.ValidationRuns.Add(run);
        _dbContext.ValidationFindings.AddRange(findings);
        _dbContext.ValidationItemResults.AddRange(items);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<ValidationRun?> GetRunAsync(Guid validationRunId, CancellationToken cancellationToken)
    {
        return _dbContext.ValidationRuns.AsNoTracking().FirstOrDefaultAsync(run => run.Id == validationRunId, cancellationToken);
    }

    public Task<ValidationRun?> GetLatestForJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        return _dbContext.ValidationRuns
            .AsNoTracking()
            .Where(run => run.MigrationJobId == jobId)
            .OrderByDescending(run => run.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ValidationFinding>> GetFindingsAsync(Guid validationRunId, CancellationToken cancellationToken)
    {
        return await _dbContext.ValidationFindings
            .AsNoTracking()
            .Where(finding => finding.ValidationRunId == validationRunId)
            .OrderByDescending(finding => finding.Severity)
            .ThenBy(finding => finding.Category)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ValidationItemResult>> GetItemsAsync(Guid validationRunId, CancellationToken cancellationToken)
    {
        return await _dbContext.ValidationItemResults
            .AsNoTracking()
            .Where(item => item.ValidationRunId == validationRunId)
            .OrderBy(item => item.SourcePath)
            .ToArrayAsync(cancellationToken);
    }
}
