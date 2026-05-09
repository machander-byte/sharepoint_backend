using ZMS.Connectors.GoogleDrive.Services;
using ZMS.Core.Enums;
using ZMS.Core.Interfaces;
using ZMS.Core.Models;

namespace ZMS.Connectors.GoogleDrive.Connectors;

public class GoogleDriveSourceConnector : ISourceConnector
{
    private const string GoogleDriveFileIdKey = "GoogleDriveFileId";
    private const string GoogleDriveMimeTypeKey = "GoogleDriveMimeType";
    private const string GoogleDriveExportMimeTypeKey = "GoogleDriveExportMimeType";
    private const string RelativePathKey = "RelativePath";
    private const string GoogleDriveFolderMimeType = "application/vnd.google-apps.folder";

    private readonly GoogleDriveApiClient _apiClient;

    public GoogleDriveSourceConnector(GoogleDriveApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public ConnectionType SupportedConnectionType => ConnectionType.GoogleDrive;

    public Task<ConnectionTestResult> TestConnectionAsync(ConnectionProfile connection, CancellationToken cancellationToken)
        => _apiClient.TestConnectionAsync(connection, cancellationToken);

    public async Task<IReadOnlyCollection<SiteInfo>> GetSitesAsync(
        ConnectionProfile connection,
        CancellationToken cancellationToken)
    {
        var rootFolderId = _apiClient.ResolveFolderId(connection, connection.RootPath ?? connection.Url);
        var rootFolder = await _apiClient.GetFolderAsync(connection, rootFolderId, cancellationToken);

        return
        [
            new SiteInfo
            {
                Id = rootFolder.Id,
                Name = rootFolder.Name,
                Url = string.IsNullOrWhiteSpace(connection.Url)
                    ? $"https://drive.google.com/drive/folders/{rootFolder.Id}"
                    : connection.Url
            }
        ];
    }

    public async Task<IReadOnlyCollection<LibraryInfo>> GetLibrariesAsync(
        ConnectionProfile connection,
        string sourceLocation,
        CancellationToken cancellationToken)
    {
        var folderId = _apiClient.ResolveFolderId(connection, sourceLocation);
        var children = await _apiClient.ListChildrenAsync(connection, folderId, cancellationToken);

        var libraries = children
            .Where(item => string.Equals(item.MimeType, GoogleDriveFolderMimeType, StringComparison.OrdinalIgnoreCase))
            .Select(item => new LibraryInfo
            {
                Id = item.Id,
                Name = item.Name,
                ItemCount = 0
            })
            .OrderBy(item => item.Name)
            .ToArray();

        if (libraries.Length > 0)
        {
            return libraries;
        }

        return
        [
            new LibraryInfo
            {
                Id = folderId,
                Name = "Root Folder",
                ItemCount = 0
            }
        ];
    }

    public async Task<IReadOnlyCollection<FileItem>> GetFilesAsync(
        ConnectionProfile connection,
        string sourceLocation,
        string? libraryName,
        CancellationToken cancellationToken)
    {
        var rootFolderId = _apiClient.ResolveFolderId(connection, sourceLocation);
        var files = new List<FileItem>();

        await WalkFolderAsync(connection, rootFolderId, string.Empty, files, cancellationToken);

        return files.OrderBy(file => file.SourcePath).ToArray();
    }

    public async Task<Stream> OpenReadAsync(
        ConnectionProfile connection,
        MigrationItem item,
        CancellationToken cancellationToken)
    {
        if (!item.Metadata.TryGetValue(GoogleDriveFileIdKey, out var fileId) || string.IsNullOrWhiteSpace(fileId))
        {
            throw new InvalidOperationException(
                $"The migration item '{item.FileName}' does not contain a Google Drive file identifier.");
        }

        item.Metadata.TryGetValue(GoogleDriveMimeTypeKey, out var mimeType);
        item.Metadata.TryGetValue(GoogleDriveExportMimeTypeKey, out var exportMimeType);

        return await _apiClient.OpenReadAsync(
            connection,
            fileId,
            mimeType ?? "application/octet-stream",
            exportMimeType,
            cancellationToken);
    }

    private async Task WalkFolderAsync(
        ConnectionProfile connection,
        string folderId,
        string relativeFolderPath,
        List<FileItem> files,
        CancellationToken cancellationToken)
    {
        var children = await _apiClient.ListChildrenAsync(connection, folderId, cancellationToken);

        foreach (var child in children)
        {
            if (string.Equals(child.MimeType, GoogleDriveFolderMimeType, StringComparison.OrdinalIgnoreCase))
            {
                var childFolderPath = CombinePath(relativeFolderPath, child.Name);
                await WalkFolderAsync(connection, child.Id, childFolderPath, files, cancellationToken);
                continue;
            }

            var download = await _apiClient.ResolveDownloadDescriptorAsync(child.Name, child.MimeType, cancellationToken);
            var relativePath = CombinePath(relativeFolderPath, download.FileName);

            files.Add(new FileItem
            {
                Name = download.FileName,
                SourcePath = relativePath,
                SizeInBytes = child.Size ?? 0,
                ModifiedUtc = child.ModifiedTime ?? DateTimeOffset.UtcNow,
                Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [RelativePathKey] = relativePath,
                    [GoogleDriveFileIdKey] = child.Id,
                    [GoogleDriveMimeTypeKey] = child.MimeType
                }
            });

            if (!string.IsNullOrWhiteSpace(download.ExportMimeType))
            {
                files[^1].Metadata[GoogleDriveExportMimeTypeKey] = download.ExportMimeType;
            }
        }
    }

    private static string CombinePath(string left, string right)
    {
        if (string.IsNullOrWhiteSpace(left))
        {
            return right.Trim().Replace('\\', '/');
        }

        return $"{left.TrimEnd('/')}/{right.TrimStart('/').Replace('\\', '/')}";
    }
}
