using ZMS.Application.Contracts;
using ZMS.Core.Interfaces;
using ZMS.Core.Models;

namespace ZMS.Application.Services;

public class DiscoveryService : IDiscoveryService
{
    private readonly IConnectionRepository _connectionRepository;
    private readonly ConnectorResolver _connectorResolver;
    private readonly ISecretProtector _secretProtector;

    public DiscoveryService(
        IConnectionRepository connectionRepository,
        ConnectorResolver connectorResolver,
        ISecretProtector secretProtector)
    {
        _connectionRepository = connectionRepository;
        _connectorResolver = connectorResolver;
        _secretProtector = secretProtector;
    }

    public async Task<IReadOnlyCollection<SiteInfo>> GetSitesAsync(Guid sourceConnectionId, CancellationToken cancellationToken)
    {
        var connection = await GetSourceConnectionAsync(sourceConnectionId, cancellationToken);
        var connector = _connectorResolver.ResolveSource(connection);
        return await connector.GetSitesAsync(connection, cancellationToken);
    }

    public async Task<IReadOnlyCollection<LibraryInfo>> GetLibrariesAsync(
        Guid sourceConnectionId,
        string sourceLocation,
        CancellationToken cancellationToken)
    {
        var connection = await GetSourceConnectionAsync(sourceConnectionId, cancellationToken);
        var connector = _connectorResolver.ResolveSource(connection);
        return await connector.GetLibrariesAsync(connection, sourceLocation, cancellationToken);
    }

    public async Task<DiscoverySummary> GetSummaryAsync(
        Guid sourceConnectionId,
        string sourceLocation,
        string? libraryName,
        CancellationToken cancellationToken)
    {
        var connection = await GetSourceConnectionAsync(sourceConnectionId, cancellationToken);
        var connector = _connectorResolver.ResolveSource(connection);
        var sites = await connector.GetSitesAsync(connection, cancellationToken);
        var libraries = await connector.GetLibrariesAsync(connection, sourceLocation, cancellationToken);
        var files = await connector.GetFilesAsync(connection, sourceLocation, libraryName, cancellationToken);

        return new DiscoverySummary
        {
            SiteCount = sites.Count,
            LibraryCount = libraries.Count,
            FileCount = files.Count,
            TotalBytes = files.Sum(file => file.SizeInBytes)
        };
    }

    private async Task<ConnectionProfile> GetSourceConnectionAsync(Guid sourceConnectionId, CancellationToken cancellationToken)
    {
        var connection = await _connectionRepository.GetByIdAsync(sourceConnectionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Source connection '{sourceConnectionId}' was not found.");

        if (!_connectorResolver.CanResolveSource(connection.Type))
        {
            throw new InvalidOperationException($"Connection '{connection.Name}' is not configured as a source connector.");
        }

        return connection.WithUnprotectedSecrets(_secretProtector);
    }
}
