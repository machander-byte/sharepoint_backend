using ZMS.Core.Interfaces;
using ZMS.Core.Models;

namespace ZMS.Connectors.SharePointOnline.Services;

public class SharePointOnlineFileTransferService : IFileTransferService
{
    private readonly SharePointGraphClient _graphClient;

    public SharePointOnlineFileTransferService(SharePointGraphClient graphClient)
    {
        _graphClient = graphClient;
    }

    public async Task<string> TransferAsync(
        ConnectionProfile targetConnection,
        MigrationJob job,
        MigrationItem item,
        Stream content,
        CancellationToken cancellationToken)
    {
        var drive = await _graphClient.ResolveDriveAsync(
            targetConnection,
            job.TargetSiteUrl,
            job.TargetLibraryName,
            cancellationToken);

        var relativePath = ResolveRelativePath(item);
        relativePath = AddTargetRootPath(job.TargetRootPath, relativePath);
        var folderPath = ResolveFolderPath(relativePath);
        var fileName = Path.GetFileName(relativePath.Replace('/', Path.DirectorySeparatorChar));
        var parentItemId = await _graphClient.EnsureFolderPathAsync(
            targetConnection,
            drive.Id,
            folderPath,
            cancellationToken);

        var uploadedItem = await _graphClient.UploadAsync(
            targetConnection,
            drive.Id,
            parentItemId,
            fileName,
            content,
            item.FileSizeInBytes,
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(uploadedItem.WebUrl))
        {
            return uploadedItem.WebUrl;
        }

        return $"{drive.WebUrl.TrimEnd('/')}/{relativePath}";
    }

    private static string AddTargetRootPath(string? targetRootPath, string relativePath)
    {
        var normalizedRoot = targetRootPath?.Trim().Replace('\\', '/').Trim('/') ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedRoot))
        {
            return relativePath;
        }

        return $"{normalizedRoot}/{relativePath.TrimStart('/')}";
    }

    private static string ResolveRelativePath(MigrationItem item)
    {
        if (item.Metadata.TryGetValue("RelativePath", out var relativePath) && !string.IsNullOrWhiteSpace(relativePath))
        {
            return NormalizeRelativePath(relativePath, item.FileName);
        }

        return NormalizeRelativePath(item.FileName, item.FileName);
    }

    private static string NormalizeRelativePath(string candidatePath, string fallbackFileName)
    {
        var normalized = candidatePath.Trim().Replace('\\', '/').Trim('/');
        return string.IsNullOrWhiteSpace(normalized) ? fallbackFileName : normalized;
    }

    private static string? ResolveFolderPath(string relativePath)
    {
        var directory = Path.GetDirectoryName(relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (string.IsNullOrWhiteSpace(directory))
        {
            return null;
        }

        return directory.Replace('\\', '/');
    }
}
