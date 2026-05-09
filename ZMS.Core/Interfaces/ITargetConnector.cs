using ZMS.Core.Enums;
using ZMS.Core.Models;

namespace ZMS.Core.Interfaces;

public interface ITargetConnector
{
    ConnectionType SupportedConnectionType { get; }

    Task<ConnectionTestResult> TestConnectionAsync(ConnectionProfile connection, CancellationToken cancellationToken);

    Task<string> EnsureTargetSiteAsync(
        ConnectionProfile connection,
        string siteUrl,
        CancellationToken cancellationToken);

    Task<string> EnsureTargetLibraryAsync(
        ConnectionProfile connection,
        string siteUrl,
        string libraryName,
        string? libraryUrlSegment,
        CancellationToken cancellationToken);

    Task<string> UploadFileAsync(
        ConnectionProfile connection,
        MigrationJob job,
        MigrationItem item,
        Stream content,
        CancellationToken cancellationToken);
}
