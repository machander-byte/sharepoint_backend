using ZMS.Core.Enums;
using ZMS.Core.Interfaces;
using ZMS.Core.Models;

namespace ZMS.Application.Services;

public class ConnectorResolver
{
    private readonly IReadOnlyDictionary<ConnectionType, ISourceConnector> _sourceConnectors;
    private readonly IReadOnlyDictionary<ConnectionType, ITargetConnector> _targetConnectors;

    public ConnectorResolver(IEnumerable<ISourceConnector> sourceConnectors, IEnumerable<ITargetConnector> targetConnectors)
    {
        _sourceConnectors = sourceConnectors.ToDictionary(connector => connector.SupportedConnectionType);
        _targetConnectors = targetConnectors.ToDictionary(connector => connector.SupportedConnectionType);
    }

    public bool CanResolveSource(ConnectionType connectionType) => _sourceConnectors.ContainsKey(connectionType);
    public bool CanResolveTarget(ConnectionType connectionType) => _targetConnectors.ContainsKey(connectionType);
    public ISourceConnector ResolveSource(ConnectionProfile connection) => ResolveSource(connection.Type);
    public ITargetConnector ResolveTarget(ConnectionProfile connection) => ResolveTarget(connection.Type);

    public ISourceConnector ResolveSource(ConnectionType connectionType)
    {
        if (_sourceConnectors.TryGetValue(connectionType, out var connector))
        {
            return connector;
        }

        throw new InvalidOperationException($"No source connector is registered for '{connectionType}'.");
    }

    public ITargetConnector ResolveTarget(ConnectionType connectionType)
    {
        if (_targetConnectors.TryGetValue(connectionType, out var connector))
        {
            return connector;
        }

        throw new InvalidOperationException($"No target connector is registered for '{connectionType}'.");
    }
}
