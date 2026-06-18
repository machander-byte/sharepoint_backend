using ZMS.Core.Enums;
using ZMS.Core.Models;

namespace ZMS.Core.Interfaces;

public interface ISourceConnector
{
    ConnectionType SupportedConnectionType { get; }

    Task<ConnectionTestResult> TestConnectionAsync(ConnectionProfile connection, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<SiteInfo>> GetSitesAsync(ConnectionProfile connection, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<LibraryInfo>> GetLibrariesAsync(
        ConnectionProfile connection,
        string sourceLocation,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<FileItem>> GetFilesAsync(
        ConnectionProfile connection,
        string sourceLocation,
        string? libraryName,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<FolderItem>> GetFoldersAsync(
        ConnectionProfile connection,
        string sourceLocation,
        string? libraryName,
        CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyCollection<FolderItem>>(Array.Empty<FolderItem>());

    Task<Stream> OpenReadAsync(
        ConnectionProfile connection,
        MigrationItem item,
        CancellationToken cancellationToken);
}
