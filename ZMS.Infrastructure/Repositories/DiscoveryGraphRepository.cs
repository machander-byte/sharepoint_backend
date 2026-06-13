using Microsoft.EntityFrameworkCore;
using ZMS.Core.Interfaces;
using ZMS.Core.Models;
using ZMS.Infrastructure.Persistence;

namespace ZMS.Infrastructure.Repositories;

public class DiscoveryGraphRepository : IDiscoveryGraphRepository
{
    private readonly ZmsDbContext _dbContext;

    public DiscoveryGraphRepository(ZmsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SaveRunAsync(
        DiscoveryRun run,
        IEnumerable<DiscoveredSite> sites,
        IEnumerable<DiscoveredWeb> webs,
        IEnumerable<DiscoveredLibrary> libraries,
        IEnumerable<DiscoveredListEntity> lists,
        IEnumerable<DiscoveredFolderEntity> folders,
        IEnumerable<DiscoveredFileEntity> files,
        IEnumerable<DiscoveredPermission> permissions,
        IEnumerable<DiscoveredSharingLink> sharingLinks,
        IEnumerable<DiscoveredMetadataFieldEntity> metadataFields,
        IEnumerable<DiscoveredContentType> contentTypes,
        IEnumerable<RiskFinding> riskFindings,
        CancellationToken cancellationToken)
    {
        var existingRun = await _dbContext.DiscoveryRuns.FirstOrDefaultAsync(item => item.Id == run.Id, cancellationToken);
        if (existingRun is not null)
        {
            _dbContext.DiscoveryRuns.Remove(existingRun);
            await RemoveExistingGraphAsync(run.Id, cancellationToken);
        }

        _dbContext.DiscoveryRuns.Add(run);
        _dbContext.DiscoveredSites.AddRange(sites);
        _dbContext.DiscoveredWebs.AddRange(webs);
        _dbContext.DiscoveredLibraries.AddRange(libraries);
        _dbContext.DiscoveredLists.AddRange(lists);
        _dbContext.DiscoveredFolders.AddRange(folders);
        _dbContext.DiscoveredFiles.AddRange(files);
        _dbContext.DiscoveredPermissions.AddRange(permissions);
        _dbContext.DiscoveredSharingLinks.AddRange(sharingLinks);
        _dbContext.DiscoveredMetadataFields.AddRange(metadataFields);
        _dbContext.DiscoveredContentTypes.AddRange(contentTypes);
        _dbContext.RiskFindings.AddRange(riskFindings);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<DiscoveryRun?> GetRunAsync(Guid runId, CancellationToken cancellationToken)
    {
        return _dbContext.DiscoveryRuns.AsNoTracking().FirstOrDefaultAsync(run => run.Id == runId, cancellationToken);
    }

    public Task<DiscoveryRun?> GetLatestRunAsync(CancellationToken cancellationToken)
    {
        return _dbContext.DiscoveryRuns
            .AsNoTracking()
            .OrderByDescending(run => run.CompletedAt ?? run.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<RiskFinding>> GetRisksAsync(Guid runId, CancellationToken cancellationToken)
    {
        return await _dbContext.RiskFindings
            .AsNoTracking()
            .Where(finding => finding.DiscoveryRunId == runId)
            .OrderByDescending(finding => finding.Severity)
            .ThenBy(finding => finding.Category)
            .ToArrayAsync(cancellationToken);
    }

    private async Task RemoveExistingGraphAsync(Guid runId, CancellationToken cancellationToken)
    {
        _dbContext.RiskFindings.RemoveRange(_dbContext.RiskFindings.Where(item => item.DiscoveryRunId == runId));
        _dbContext.DiscoveredContentTypes.RemoveRange(_dbContext.DiscoveredContentTypes.Where(item => item.DiscoveryRunId == runId));
        _dbContext.DiscoveredMetadataFields.RemoveRange(_dbContext.DiscoveredMetadataFields.Where(item => item.DiscoveryRunId == runId));
        _dbContext.DiscoveredSharingLinks.RemoveRange(_dbContext.DiscoveredSharingLinks.Where(item => item.DiscoveryRunId == runId));
        _dbContext.DiscoveredPermissions.RemoveRange(_dbContext.DiscoveredPermissions.Where(item => item.DiscoveryRunId == runId));
        _dbContext.DiscoveredFiles.RemoveRange(_dbContext.DiscoveredFiles.Where(item => item.DiscoveryRunId == runId));
        _dbContext.DiscoveredFolders.RemoveRange(_dbContext.DiscoveredFolders.Where(item => item.DiscoveryRunId == runId));
        _dbContext.DiscoveredLists.RemoveRange(_dbContext.DiscoveredLists.Where(item => item.DiscoveryRunId == runId));
        _dbContext.DiscoveredLibraries.RemoveRange(_dbContext.DiscoveredLibraries.Where(item => item.DiscoveryRunId == runId));
        _dbContext.DiscoveredWebs.RemoveRange(_dbContext.DiscoveredWebs.Where(item => item.DiscoveryRunId == runId));
        _dbContext.DiscoveredSites.RemoveRange(_dbContext.DiscoveredSites.Where(item => item.DiscoveryRunId == runId));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
