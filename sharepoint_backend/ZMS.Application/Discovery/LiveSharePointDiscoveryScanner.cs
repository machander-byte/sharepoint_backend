using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZMS.Core.Options;
using ZMS.Core.Security;

namespace ZMS.Application.Discovery;

public sealed class LiveSharePointDiscoveryScanner : ILiveSharePointDiscoveryScanner
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly DiscoveryOptions _options;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LiveSharePointDiscoveryScanner> _logger;

    public LiveSharePointDiscoveryScanner(
        HttpClient httpClient,
        IOptions<DiscoveryOptions> options,
        IConfiguration configuration,
        ILogger<LiveSharePointDiscoveryScanner> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<DiscoveryScanResult> ScanAsync(
        string scanId,
        DiscoveryScanRequest request,
        Func<int, string, Task> reportProgress,
        CancellationToken cancellationToken = default)
    {
        await reportProgress(10, "Preparing Microsoft Graph discovery");
        var result = new DiscoveryScanResult
        {
            ScanId = scanId,
            ScanName = string.IsNullOrWhiteSpace(request.ScanName) ? "Live Microsoft Graph SharePoint discovery" : request.ScanName,
            Mode = "live",
            Status = "running",
            StartedAt = DateTimeOffset.UtcNow
        };

        var credentials = ResolveCredentials(request);
        if (!credentials.IsConfigured)
        {
            result.IsPartial = true;
            result.Warnings.Add("Live Microsoft Graph discovery is not configured. Set DISCOVERY_TENANT_ID, DISCOVERY_CLIENT_ID, and DISCOVERY_CLIENT_SECRET or use config/demo discovery.");
            AddPlaceholderSites(request, result);
            return result;
        }

        var siteUrls = ResolveSiteUrls(request);
        if (siteUrls.Count == 0)
        {
            result.IsPartial = true;
            result.Errors.Add("At least one SharePoint site URL or tenant URL is required for live Graph discovery.");
            return result;
        }

        string accessToken;
        try
        {
            accessToken = await AcquireAccessTokenAsync(credentials, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or JsonException)
        {
            result.IsPartial = true;
            result.Errors.Add(SecretRedactor.Redact(ex.Message));
            AddPlaceholderSites(request, result);
            return result;
        }

        var maxItems = ResolveMaxItems(request);
        var scannedItems = 0;
        var siteIndex = 0;

        foreach (var siteUrl in siteUrls)
        {
            cancellationToken.ThrowIfCancellationRequested();
            siteIndex++;
            await reportProgress(Math.Min(75, 10 + (siteIndex * 60 / Math.Max(1, siteUrls.Count))), $"Scanning {siteUrl}");

            try
            {
                var site = await ResolveSiteAsync(accessToken, siteUrl, result, cancellationToken);
                var discoveredSite = new DiscoveredSiteCollection
                {
                    Id = site.Id,
                    Title = string.IsNullOrWhiteSpace(site.DisplayName) ? TitleFromUrl(siteUrl) : site.DisplayName,
                    Url = string.IsNullOrWhiteSpace(site.WebUrl) ? siteUrl : site.WebUrl,
                    Description = "Scanned with Microsoft Graph discovery v1."
                };

                result.SiteCollections.Add(discoveredSite);
                result.InventoryItems.Add(new DiscoveredInventoryItem
                {
                    Id = StableId(discoveredSite.Url),
                    SiteCollection = discoveredSite.Title,
                    Subsite = "Root",
                    ItemType = "Site Collection",
                    Path = discoveredSite.Url,
                    PermissionStatus = "Graph scanned",
                    RiskLevel = "Low",
                    ReadinessStatus = "Scanned"
                });

                await ScanListsAsync(accessToken, site, discoveredSite, request, result, cancellationToken);
                await ScanDrivesAsync(accessToken, site, discoveredSite, request, result, maxItems, () => scannedItems, value => scannedItems = value, cancellationToken);
            }
            catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or JsonException)
            {
                result.IsPartial = true;
                var warning = SecretRedactor.Redact($"Partial Graph discovery failure for '{siteUrl}': {ex.Message}");
                result.Warnings.Add(warning);
                _logger.LogWarning("Partial Graph discovery failure for {SiteUrl}: {Message}", siteUrl, SecretRedactor.Redact(ex.Message));
            }

            if (scannedItems >= maxItems)
            {
                result.IsPartial = true;
                result.Warnings.Add($"Discovery stopped after reaching DISCOVERY_MAX_ITEMS={maxItems}.");
                break;
            }
        }

        return result;
    }

    private async Task ScanListsAsync(
        string accessToken,
        GraphSite site,
        DiscoveredSiteCollection discoveredSite,
        DiscoveryScanRequest request,
        DiscoveryScanResult result,
        CancellationToken cancellationToken)
    {
        GraphCollection<GraphList>? lists;
        try
        {
            lists = await GetGraphAsync<GraphCollection<GraphList>>(
                accessToken,
                $"https://graph.microsoft.com/v1.0/sites/{site.Id}/lists?$select=id,displayName,webUrl,description,list",
                result,
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            result.IsPartial = true;
            result.Warnings.Add(SecretRedactor.Redact($"List scan unavailable for '{discoveredSite.Url}': {ex.Message}"));
            return;
        }

        foreach (var list in lists.Value)
        {
            var template = list.List?.Template ?? string.Empty;
            if (template.Equals("documentLibrary", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var discoveredList = new DiscoveredList
            {
                Id = list.Id,
                Title = list.DisplayName ?? "List",
                Description = list.Description ?? string.Empty
            };

            if (request.IncludeMetadata)
            {
                discoveredList.Fields = await TryGetColumnsAsync(accessToken, site.Id, list.Id, result, cancellationToken);
            }

            discoveredSite.Lists.Add(discoveredList);
            result.InventoryItems.Add(new DiscoveredInventoryItem
            {
                Id = StableId($"{site.Id}-{list.Id}"),
                SiteCollection = discoveredSite.Title,
                Subsite = "Root",
                Library = discoveredList.Title,
                ItemType = "List",
                Path = list.WebUrl ?? discoveredSite.Url,
                MetadataCount = discoveredList.Fields.Count,
                PermissionStatus = "Unknown",
                RiskLevel = "Low",
                ReadinessStatus = "Scanned"
            });
        }
    }

    private async Task ScanDrivesAsync(
        string accessToken,
        GraphSite site,
        DiscoveredSiteCollection discoveredSite,
        DiscoveryScanRequest request,
        DiscoveryScanResult result,
        int maxItems,
        Func<int> getScannedItems,
        Action<int> setScannedItems,
        CancellationToken cancellationToken)
    {
        var drives = await GetGraphAsync<GraphCollection<GraphDrive>>(
            accessToken,
            $"https://graph.microsoft.com/v1.0/sites/{site.Id}/drives?$select=id,name,webUrl,driveType",
            result,
            cancellationToken);

        foreach (var drive in drives.Value.Where(item =>
            string.IsNullOrWhiteSpace(item.DriveType)
            || item.DriveType.Equals("documentLibrary", StringComparison.OrdinalIgnoreCase)))
        {
            if (getScannedItems() >= maxItems)
            {
                return;
            }

            var library = new DiscoveredLibrary
            {
                Id = drive.Id,
                Title = drive.Name ?? "Documents",
                Type = "Document Library",
                Url = drive.WebUrl ?? discoveredSite.Url
            };

            if (request.IncludeMetadata)
            {
                library.ContentTypes = await TryGetDriveContentTypesAsync(accessToken, site.Id, library.Title, result, cancellationToken);
            }

            if (ShouldIncludePermissions(request))
            {
                library.Permissions = await TryGetDrivePermissionsAsync(accessToken, drive.Id, "root", discoveredSite.Title, library.Title, result, cancellationToken);
                library.BrokenInheritance = library.Permissions.Any(permission =>
                    permission.InheritanceStatus.Contains("unique", StringComparison.OrdinalIgnoreCase)
                    || permission.InheritanceStatus.Contains("broken", StringComparison.OrdinalIgnoreCase));
            }

            await WalkDriveAsync(
                accessToken,
                drive,
                "root",
                string.Empty,
                0,
                ResolveMaxDepth(request),
                maxItems,
                getScannedItems,
                setScannedItems,
                library,
                result,
                cancellationToken);

            library.FileCount = library.Files.Count;
            library.FolderCount = library.Folders.Count;
            library.SizeBytes = library.Files.Sum(file => file.SizeBytes);
            discoveredSite.FileCount += library.FileCount;
            discoveredSite.FolderCount += library.FolderCount;
            discoveredSite.SizeBytes += library.SizeBytes;
            discoveredSite.Libraries.Add(library);

            result.InventoryItems.Add(new DiscoveredInventoryItem
            {
                Id = StableId($"{site.Id}-{drive.Id}"),
                SiteCollection = discoveredSite.Title,
                Subsite = "Root",
                Library = library.Title,
                ItemType = "Library",
                Path = library.Url,
                FileCount = library.FileCount,
                SizeBytes = library.SizeBytes,
                MetadataCount = library.MetadataFields.Count + library.ContentTypes.Count,
                PermissionStatus = library.BrokenInheritance ? "Unique permissions detected" : "Inherited or unavailable",
                RiskLevel = library.BrokenInheritance ? "High" : "Low",
                ReadinessStatus = library.BrokenInheritance ? "Needs permission review" : "Scanned"
            });
        }
    }

    private async Task WalkDriveAsync(
        string accessToken,
        GraphDrive drive,
        string itemId,
        string relativePath,
        int depth,
        int maxDepth,
        int maxItems,
        Func<int> getScannedItems,
        Action<int> setScannedItems,
        DiscoveredLibrary library,
        DiscoveryScanResult result,
        CancellationToken cancellationToken)
    {
        if (depth > maxDepth || getScannedItems() >= maxItems)
        {
            return;
        }

        var requestUri = itemId == "root"
            ? $"https://graph.microsoft.com/v1.0/drives/{drive.Id}/root/children?$select=id,name,size,createdDateTime,lastModifiedDateTime,webUrl,file,folder"
            : $"https://graph.microsoft.com/v1.0/drives/{drive.Id}/items/{itemId}/children?$select=id,name,size,createdDateTime,lastModifiedDateTime,webUrl,file,folder";

        while (!string.IsNullOrWhiteSpace(requestUri) && getScannedItems() < maxItems)
        {
            var children = await GetGraphAsync<GraphCollection<GraphDriveItem>>(accessToken, requestUri, result, cancellationToken);
            foreach (var child in children.Value)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (getScannedItems() >= maxItems)
                {
                    break;
                }

                setScannedItems(getScannedItems() + 1);
                var childPath = CombinePath(relativePath, child.Name ?? child.Id);
                if (child.Folder is not null)
                {
                    library.Folders.Add(new DiscoveredFolder
                    {
                        Id = child.Id,
                        Name = child.Name ?? child.Id,
                        Path = childPath,
                        Depth = depth + 1,
                        LongPathRisk = childPath.Length > 350
                    });

                    await WalkDriveAsync(
                        accessToken,
                        drive,
                        child.Id,
                        childPath,
                        depth + 1,
                        maxDepth,
                        maxItems,
                        getScannedItems,
                        setScannedItems,
                        library,
                        result,
                        cancellationToken);
                    continue;
                }

                if (child.File is null)
                {
                    continue;
                }

                library.Files.Add(new DiscoveredFile
                {
                    Name = child.Name ?? child.Id,
                    Path = childPath,
                    Url = child.WebUrl ?? string.Empty,
                    SizeBytes = child.Size ?? 0,
                    CreatedAt = child.CreatedDateTime,
                    ModifiedAt = child.LastModifiedDateTime,
                    LargeFileRisk = (child.Size ?? 0) > 250L * 1024L * 1024L,
                    LongPathRisk = childPath.Length > 350
                });
            }

            requestUri = children.NextLink;
        }
    }

    private async Task<List<DiscoveredMetadataField>> TryGetColumnsAsync(
        string accessToken,
        string siteId,
        string listId,
        DiscoveryScanResult result,
        CancellationToken cancellationToken)
    {
        try
        {
            var columns = await GetGraphAsync<GraphCollection<GraphColumn>>(
                accessToken,
                $"https://graph.microsoft.com/v1.0/sites/{siteId}/lists/{listId}/columns?$select=id,name,displayName,required",
                result,
                cancellationToken);

            return columns.Value.Select(column => new DiscoveredMetadataField
            {
                Id = column.Id,
                Name = column.DisplayName ?? column.Name ?? column.Id,
                FieldType = "GraphColumn",
                Required = column.Required
            }).ToList();
        }
        catch (InvalidOperationException ex)
        {
            result.IsPartial = true;
            result.Warnings.Add(SecretRedactor.Redact($"Column metadata scan unavailable: {ex.Message}"));
            return [];
        }
    }

    private async Task<List<string>> TryGetDriveContentTypesAsync(
        string accessToken,
        string siteId,
        string libraryName,
        DiscoveryScanResult result,
        CancellationToken cancellationToken)
    {
        try
        {
            var lists = await GetGraphAsync<GraphCollection<GraphList>>(
                accessToken,
                $"https://graph.microsoft.com/v1.0/sites/{siteId}/lists?$select=id,displayName,list",
                result,
                cancellationToken);
            var list = lists.Value.FirstOrDefault(candidate =>
                string.Equals(candidate.DisplayName, libraryName, StringComparison.OrdinalIgnoreCase));
            if (list is null)
            {
                return [];
            }

            var contentTypes = await GetGraphAsync<GraphCollection<GraphContentType>>(
                accessToken,
                $"https://graph.microsoft.com/v1.0/sites/{siteId}/lists/{list.Id}/contentTypes?$select=id,name",
                result,
                cancellationToken);
            return contentTypes.Value.Select(contentType => contentType.Name ?? contentType.Id).ToList();
        }
        catch (InvalidOperationException ex)
        {
            result.IsPartial = true;
            result.Warnings.Add(SecretRedactor.Redact($"Content type scan unavailable: {ex.Message}"));
            return [];
        }
    }

    private async Task<List<DiscoveredPermissionEntry>> TryGetDrivePermissionsAsync(
        string accessToken,
        string driveId,
        string itemId,
        string siteTitle,
        string scope,
        DiscoveryScanResult result,
        CancellationToken cancellationToken)
    {
        try
        {
            var permissions = await GetGraphAsync<GraphCollection<GraphPermission>>(
                accessToken,
                $"https://graph.microsoft.com/v1.0/drives/{driveId}/items/{itemId}/permissions?$select=id,roles,link,grantedToV2,grantedToIdentitiesV2",
                result,
                cancellationToken);

            return permissions.Value.Select(permission =>
            {
                var users = new List<string>();
                if (!string.IsNullOrWhiteSpace(permission.GrantedToV2?.User?.DisplayName))
                {
                    users.Add(permission.GrantedToV2.User.DisplayName);
                }

                users.AddRange(permission.GrantedToIdentitiesV2
                    .Select(identity => identity.User?.DisplayName)
                    .Where(value => !string.IsNullOrWhiteSpace(value))!);

                var accessLevels = permission.Roles.ToList();
                if (permission.Link is not null)
                {
                    accessLevels.Add($"{permission.Link.Scope}:{permission.Link.Type}");
                }

                var anonymous = permission.Link?.Scope?.Contains("anonymous", StringComparison.OrdinalIgnoreCase) == true;
                var external = permission.Link?.Scope?.Contains("users", StringComparison.OrdinalIgnoreCase) == true;
                return new DiscoveredPermissionEntry
                {
                    Site = siteTitle,
                    LibraryOrFolder = scope,
                    InheritanceStatus = "Graph permission",
                    Users = users,
                    AccessLevels = accessLevels,
                    RiskLevel = anonymous ? "Critical" : external ? "High" : "Low",
                    RecommendedAction = anonymous
                        ? "Remove anonymous sharing links before migration."
                        : external
                            ? "Review external sharing links before migration."
                            : "Review permissions during cutover validation."
                };
            }).ToList();
        }
        catch (InvalidOperationException ex)
        {
            result.IsPartial = true;
            result.Warnings.Add(SecretRedactor.Redact($"Permission scan unavailable: {ex.Message}"));
            return [];
        }
    }

    private async Task<string> AcquireAccessTokenAsync(GraphCredentials credentials, CancellationToken cancellationToken)
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = credentials.ClientId,
            ["scope"] = "https://graph.microsoft.com/.default",
            ["client_secret"] = credentials.ClientSecret,
            ["grant_type"] = "client_credentials"
        });

        using var response = await _httpClient.PostAsync(
            $"https://login.microsoftonline.com/{credentials.TenantId}/oauth2/v2.0/token",
            content,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(SecretRedactor.Redact($"Microsoft Graph token acquisition failed with status {(int)response.StatusCode}: {error}"));
        }

        var token = await response.Content.ReadFromJsonAsync<TokenResponse>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Microsoft Graph token endpoint returned an empty response.");
        return token.AccessToken;
    }

    private async Task<GraphSite> ResolveSiteAsync(
        string accessToken,
        string siteUrl,
        DiscoveryScanResult result,
        CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(siteUrl, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException($"Invalid SharePoint site URL '{siteUrl}'.");
        }

        var relativePath = string.Join(
            '/',
            uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(Uri.EscapeDataString));
        var requestUri = string.IsNullOrWhiteSpace(relativePath)
            ? $"https://graph.microsoft.com/v1.0/sites/{uri.Host}:/"
            : $"https://graph.microsoft.com/v1.0/sites/{uri.Host}:/{relativePath}";

        return await GetGraphAsync<GraphSite>(accessToken, requestUri, result, cancellationToken);
    }

    private async Task<T> GetGraphAsync<T>(
        string accessToken,
        string requestUri,
        DiscoveryScanResult result,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 4; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var value = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
                return value ?? throw new InvalidOperationException("Microsoft Graph returned an empty response body.");
            }

            if (response.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable)
            {
                result.ThrottleCount++;
                var delay = ResolveRetryDelay(response, attempt);
                result.Warnings.Add($"Microsoft Graph throttled discovery with HTTP {(int)response.StatusCode}; retrying after {delay.TotalSeconds:n0}s.");
                await Task.Delay(delay, cancellationToken);
                continue;
            }

            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(SecretRedactor.Redact($"Microsoft Graph request failed with status {(int)response.StatusCode}: {error}"));
        }

        throw new InvalidOperationException("Microsoft Graph throttling retry limit was exceeded.");
    }

    private GraphCredentials ResolveCredentials(DiscoveryScanRequest request)
    {
        var tenantId = _configuration["DISCOVERY_TENANT_ID"] ?? _configuration["Discovery:TenantId"] ?? _options.TenantId;
        var clientId = _configuration["DISCOVERY_CLIENT_ID"]
            ?? _configuration["Discovery:ClientId"]
            ?? _options.ClientId
            ?? request.ClientId;
        var clientSecret = _configuration["DISCOVERY_CLIENT_SECRET"] ?? _configuration["Discovery:ClientSecret"] ?? _options.ClientSecret;

        return new GraphCredentials(tenantId ?? string.Empty, clientId ?? string.Empty, clientSecret ?? string.Empty);
    }

    private static IReadOnlyCollection<string> ResolveSiteUrls(DiscoveryScanRequest request)
    {
        var siteUrls = request.SiteUrls
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Select(url => url.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (siteUrls.Count == 0 && !string.IsNullOrWhiteSpace(request.TenantUrl))
        {
            siteUrls.Add(request.TenantUrl.TrimEnd('/'));
        }

        return siteUrls;
    }

    private int ResolveMaxDepth(DiscoveryScanRequest request)
    {
        var configured = _configuration["DISCOVERY_MAX_DEPTH"] ?? _configuration["Discovery:MaxDepth"];
        return Math.Clamp(request.MaxDepth ?? ParseInt(configured, _options.MaxDepth), 1, 20);
    }

    private int ResolveMaxItems(DiscoveryScanRequest request)
    {
        var configured = _configuration["DISCOVERY_MAX_ITEMS"] ?? _configuration["Discovery:MaxItems"];
        return Math.Clamp(request.MaxItems ?? ParseInt(configured, _options.MaxItems), 1, 1_000_000);
    }

    private bool ShouldIncludePermissions(DiscoveryScanRequest request)
    {
        var configured = _configuration["DISCOVERY_INCLUDE_PERMISSIONS"] ?? _configuration["Discovery:IncludePermissions"];
        return request.IncludePermissions && ParseBool(configured, _options.IncludePermissions);
    }

    private static int ParseInt(string? value, int fallback) => int.TryParse(value, out var parsed) ? parsed : fallback;

    private static bool ParseBool(string? value, bool fallback) => bool.TryParse(value, out var parsed) ? parsed : fallback;

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

        return TimeSpan.FromSeconds(Math.Min(30, Math.Pow(2, attempt + 1)));
    }

    private static void AddPlaceholderSites(DiscoveryScanRequest request, DiscoveryScanResult result)
    {
        foreach (var siteUrl in ResolveSiteUrls(request))
        {
            var title = TitleFromUrl(siteUrl);
            result.SiteCollections.Add(new DiscoveredSiteCollection
            {
                Id = StableId(siteUrl),
                Title = title,
                Url = siteUrl,
                Description = "Live Graph discovery fallback placeholder. Configure Graph credentials for full tenant reads."
            });
        }
    }

    private static string TitleFromUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return url.Trim('/').Split('/').LastOrDefault() ?? "SharePoint Site";
        }

        var segment = uri.Segments.LastOrDefault()?.Trim('/') ?? uri.Host;
        return string.IsNullOrWhiteSpace(segment)
            ? uri.Host
            : segment.Replace("-", " ", StringComparison.Ordinal);
    }

    private static string StableId(string value)
    {
        var chars = value
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray();
        return string.Join(string.Empty, chars).Replace("--", "-", StringComparison.Ordinal).Trim('-');
    }

    private static string CombinePath(string left, string right)
    {
        var normalizedRight = right.Trim().Replace('\\', '/').Trim('/');
        return string.IsNullOrWhiteSpace(left)
            ? normalizedRight
            : $"{left.TrimEnd('/')}/{normalizedRight}";
    }

    private sealed record GraphCredentials(string TenantId, string ClientId, string ClientSecret)
    {
        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(TenantId)
            && !string.IsNullOrWhiteSpace(ClientId)
            && !string.IsNullOrWhiteSpace(ClientSecret);
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;
    }

    private sealed class GraphCollection<T>
    {
        public IReadOnlyCollection<T> Value { get; set; } = Array.Empty<T>();

        [JsonPropertyName("@odata.nextLink")]
        public string? NextLink { get; set; }
    }

    private sealed class GraphSite
    {
        public string Id { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string? WebUrl { get; set; }
    }

    private sealed class GraphDrive
    {
        public string Id { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? WebUrl { get; set; }
        public string? DriveType { get; set; }
    }

    private sealed class GraphDriveItem
    {
        public string Id { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? WebUrl { get; set; }
        public long? Size { get; set; }
        public DateTimeOffset? CreatedDateTime { get; set; }
        public DateTimeOffset? LastModifiedDateTime { get; set; }
        public object? File { get; set; }
        public object? Folder { get; set; }
    }

    private sealed class GraphList
    {
        public string Id { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string? WebUrl { get; set; }
        public string? Description { get; set; }
        public GraphListFacet? List { get; set; }
    }

    private sealed class GraphListFacet
    {
        public string? Template { get; set; }
    }

    private sealed class GraphColumn
    {
        public string Id { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? DisplayName { get; set; }
        public bool Required { get; set; }
    }

    private sealed class GraphContentType
    {
        public string Id { get; set; } = string.Empty;
        public string? Name { get; set; }
    }

    private sealed class GraphPermission
    {
        public IReadOnlyCollection<string> Roles { get; set; } = Array.Empty<string>();
        public GraphLink? Link { get; set; }
        public GraphIdentitySet? GrantedToV2 { get; set; }
        public IReadOnlyCollection<GraphIdentitySet> GrantedToIdentitiesV2 { get; set; } = Array.Empty<GraphIdentitySet>();
    }

    private sealed class GraphLink
    {
        public string? Scope { get; set; }
        public string? Type { get; set; }
    }

    private sealed class GraphIdentitySet
    {
        public GraphIdentity? User { get; set; }
    }

    private sealed class GraphIdentity
    {
        public string? DisplayName { get; set; }
    }
}
