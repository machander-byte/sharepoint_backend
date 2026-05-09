using ZMS.Connectors.SharePointOnline.Services;
using ZMS.Core.Enums;
using ZMS.Core.Interfaces;
using ZMS.Core.Models;

namespace ZMS.Connectors.SharePointOnline.Connectors;

public class SharePointOnlineSourceConnector : ISourceConnector
{
    private const string SharePointDriveIdKey = "SharePointDriveId";
    private const string SharePointDriveItemIdKey = "SharePointDriveItemId";
    private const string SharePointLibraryNameKey = "SharePointLibraryName";
    private const string RelativePathKey = "RelativePath";

    private readonly SharePointGraphClient _graphClient;

    public SharePointOnlineSourceConnector(SharePointGraphClient graphClient)
    {
        _graphClient = graphClient;
    }

    public ConnectionType SupportedConnectionType => ConnectionType.SharePointOnline;

    public Task<ConnectionTestResult> TestConnectionAsync(ConnectionProfile connection, CancellationToken cancellationToken)
        => _graphClient.TestConnectionAsync(connection, cancellationToken);

    public async Task<IReadOnlyCollection<SiteInfo>> GetSitesAsync(
        ConnectionProfile connection,
        CancellationToken cancellationToken)
    {
        var site = await _graphClient.ResolveSiteAsync(connection, connection.Url, cancellationToken);
        return
        [
            new SiteInfo
            {
                Id = site.Id,
                Name = site.Name,
                Url = site.WebUrl
            }
        ];
    }

    public async Task<IReadOnlyCollection<LibraryInfo>> GetLibrariesAsync(
        ConnectionProfile connection,
        string sourceLocation,
        CancellationToken cancellationToken)
    {
        var libraries = await _graphClient.ListDocumentLibrariesAsync(
            connection,
            ResolveSiteUrl(connection, sourceLocation),
            cancellationToken);

        return libraries
            .Select(library => new LibraryInfo
            {
                Id = library.Id,
                Name = library.Name,
                ItemCount = 0
            })
            .ToArray();
    }

    public async Task<IReadOnlyCollection<FileItem>> GetFilesAsync(
        ConnectionProfile connection,
        string sourceLocation,
        string? libraryName,
        CancellationToken cancellationToken)
    {
        var siteUrl = ResolveSiteUrl(connection, sourceLocation);
        var libraryNames = await ResolveLibraryNamesAsync(connection, siteUrl, libraryName, cancellationToken);
        var files = new List<FileItem>();

        foreach (var resolvedLibraryName in libraryNames)
        {
            var libraryFiles = await _graphClient.ListDriveFilesAsync(
                connection,
                siteUrl,
                resolvedLibraryName,
                null,
                cancellationToken);

            foreach (var file in libraryFiles)
            {
                var relativePath = libraryNames.Count > 1
                    ? CombinePath(file.LibraryName, file.RelativePath)
                    : file.RelativePath;

                files.Add(new FileItem
                {
                    Name = file.Name,
                    SourcePath = file.WebUrl ?? CombinePath(siteUrl.TrimEnd('/'), relativePath),
                    SizeInBytes = file.SizeInBytes,
                    ModifiedUtc = file.ModifiedUtc,
                    Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        [RelativePathKey] = relativePath,
                        [SharePointDriveIdKey] = file.DriveId,
                        [SharePointDriveItemIdKey] = file.DriveItemId,
                        [SharePointLibraryNameKey] = file.LibraryName
                    }
                });
            }
        }

        return files.OrderBy(file => file.SourcePath).ToArray();
    }

    public async Task<Stream> OpenReadAsync(
        ConnectionProfile connection,
        MigrationItem item,
        CancellationToken cancellationToken)
    {
        if (!item.Metadata.TryGetValue(SharePointDriveIdKey, out var driveId)
            || string.IsNullOrWhiteSpace(driveId)
            || !item.Metadata.TryGetValue(SharePointDriveItemIdKey, out var driveItemId)
            || string.IsNullOrWhiteSpace(driveItemId))
        {
            throw new InvalidOperationException(
                $"The migration item '{item.FileName}' does not contain SharePoint drive identifiers.");
        }

        return await _graphClient.OpenDriveItemReadAsync(connection, driveId, driveItemId, cancellationToken);
    }

    private static string ResolveSiteUrl(ConnectionProfile connection, string sourceLocation)
        => string.IsNullOrWhiteSpace(sourceLocation) ? connection.Url : sourceLocation.Trim();

    private async Task<IReadOnlyCollection<string>> ResolveLibraryNamesAsync(
        ConnectionProfile connection,
        string siteUrl,
        string? libraryName,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(libraryName) && libraryName.Trim() != "*")
        {
            return [libraryName.Trim()];
        }

        var libraries = await _graphClient.ListDocumentLibrariesAsync(connection, siteUrl, cancellationToken);
        return libraries.Select(library => library.Name).ToArray();
    }

    private static string CombinePath(string left, string right)
    {
        if (string.IsNullOrWhiteSpace(left))
        {
            return right.Trim().Replace('\\', '/').Trim('/');
        }

        return $"{left.TrimEnd('/')}/{right.TrimStart('/').Replace('\\', '/')}";
    }
}
