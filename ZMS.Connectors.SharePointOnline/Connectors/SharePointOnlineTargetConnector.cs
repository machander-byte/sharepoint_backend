using Microsoft.Extensions.Logging;
using ZMS.Core.Enums;
using ZMS.Core.Interfaces;
using ZMS.Core.Models;
using ZMS.Connectors.SharePointOnline.Services;

namespace ZMS.Connectors.SharePointOnline.Connectors;

public class SharePointOnlineTargetConnector : ITargetConnector
{
    private readonly IFileTransferService _fileTransferService;
    private readonly SharePointGraphClient _graphClient;
    private readonly ILogger<SharePointOnlineTargetConnector> _logger;

    public SharePointOnlineTargetConnector(
        IFileTransferService fileTransferService,
        SharePointGraphClient graphClient,
        ILogger<SharePointOnlineTargetConnector> logger)
    {
        _fileTransferService = fileTransferService;
        _graphClient = graphClient;
        _logger = logger;
    }

    public ConnectionType SupportedConnectionType => ConnectionType.SharePointOnline;

    public Task<ConnectionTestResult> TestConnectionAsync(ConnectionProfile connection, CancellationToken cancellationToken)
        => _graphClient.TestConnectionAsync(connection, cancellationToken);

    public async Task<string> EnsureTargetSiteAsync(
        ConnectionProfile connection,
        string siteUrl,
        CancellationToken cancellationToken)
    {
        var site = await _graphClient.ResolveSiteAsync(connection, siteUrl, cancellationToken);
        _logger.LogInformation("Resolved target site '{SiteUrl}' as '{ResolvedSiteId}'.", site.WebUrl, site.Id);
        return site.WebUrl;
    }

    public async Task<string> EnsureTargetLibraryAsync(
        ConnectionProfile connection,
        string siteUrl,
        string libraryName,
        string? libraryUrlSegment,
        CancellationToken cancellationToken)
    {
        var drive = await _graphClient.EnsureDocumentLibraryAsync(
            connection,
            siteUrl,
            libraryName,
            libraryUrlSegment,
            cancellationToken);
        _logger.LogInformation(
            "Resolved target library '{LibraryName}' to drive '{DriveId}' under '{SiteUrl}'.",
            drive.Name,
            drive.Id,
            siteUrl);

        return drive.WebUrl;
    }

    public async Task<string> UploadFileAsync(
        ConnectionProfile connection,
        MigrationJob job,
        MigrationItem item,
        Stream content,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Uploading '{FileName}' to '{TargetLibraryName}'.", item.FileName, job.TargetLibraryName);
        return await _fileTransferService.TransferAsync(connection, job, item, content, cancellationToken);
    }
}
