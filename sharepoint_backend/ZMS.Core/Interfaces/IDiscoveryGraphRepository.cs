using ZMS.Core.Models;

namespace ZMS.Core.Interfaces;

public interface IDiscoveryGraphRepository
{
    Task SaveRunAsync(
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
        CancellationToken cancellationToken);

    Task<DiscoveryRun?> GetRunAsync(Guid runId, CancellationToken cancellationToken);
    Task<DiscoveryRun?> GetLatestRunAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<RiskFinding>> GetRisksAsync(Guid runId, CancellationToken cancellationToken);
}
