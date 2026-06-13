namespace ZMS.Application.Contracts;

public interface IAiAdvisorService
{
    Task<AiAdvisorResponse> AskAsync(AiAdvisorRequest request, string userId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<RemediationItem>> GetDiscoveryRemediationAsync(string discoveryRunId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<RemediationItem>> GetMigrationRemediationAsync(Guid jobId, string userId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<RemediationItem>> GetValidationRemediationAsync(Guid validationRunId, CancellationToken cancellationToken);
    Task<EtaEstimate> GetMigrationEtaAsync(Guid jobId, string userId, CancellationToken cancellationToken);
    Task<EtaEstimate> GetDiscoveryEtaAsync(string discoveryRunId, CancellationToken cancellationToken);
}
