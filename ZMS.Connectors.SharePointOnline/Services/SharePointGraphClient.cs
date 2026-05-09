using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZMS.Core.Models;
using ZMS.Core.Options;

namespace ZMS.Connectors.SharePointOnline.Services;

public class SharePointGraphClient
{
    private const long GraphSmallFileUploadLimitInBytes = 250L * 1024 * 1024;
    private const long DefaultLargeFileUploadThresholdInBytes = 10L * 1024 * 1024;
    private const int DefaultUploadChunkSizeInBytes = 20 * 320 * 1024;
    private const int MaxUploadChunkRetries = 3;
    private const string SharePointDocumentLibraryNameKey = "DocumentLibraryName";
    private static readonly string[] RequiredGraphRoles = ["Sites.ReadWrite.All", "Files.ReadWrite.All"];
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SharePointGraphClient> _logger;
    private readonly MigrationEngineOptions _options;
    private readonly ConcurrentDictionary<Guid, CachedToken> _tokenCache = new();

    public SharePointGraphClient(
        IHttpClientFactory httpClientFactory,
        ILogger<SharePointGraphClient> logger,
        IOptions<MigrationEngineOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<ConnectionTestResult> TestConnectionAsync(
        ConnectionProfile connection,
        CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(connection.Url, UriKind.Absolute, out _))
        {
            return new ConnectionTestResult
            {
                IsSuccess = false,
                Message = "Provide a valid SharePoint Online site URL."
            };
        }

        if (string.IsNullOrWhiteSpace(connection.TenantId)
            || string.IsNullOrWhiteSpace(connection.ClientId)
            || string.IsNullOrWhiteSpace(connection.ClientSecret))
        {
            return new ConnectionTestResult
            {
                IsSuccess = false,
                Message = "SharePoint Online requires tenant ID, client ID, and client secret for direct transfers."
            };
        }

        if (!connection.AdditionalSettings.TryGetValue(SharePointDocumentLibraryNameKey, out var documentLibraryName)
            || string.IsNullOrWhiteSpace(documentLibraryName))
        {
            return new ConnectionTestResult
            {
                IsSuccess = false,
                Message = "SharePoint Online requires a target document library name."
            };
        }

        try
        {
            var site = await ResolveSiteAsync(connection, connection.Url, cancellationToken);
            var drive = await ResolveDriveAsync(connection, site.WebUrl, documentLibraryName, cancellationToken);
            return new ConnectionTestResult
            {
                IsSuccess = true,
                Message = $"SharePoint Online connection verified for '{site.WebUrl}' library '{drive.Name}'."
            };
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "SharePoint Online connection test failed for '{ConnectionName}'.", connection.Name);
            return new ConnectionTestResult
            {
                IsSuccess = false,
                Message = exception.Message
            };
        }
    }

    public async Task<SharePointSiteDescriptor> ResolveSiteAsync(
        ConnectionProfile connection,
        string siteUrl,
        CancellationToken cancellationToken)
    {
        var effectiveSiteUrl = string.IsNullOrWhiteSpace(siteUrl) ? connection.Url : siteUrl;
        if (!Uri.TryCreate(effectiveSiteUrl, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException("A valid SharePoint site URL is required.");
        }

        var relativePath = string.Join(
            '/',
            uri.AbsolutePath
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(Uri.EscapeDataString));

        var requestUri = string.IsNullOrWhiteSpace(relativePath)
            ? $"https://graph.microsoft.com/v1.0/sites/{uri.Host}:/"
            : $"https://graph.microsoft.com/v1.0/sites/{uri.Host}:/{relativePath}";

        using var response = await SendGraphAsync(connection, HttpMethod.Get, requestUri, null, cancellationToken);
        var site = await ReadRequiredAsync<GraphSiteResponse>(response, cancellationToken);

        return new SharePointSiteDescriptor(
            site.Id,
            string.IsNullOrWhiteSpace(site.WebUrl) ? effectiveSiteUrl : site.WebUrl,
            site.DisplayName ?? uri.Host);
    }

    public async Task<SharePointDriveDescriptor> ResolveDriveAsync(
        ConnectionProfile connection,
        string siteUrl,
        string libraryName,
        CancellationToken cancellationToken)
    {
        var drive = await TryResolveDriveAsync(connection, siteUrl, libraryName, cancellationToken);
        if (drive is not null)
        {
            return drive;
        }

        var site = await ResolveSiteAsync(connection, siteUrl, cancellationToken);
        throw new InvalidOperationException(
            $"The SharePoint document library '{ResolveLibraryName(libraryName)}' was not found under '{site.WebUrl}'.");
    }

    public async Task<SharePointDriveDescriptor> EnsureDocumentLibraryAsync(
        ConnectionProfile connection,
        string siteUrl,
        string libraryName,
        string? libraryUrlSegment,
        CancellationToken cancellationToken)
    {
        var existingDrive = await TryResolveDriveAsync(connection, siteUrl, libraryName, cancellationToken);
        if (existingDrive is not null)
        {
            return existingDrive;
        }

        var site = await ResolveSiteAsync(connection, siteUrl, cancellationToken);
        var requestedLibraryName = ResolveLibraryName(libraryName);
        var requestedUrlSegment = NormalizeLibraryUrlSegment(libraryUrlSegment);
        var creationDisplayName = requestedUrlSegment ?? requestedLibraryName;

        using var createResponse = await SendGraphAsync(
            connection,
            HttpMethod.Post,
            $"https://graph.microsoft.com/v1.0/sites/{site.Id}/lists",
            JsonContent.Create(new Dictionary<string, object?>
            {
                ["displayName"] = creationDisplayName,
                ["list"] = new Dictionary<string, object?>
                {
                    ["template"] = "documentLibrary"
                }
            }),
            cancellationToken);

        var createdList = await ReadRequiredAsync<GraphListResponse>(createResponse, cancellationToken);
        if (!string.Equals(creationDisplayName, requestedLibraryName, StringComparison.OrdinalIgnoreCase))
        {
            using var renameResponse = await SendGraphAsync(
                connection,
                HttpMethod.Patch,
                $"https://graph.microsoft.com/v1.0/sites/{site.Id}/lists/{createdList.Id}",
                JsonContent.Create(new Dictionary<string, object?>
                {
                    ["displayName"] = requestedLibraryName
                }),
                cancellationToken);

            renameResponse.EnsureSuccessStatusCode();
        }

        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        try
        {
            return await ResolveDriveAsync(connection, siteUrl, requestedLibraryName, cancellationToken);
        }
        catch when (!string.IsNullOrWhiteSpace(requestedUrlSegment))
        {
            return await ResolveDriveAsync(connection, siteUrl, requestedUrlSegment, cancellationToken);
        }
    }

    public async Task<IReadOnlyCollection<SharePointDriveDescriptor>> ListDocumentLibrariesAsync(
        ConnectionProfile connection,
        string siteUrl,
        CancellationToken cancellationToken)
    {
        var site = await ResolveSiteAsync(connection, siteUrl, cancellationToken);
        var drives = await ListDrivesAsync(connection, site, cancellationToken);

        return drives
            .Select(drive => ToDriveDescriptor(site, drive, drive.Name ?? "Documents"))
            .OrderBy(drive => drive.Name)
            .ToArray();
    }

    public async Task<IReadOnlyCollection<SharePointFileDescriptor>> ListDriveFilesAsync(
        ConnectionProfile connection,
        string siteUrl,
        string libraryName,
        string? relativeRootPath,
        CancellationToken cancellationToken)
    {
        var drive = await ResolveDriveAsync(connection, siteUrl, libraryName, cancellationToken);
        var files = new List<SharePointFileDescriptor>();
        var rootItem = await ResolveDriveRootItemAsync(connection, drive.Id, relativeRootPath, cancellationToken);

        await WalkDriveFolderAsync(
            connection,
            drive,
            rootItem.Id,
            string.Empty,
            files,
            cancellationToken);

        return files.OrderBy(file => file.RelativePath).ToArray();
    }

    public async Task<Stream> OpenDriveItemReadAsync(
        ConnectionProfile connection,
        string driveId,
        string driveItemId,
        CancellationToken cancellationToken)
    {
        var response = await SendGraphAsync(
            connection,
            HttpMethod.Get,
            $"https://graph.microsoft.com/v1.0/drives/{driveId}/items/{driveItemId}/content",
            null,
            cancellationToken);
        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        return new ResponseBackedStream(response, stream);
    }

    public async Task<string> EnsureFolderPathAsync(
        ConnectionProfile connection,
        string driveId,
        string? relativeFolderPath,
        CancellationToken cancellationToken)
    {
        var token = await GetAccessTokenAsync(connection, cancellationToken);
        var parentItemId = await GetRootItemIdAsync(token, driveId, cancellationToken);

        if (string.IsNullOrWhiteSpace(relativeFolderPath))
        {
            return parentItemId;
        }

        foreach (var segment in relativeFolderPath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            parentItemId = await GetOrCreateFolderAsync(token, driveId, parentItemId, segment, cancellationToken);
        }

        return parentItemId;
    }

    public async Task<SharePointDriveItemDescriptor> UploadAsync(
        ConnectionProfile connection,
        string driveId,
        string parentItemId,
        string fileName,
        Stream content,
        long fileSizeInBytes,
        CancellationToken cancellationToken)
    {
        var knownSize = ResolveContentLength(content, fileSizeInBytes);
        if (knownSize is null)
        {
            return await UploadUnknownSizeFileAsync(
                connection,
                driveId,
                parentItemId,
                fileName,
                content,
                cancellationToken);
        }

        return await UploadKnownSizeFileAsync(
            connection,
            driveId,
            parentItemId,
            fileName,
            content,
            knownSize.Value,
            cancellationToken);
    }

    private async Task<SharePointDriveDescriptor?> TryResolveDriveAsync(
        ConnectionProfile connection,
        string siteUrl,
        string libraryName,
        CancellationToken cancellationToken)
    {
        var site = await ResolveSiteAsync(connection, siteUrl, cancellationToken);
        var drives = await ListDrivesAsync(connection, site, cancellationToken);
        var requestedLibraryName = ResolveLibraryName(libraryName);

        var drive = drives.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, requestedLibraryName, StringComparison.OrdinalIgnoreCase));

        if (drive is null)
        {
            var defaultNames = new[] { "Documents", "Shared Documents" };
            if (defaultNames.Contains(requestedLibraryName, StringComparer.OrdinalIgnoreCase))
            {
                drive = drives.FirstOrDefault(candidate =>
                    defaultNames.Contains(candidate.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase));
            }
        }

        return drive is null ? null : ToDriveDescriptor(site, drive, requestedLibraryName);
    }

    private async Task<IReadOnlyCollection<GraphDriveResponse>> ListDrivesAsync(
        ConnectionProfile connection,
        SharePointSiteDescriptor site,
        CancellationToken cancellationToken)
    {
        using var response = await SendGraphAsync(
            connection,
            HttpMethod.Get,
            $"https://graph.microsoft.com/v1.0/sites/{site.Id}/drives?$select=id,name,webUrl,driveType",
            null,
            cancellationToken);

        var drives = await ReadRequiredAsync<GraphDriveCollectionResponse>(response, cancellationToken);
        return drives.Value
            .Where(drive => string.IsNullOrWhiteSpace(drive.DriveType)
                || string.Equals(drive.DriveType, "documentLibrary", StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    private static SharePointDriveDescriptor ToDriveDescriptor(
        SharePointSiteDescriptor site,
        GraphDriveResponse drive,
        string fallbackLibraryName)
    {
        var libraryName = string.IsNullOrWhiteSpace(drive.Name) ? fallbackLibraryName : drive.Name;
        return new SharePointDriveDescriptor(
            drive.Id,
            libraryName,
            string.IsNullOrWhiteSpace(drive.WebUrl)
                ? $"{site.WebUrl.TrimEnd('/')}/{Uri.EscapeDataString(libraryName)}"
                : drive.WebUrl);
    }

    private async Task<GraphDriveItemResponse> ResolveDriveRootItemAsync(
        ConnectionProfile connection,
        string driveId,
        string? relativeRootPath,
        CancellationToken cancellationToken)
    {
        var normalizedPath = NormalizeRelativePath(relativeRootPath);
        var requestUri = string.IsNullOrWhiteSpace(normalizedPath)
            ? $"https://graph.microsoft.com/v1.0/drives/{driveId}/root?$select=id,name,webUrl"
            : $"https://graph.microsoft.com/v1.0/drives/{driveId}/root:/{Uri.EscapeDataString(normalizedPath).Replace("%2F", "/")}?$select=id,name,webUrl";

        using var response = await SendGraphAsync(connection, HttpMethod.Get, requestUri, null, cancellationToken);
        return await ReadRequiredAsync<GraphDriveItemResponse>(response, cancellationToken);
    }

    private async Task WalkDriveFolderAsync(
        ConnectionProfile connection,
        SharePointDriveDescriptor drive,
        string parentItemId,
        string relativeFolderPath,
        List<SharePointFileDescriptor> files,
        CancellationToken cancellationToken)
    {
        string? requestUri =
            $"https://graph.microsoft.com/v1.0/drives/{drive.Id}/items/{parentItemId}/children?$select=id,name,size,lastModifiedDateTime,webUrl,file,folder";

        while (!string.IsNullOrWhiteSpace(requestUri))
        {
            using var response = await SendGraphAsync(connection, HttpMethod.Get, requestUri, null, cancellationToken);
            var children = await ReadRequiredAsync<GraphDriveItemCollectionResponse>(response, cancellationToken);

            foreach (var child in children.Value)
            {
                if (child.Folder is not null)
                {
                    await WalkDriveFolderAsync(
                        connection,
                        drive,
                        child.Id,
                        CombinePath(relativeFolderPath, child.Name ?? child.Id),
                        files,
                        cancellationToken);
                    continue;
                }

                if (child.File is null)
                {
                    continue;
                }

                files.Add(new SharePointFileDescriptor(
                    drive.Id,
                    drive.Name,
                    child.Id,
                    child.Name ?? child.Id,
                    CombinePath(relativeFolderPath, child.Name ?? child.Id),
                    child.Size ?? 0,
                    child.LastModifiedDateTime ?? DateTimeOffset.UtcNow,
                    child.WebUrl));
            }

            requestUri = children.NextLink;
        }
    }

    private async Task<SharePointDriveItemDescriptor> UploadUnknownSizeFileAsync(
        ConnectionProfile connection,
        string driveId,
        string parentItemId,
        string fileName,
        Stream content,
        CancellationToken cancellationToken)
    {
        var temporaryPath = Path.Combine(Path.GetTempPath(), $"zms-upload-{Guid.NewGuid():N}.tmp");
        await using var temporaryFile = new FileStream(
            temporaryPath,
            FileMode.CreateNew,
            FileAccess.ReadWrite,
            FileShare.None,
            bufferSize: 1024 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan | FileOptions.DeleteOnClose);

        await content.CopyToAsync(temporaryFile, cancellationToken);
        temporaryFile.Position = 0;

        _logger.LogInformation(
            "Buffered unknown-size SharePoint upload '{FileName}' to {FileSizeInBytes} bytes before transfer.",
            fileName,
            temporaryFile.Length);

        return await UploadKnownSizeFileAsync(
            connection,
            driveId,
            parentItemId,
            fileName,
            temporaryFile,
            temporaryFile.Length,
            cancellationToken);
    }

    private Task<SharePointDriveItemDescriptor> UploadKnownSizeFileAsync(
        ConnectionProfile connection,
        string driveId,
        string parentItemId,
        string fileName,
        Stream content,
        long fileSizeInBytes,
        CancellationToken cancellationToken)
    {
        var uploadSessionThreshold = ResolveUploadSessionThreshold();
        return fileSizeInBytes > uploadSessionThreshold
            ? UploadLargeFileAsync(connection, driveId, parentItemId, fileName, content, fileSizeInBytes, cancellationToken)
            : UploadSmallFileAsync(connection, driveId, parentItemId, fileName, content, cancellationToken);
    }

    private async Task<SharePointDriveItemDescriptor> UploadSmallFileAsync(
        ConnectionProfile connection,
        string driveId,
        string parentItemId,
        string fileName,
        Stream content,
        CancellationToken cancellationToken)
    {
        var requestUri =
            $"https://graph.microsoft.com/v1.0/drives/{driveId}/items/{parentItemId}:/{Uri.EscapeDataString(fileName)}:/content";

        using var contentBody = new StreamContent(content);
        contentBody.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        using var response = await SendGraphAsync(connection, HttpMethod.Put, requestUri, contentBody, cancellationToken);
        var uploadedItem = await ReadRequiredAsync<GraphDriveItemResponse>(response, cancellationToken);

        return new SharePointDriveItemDescriptor(uploadedItem.Id, uploadedItem.Name ?? fileName, uploadedItem.WebUrl);
    }

    private async Task<SharePointDriveItemDescriptor> UploadLargeFileAsync(
        ConnectionProfile connection,
        string driveId,
        string parentItemId,
        string fileName,
        Stream content,
        long fileSizeInBytes,
        CancellationToken cancellationToken)
    {
        if (fileSizeInBytes <= 0)
        {
            throw new InvalidOperationException(
                $"Large-file upload for '{fileName}' requires a known source size.");
        }

        var sessionRequestUri =
            $"https://graph.microsoft.com/v1.0/drives/{driveId}/items/{parentItemId}:/{Uri.EscapeDataString(fileName)}:/createUploadSession";
        var requestBody = JsonContent.Create(new Dictionary<string, object?>
        {
            ["item"] = new Dictionary<string, object?>
            {
                ["@microsoft.graph.conflictBehavior"] = "replace"
            }
        });

        using var sessionResponse = await SendGraphAsync(connection, HttpMethod.Post, sessionRequestUri, requestBody, cancellationToken);
        var session = await ReadRequiredAsync<GraphUploadSessionResponse>(sessionResponse, cancellationToken);

        var client = _httpClientFactory.CreateClient();
        var buffer = new byte[ResolveUploadChunkSize()];
        long position = 0;

        while (position < fileSizeInBytes)
        {
            var bytesToRead = (int)Math.Min(buffer.Length, fileSizeInBytes - position);
            var bytesRead = await ReadAtMostAsync(content, buffer, bytesToRead, cancellationToken);
            if (bytesRead == 0)
            {
                throw new InvalidOperationException(
                    $"The source stream for '{fileName}' ended before the expected {fileSizeInBytes} bytes were uploaded.");
            }

            using var chunkResponse = await UploadChunkWithRetriesAsync(
                client,
                session.UploadUrl,
                buffer,
                bytesRead,
                position,
                fileSizeInBytes,
                fileName,
                cancellationToken);

            if (chunkResponse.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created)
            {
                var uploadedItem = await ReadRequiredAsync<GraphDriveItemResponse>(chunkResponse, cancellationToken);
                return new SharePointDriveItemDescriptor(uploadedItem.Id, uploadedItem.Name ?? fileName, uploadedItem.WebUrl);
            }

            if (chunkResponse.StatusCode != HttpStatusCode.Accepted)
            {
                var error = await chunkResponse.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException(
                    $"SharePoint large-file upload failed for '{fileName}' with status {(int)chunkResponse.StatusCode}: {error}");
            }

            position += bytesRead;
        }

        throw new InvalidOperationException($"SharePoint large-file upload did not complete for '{fileName}'.");
    }

    private async Task<HttpResponseMessage> UploadChunkWithRetriesAsync(
        HttpClient client,
        string uploadUrl,
        byte[] buffer,
        int bytesRead,
        long position,
        long fileSizeInBytes,
        string fileName,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxUploadChunkRetries; attempt++)
        {
            var chunkResponse = await UploadChunkAsync(
                client,
                uploadUrl,
                buffer,
                bytesRead,
                position,
                fileSizeInBytes,
                cancellationToken);

            if (!IsTransientUploadFailure(chunkResponse.StatusCode) || attempt == MaxUploadChunkRetries)
            {
                return chunkResponse;
            }

            var retryDelay = ResolveRetryDelay(chunkResponse, attempt);
            _logger.LogWarning(
                "Transient SharePoint upload response {StatusCode} for '{FileName}' at byte {Position}. Retrying in {DelayMilliseconds} ms.",
                (int)chunkResponse.StatusCode,
                fileName,
                position,
                retryDelay.TotalMilliseconds);

            chunkResponse.Dispose();
            await Task.Delay(retryDelay, cancellationToken);
        }

        throw new InvalidOperationException($"SharePoint upload retry loop exited unexpectedly for '{fileName}'.");
    }

    private static async Task<HttpResponseMessage> UploadChunkAsync(
        HttpClient client,
        string uploadUrl,
        byte[] buffer,
        int bytesRead,
        long position,
        long fileSizeInBytes,
        CancellationToken cancellationToken)
    {
        using var chunkRequest = new HttpRequestMessage(HttpMethod.Put, uploadUrl)
        {
            Content = new ByteArrayContent(buffer, 0, bytesRead)
        };

        chunkRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        chunkRequest.Content.Headers.ContentRange = new ContentRangeHeaderValue(
            position,
            position + bytesRead - 1,
            fileSizeInBytes);

        return await client.SendAsync(chunkRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    }

    private async Task<string> GetRootItemIdAsync(
        string accessToken,
        string driveId,
        CancellationToken cancellationToken)
    {
        using var response = await SendAuthorizedAsync(
            accessToken,
            HttpMethod.Get,
            $"https://graph.microsoft.com/v1.0/drives/{driveId}/root?$select=id",
            null,
            cancellationToken);

        var root = await ReadRequiredAsync<GraphDriveItemResponse>(response, cancellationToken);
        return root.Id;
    }

    private async Task<string> GetOrCreateFolderAsync(
        string accessToken,
        string driveId,
        string parentItemId,
        string folderName,
        CancellationToken cancellationToken)
    {
        var existingFolder = await TryGetItemAsync(accessToken, driveId, parentItemId, folderName, cancellationToken);
        if (existingFolder is not null)
        {
            return existingFolder.Id;
        }

        var requestBody = JsonContent.Create(new Dictionary<string, object?>
        {
            ["name"] = folderName,
            ["folder"] = new Dictionary<string, object?>(),
            ["@microsoft.graph.conflictBehavior"] = "fail"
        });

        using var response = await SendAuthorizedAsync(
            accessToken,
            HttpMethod.Post,
            $"https://graph.microsoft.com/v1.0/drives/{driveId}/items/{parentItemId}/children",
            requestBody,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            var currentFolder = await TryGetItemAsync(accessToken, driveId, parentItemId, folderName, cancellationToken);
            if (currentFolder is not null)
            {
                return currentFolder.Id;
            }
        }

        var createdFolder = await ReadRequiredAsync<GraphDriveItemResponse>(response, cancellationToken);
        return createdFolder.Id;
    }

    private async Task<GraphDriveItemResponse?> TryGetItemAsync(
        string accessToken,
        string driveId,
        string parentItemId,
        string itemName,
        CancellationToken cancellationToken)
    {
        var requestUri =
            $"https://graph.microsoft.com/v1.0/drives/{driveId}/items/{parentItemId}:/{Uri.EscapeDataString(itemName)}?$select=id,name,webUrl";

        using var response = await SendAuthorizedAsync(accessToken, HttpMethod.Get, requestUri, null, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"SharePoint path lookup failed for '{itemName}' with status {(int)response.StatusCode}: {error}");
        }

        return await response.Content.ReadFromJsonAsync<GraphDriveItemResponse>(JsonOptions, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendGraphAsync(
        ConnectionProfile connection,
        HttpMethod method,
        string requestUri,
        HttpContent? content,
        CancellationToken cancellationToken)
    {
        var accessToken = await GetAccessTokenAsync(connection, cancellationToken);
        return await SendAuthorizedAsync(accessToken, method, requestUri, content, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendAuthorizedAsync(
        string accessToken,
        HttpMethod method,
        string requestUri,
        HttpContent? content,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(method, requestUri) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.Conflict)
        {
            return response;
        }

        var error = await response.Content.ReadAsStringAsync(cancellationToken);
        response.Dispose();
        throw new InvalidOperationException(
            BuildGraphFailureMessage(response.StatusCode, error));
    }

    private async Task<string> GetAccessTokenAsync(
        ConnectionProfile connection,
        CancellationToken cancellationToken)
    {
        if (_tokenCache.TryGetValue(connection.Id, out var cachedToken)
            && cachedToken.ExpiresUtc > DateTimeOffset.UtcNow.AddMinutes(2))
        {
            return cachedToken.AccessToken;
        }

        if (string.IsNullOrWhiteSpace(connection.TenantId)
            || string.IsNullOrWhiteSpace(connection.ClientId)
            || string.IsNullOrWhiteSpace(connection.ClientSecret))
        {
            throw new InvalidOperationException(
                "SharePoint Online direct transfer requires tenant ID, client ID, and client secret.");
        }

        var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = connection.ClientId,
            ["scope"] = "https://graph.microsoft.com/.default",
            ["client_secret"] = connection.ClientSecret,
            ["grant_type"] = "client_credentials"
        });

        var client = _httpClientFactory.CreateClient();
        using var response = await client.PostAsync(
            $"https://login.microsoftonline.com/{connection.TenantId}/oauth2/v2.0/token",
            tokenRequest,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Failed to acquire a Microsoft Graph access token: {error}");
        }

        var token = await ReadRequiredAsync<TokenResponse>(response, cancellationToken);
        EnsureTokenHasRequiredRoles(token.AccessToken);

        var expiresUtc = DateTimeOffset.UtcNow.AddSeconds(Math.Max(60, token.ExpiresIn - 120));
        _tokenCache[connection.Id] = new CachedToken(token.AccessToken, expiresUtc);
        return token.AccessToken;
    }

    private static async Task<T> ReadRequiredAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var value = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
        if (value is null)
        {
            throw new InvalidOperationException("The remote service returned an empty response body.");
        }

        return value;
    }

    private static async Task<int> ReadAtMostAsync(
        Stream source,
        byte[] buffer,
        int bytesToRead,
        CancellationToken cancellationToken)
    {
        var totalBytesRead = 0;

        while (totalBytesRead < bytesToRead)
        {
            var bytesRead = await source.ReadAsync(
                buffer.AsMemory(totalBytesRead, bytesToRead - totalBytesRead),
                cancellationToken);

            if (bytesRead == 0)
            {
                break;
            }

            totalBytesRead += bytesRead;
        }

        return totalBytesRead;
    }

    private long? ResolveContentLength(Stream content, long declaredSizeInBytes)
    {
        if (declaredSizeInBytes > 0)
        {
            return declaredSizeInBytes;
        }

        if (!content.CanSeek)
        {
            return null;
        }

        try
        {
            return Math.Max(0, content.Length - content.Position);
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private long ResolveUploadSessionThreshold()
    {
        if (_options.LargeFileUploadThresholdBytes <= 0)
        {
            return DefaultLargeFileUploadThresholdInBytes;
        }

        return Math.Min(_options.LargeFileUploadThresholdBytes, GraphSmallFileUploadLimitInBytes);
    }

    private int ResolveUploadChunkSize()
    {
        var configuredChunkSize = _options.UploadChunkSizeBytes <= 0
            ? DefaultUploadChunkSizeInBytes
            : _options.UploadChunkSizeBytes;

        var chunkSize = Math.Max(320 * 1024, configuredChunkSize);
        var remainder = chunkSize % (320 * 1024);
        return remainder == 0 ? chunkSize : chunkSize + (320 * 1024 - remainder);
    }

    private static bool IsTransientUploadFailure(HttpStatusCode statusCode)
    {
        return statusCode is HttpStatusCode.TooManyRequests
            or HttpStatusCode.RequestTimeout
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout
            || (int)statusCode >= 500;
    }

    private static TimeSpan ResolveRetryDelay(HttpResponseMessage response, int attempt)
    {
        if (response.Headers.RetryAfter?.Delta is { } delta)
        {
            return delta;
        }

        if (response.Headers.RetryAfter?.Date is { } date)
        {
            var delay = date - DateTimeOffset.UtcNow;
            if (delay > TimeSpan.Zero)
            {
                return delay;
            }
        }

        return TimeSpan.FromSeconds(Math.Min(30, Math.Pow(2, attempt)));
    }

    private static void EnsureTokenHasRequiredRoles(string accessToken)
    {
        var roles = ExtractJwtRoles(accessToken);
        if (roles.Count == 0)
        {
            throw new InvalidOperationException(
                "Microsoft Graph token does not contain application roles. Use application permissions, grant admin consent, and confirm the token contains a roles claim.");
        }

        var missingRoles = RequiredGraphRoles
            .Where(role => !roles.Contains(role))
            .ToArray();

        if (missingRoles.Length == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Microsoft Graph token is missing required application permission(s): {string.Join(", ", missingRoles)}. Grant admin consent for Sites.ReadWrite.All and Files.ReadWrite.All, then recreate or retest the SharePoint connection.");
    }

    private static HashSet<string> ExtractJwtRoles(string accessToken)
    {
        var roles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var tokenParts = accessToken.Split('.');
        if (tokenParts.Length < 2)
        {
            return roles;
        }

        try
        {
            var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(tokenParts[1]));
            using var payload = JsonDocument.Parse(payloadJson);
            if (!payload.RootElement.TryGetProperty("roles", out var rolesElement)
                || rolesElement.ValueKind != JsonValueKind.Array)
            {
                return roles;
            }

            foreach (var roleElement in rolesElement.EnumerateArray())
            {
                if (roleElement.ValueKind == JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(roleElement.GetString()))
                {
                    roles.Add(roleElement.GetString()!);
                }
            }
        }
        catch (JsonException)
        {
            return roles;
        }
        catch (FormatException)
        {
            return roles;
        }

        return roles;
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
        return Convert.FromBase64String(padded);
    }

    private static string BuildGraphFailureMessage(HttpStatusCode statusCode, string error)
    {
        var statusMessage = $"SharePoint Graph request failed with status {(int)statusCode}.";

        if (statusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return $"{statusMessage} Check Azure app registration admin consent and application permissions. Required permissions: Sites.ReadWrite.All and Files.ReadWrite.All. Graph response: {TrimError(error)}";
        }

        if (statusCode == HttpStatusCode.NotFound)
        {
            return $"{statusMessage} Confirm the SharePoint site URL, document library name, and target folder path are correct. Graph response: {TrimError(error)}";
        }

        return $"{statusMessage} Graph response: {TrimError(error)}";
    }

    private static string TrimError(string error)
    {
        var trimmed = string.IsNullOrWhiteSpace(error) ? "No response body." : error.Trim();
        return trimmed.Length <= 1200 ? trimmed : $"{trimmed[..1200]}...";
    }

    private static string ResolveLibraryName(string? libraryName)
        => string.IsNullOrWhiteSpace(libraryName) ? "Documents" : libraryName.Trim();

    private static string? NormalizeLibraryUrlSegment(string? value)
    {
        var normalized = NormalizeRelativePath(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return normalized
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .LastOrDefault();
    }

    private static string? NormalizeRelativePath(string? value)
    {
        var normalized = value?.Trim().Replace('\\', '/').Trim('/') ?? string.Empty;
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string CombinePath(string left, string right)
    {
        var normalizedRight = right.Trim().Replace('\\', '/').Trim('/');
        return string.IsNullOrWhiteSpace(left)
            ? normalizedRight
            : $"{left.TrimEnd('/')}/{normalizedRight}";
    }

    private sealed record CachedToken(string AccessToken, DateTimeOffset ExpiresUtc);

    private sealed class ResponseBackedStream : Stream
    {
        private readonly HttpResponseMessage _response;
        private readonly Stream _inner;

        public ResponseBackedStream(HttpResponseMessage response, Stream inner)
        {
            _response = response;
            _inner = inner;
        }

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => _inner.CanSeek;
        public override bool CanWrite => _inner.CanWrite;
        public override long Length => _inner.Length;

        public override long Position
        {
            get => _inner.Position;
            set => _inner.Position = value;
        }

        public override void Flush() => _inner.Flush();

        public override int Read(byte[] buffer, int offset, int count)
            => _inner.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin)
            => _inner.Seek(offset, origin);

        public override void SetLength(long value) => _inner.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count)
            => _inner.Write(buffer, offset, count);

        public override async ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
            => await _inner.ReadAsync(buffer, cancellationToken);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
                _response.Dispose();
            }

            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            await _inner.DisposeAsync();
            _response.Dispose();
            await base.DisposeAsync();
        }
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }

    private sealed class GraphSiteResponse
    {
        public string Id { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string? WebUrl { get; set; }
    }

    private sealed class GraphDriveCollectionResponse
    {
        public IReadOnlyCollection<GraphDriveResponse> Value { get; set; } = Array.Empty<GraphDriveResponse>();
    }

    private sealed class GraphDriveResponse
    {
        public string Id { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? WebUrl { get; set; }
        public string? DriveType { get; set; }
    }

    private sealed class GraphDriveItemResponse
    {
        public string Id { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? WebUrl { get; set; }
        public long? Size { get; set; }
        public DateTimeOffset? LastModifiedDateTime { get; set; }
        public object? File { get; set; }
        public object? Folder { get; set; }
    }

    private sealed class GraphDriveItemCollectionResponse
    {
        public IReadOnlyCollection<GraphDriveItemResponse> Value { get; set; } = Array.Empty<GraphDriveItemResponse>();

        [JsonPropertyName("@odata.nextLink")]
        public string? NextLink { get; set; }
    }

    private sealed class GraphListResponse
    {
        public string Id { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string? WebUrl { get; set; }
    }

    private sealed class GraphUploadSessionResponse
    {
        public string UploadUrl { get; set; } = string.Empty;
    }
}

public sealed record SharePointSiteDescriptor(string Id, string WebUrl, string Name);
public sealed record SharePointDriveDescriptor(string Id, string Name, string WebUrl);
public sealed record SharePointDriveItemDescriptor(string Id, string Name, string? WebUrl);
public sealed record SharePointFileDescriptor(
    string DriveId,
    string LibraryName,
    string DriveItemId,
    string Name,
    string RelativePath,
    long SizeInBytes,
    DateTimeOffset ModifiedUtc,
    string? WebUrl);
