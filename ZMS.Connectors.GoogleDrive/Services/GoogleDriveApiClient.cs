using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZMS.Core.Models;
using ZMS.Core.Options;

namespace ZMS.Connectors.GoogleDrive.Services;

public class GoogleDriveApiClient
{
    private const string GoogleDriveFolderMimeType = "application/vnd.google-apps.folder";
    private const string GoogleDriveFolderIdKey = "FolderId";
    private const string GoogleDriveFolderUrlKey = "FolderUrl";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GoogleDriveApiClient> _logger;
    private readonly GoogleDriveOptions _options;
    private readonly ConcurrentDictionary<Guid, CachedToken> _tokenCache = new();

    public GoogleDriveApiClient(
        IHttpClientFactory httpClientFactory,
        ILogger<GoogleDriveApiClient> logger,
        IOptions<GoogleDriveOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<ConnectionTestResult> TestConnectionAsync(
        ConnectionProfile connection,
        CancellationToken cancellationToken)
    {
        var credentials = ResolveCredentials();
        if (!credentials.IsConfigured)
        {
            return new ConnectionTestResult
            {
                IsSuccess = false,
                Message = "Google Drive backend credentials are not configured. Set GOOGLE_CLIENT_ID, GOOGLE_CLIENT_SECRET, and GOOGLE_REFRESH_TOKEN before testing."
            };
        }

        try
        {
            var rootFolder = await GetFolderAsync(
                connection,
                ResolveFolderId(connection, connection.RootPath ?? connection.Url),
                cancellationToken);

            if (!string.Equals(rootFolder.MimeType, GoogleDriveFolderMimeType, StringComparison.OrdinalIgnoreCase))
            {
                return new ConnectionTestResult
                {
                    IsSuccess = false,
                    Message = "Google Drive folder is not accessible because the configured ID does not point to a folder."
                };
            }

            _ = await ListChildrenAsync(connection, rootFolder.Id, cancellationToken);

            return new ConnectionTestResult
            {
                IsSuccess = true,
                Message = $"Google Drive connection verified for '{rootFolder.Name}'."
            };
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Google Drive connection test failed for '{ConnectionName}'.", connection.Name);
            return new ConnectionTestResult
            {
                IsSuccess = false,
                Message = exception.Message
            };
        }
    }

    public string ResolveFolderId(ConnectionProfile connection, string? candidate)
    {
        var rawValue = string.IsNullOrWhiteSpace(candidate)
            ? connection.RootPath ?? connection.Url
            : candidate;

        if (string.IsNullOrWhiteSpace(rawValue)
            && connection.AdditionalSettings.TryGetValue(GoogleDriveFolderIdKey, out var configuredFolderId))
        {
            rawValue = configuredFolderId;
        }

        if (string.IsNullOrWhiteSpace(rawValue)
            && connection.AdditionalSettings.TryGetValue(GoogleDriveFolderUrlKey, out var configuredFolderUrl))
        {
            rawValue = configuredFolderUrl;
        }

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            throw new InvalidOperationException("Google Drive folder ID is missing.");
        }

        rawValue = rawValue.Trim();

        if (!Uri.TryCreate(rawValue, UriKind.Absolute, out var uri))
        {
            return rawValue;
        }

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (var index = 0; index < segments.Length - 1; index++)
        {
            if (segments[index].Equals("folders", StringComparison.OrdinalIgnoreCase))
            {
                return segments[index + 1];
            }
        }

        var query = ParseQuery(uri.Query);
        if (query.TryGetValue("id", out var folderId) && !string.IsNullOrWhiteSpace(folderId))
        {
            return folderId;
        }

        return "root";
    }

    public async Task<GoogleDriveItemDescriptor> GetFolderAsync(
        ConnectionProfile connection,
        string folderId,
        CancellationToken cancellationToken)
    {
        var requestUri =
            $"https://www.googleapis.com/drive/v3/files/{Uri.EscapeDataString(folderId)}?fields=id,name,mimeType,modifiedTime&supportsAllDrives=true";

        using var response = await SendDriveAsync(connection, HttpMethod.Get, requestUri, null, cancellationToken);
        var folder = await ReadRequiredAsync<GoogleDriveItemResponse>(response, cancellationToken);

        return new GoogleDriveItemDescriptor(
            folder.Id,
            folder.Name ?? folderId,
            folder.MimeType ?? GoogleDriveFolderMimeType,
            folder.Size,
            folder.ModifiedTime);
    }

    public async Task<IReadOnlyCollection<GoogleDriveItemDescriptor>> ListChildrenAsync(
        ConnectionProfile connection,
        string folderId,
        CancellationToken cancellationToken)
    {
        var items = new List<GoogleDriveItemDescriptor>();
        string? pageToken = null;

        do
        {
            var query = $"'{folderId}' in parents and trashed = false";
            var requestUri =
                $"https://www.googleapis.com/drive/v3/files?q={Uri.EscapeDataString(query)}&fields=nextPageToken,files(id,name,mimeType,size,modifiedTime)&pageSize=1000&includeItemsFromAllDrives=true&supportsAllDrives=true";

            if (!string.IsNullOrWhiteSpace(pageToken))
            {
                requestUri += $"&pageToken={Uri.EscapeDataString(pageToken)}";
            }

            using var response = await SendDriveAsync(connection, HttpMethod.Get, requestUri, null, cancellationToken);
            var result = await ReadRequiredAsync<GoogleDriveFileListResponse>(response, cancellationToken);

            items.AddRange(result.Files.Select(file => new GoogleDriveItemDescriptor(
                file.Id,
                file.Name ?? file.Id,
                file.MimeType ?? "application/octet-stream",
                file.Size,
                file.ModifiedTime)));

            pageToken = result.NextPageToken;
        }
        while (!string.IsNullOrWhiteSpace(pageToken));

        return items;
    }

    public Task<GoogleDriveDownloadDescriptor> ResolveDownloadDescriptorAsync(
        string fileName,
        string mimeType,
        CancellationToken cancellationToken)
    {
        var descriptor = ResolveDownloadDescriptor(fileName, mimeType);
        return Task.FromResult(descriptor);
    }

    public async Task<Stream> OpenReadAsync(
        ConnectionProfile connection,
        string fileId,
        string mimeType,
        string? exportMimeType,
        CancellationToken cancellationToken)
    {
        var requestUri = string.IsNullOrWhiteSpace(exportMimeType)
            ? $"https://www.googleapis.com/drive/v3/files/{Uri.EscapeDataString(fileId)}?alt=media&supportsAllDrives=true"
            : $"https://www.googleapis.com/drive/v3/files/{Uri.EscapeDataString(fileId)}/export?mimeType={Uri.EscapeDataString(exportMimeType)}";

        var accessToken = await GetAccessTokenAsync(connection, cancellationToken);
        var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            response.Dispose();
            request.Dispose();
            throw new InvalidOperationException(
                $"Google Drive download failed for '{fileId}' with status {(int)response.StatusCode}: {error}");
        }

        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return new OwnedHttpResponseStream(request, response, stream);
    }

    public static GoogleDriveDownloadDescriptor ResolveDownloadDescriptor(string fileName, string mimeType)
    {
        if (!mimeType.StartsWith("application/vnd.google-apps.", StringComparison.OrdinalIgnoreCase))
        {
            return new GoogleDriveDownloadDescriptor(fileName, null);
        }

        return mimeType switch
        {
            "application/vnd.google-apps.document" => EnsureExtension(fileName, ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"),
            "application/vnd.google-apps.spreadsheet" => EnsureExtension(fileName, ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"),
            "application/vnd.google-apps.presentation" => EnsureExtension(fileName, ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation"),
            "application/vnd.google-apps.drawing" => EnsureExtension(fileName, ".pdf", "application/pdf"),
            _ => EnsureExtension(fileName, ".pdf", "application/pdf")
        };
    }

    private static GoogleDriveDownloadDescriptor EnsureExtension(
        string fileName,
        string extension,
        string exportMimeType)
    {
        var resolvedName = fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase)
            ? fileName
            : $"{fileName}{extension}";

        return new GoogleDriveDownloadDescriptor(resolvedName, exportMimeType);
    }

    private async Task<HttpResponseMessage> SendDriveAsync(
        ConnectionProfile connection,
        HttpMethod method,
        string requestUri,
        HttpContent? content,
        CancellationToken cancellationToken)
    {
        var accessToken = await GetAccessTokenAsync(connection, cancellationToken);
        var client = _httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(method, requestUri) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        var error = await response.Content.ReadAsStringAsync(cancellationToken);
        response.Dispose();
        throw new InvalidOperationException(
            $"Google Drive request failed with status {(int)response.StatusCode}: {error}");
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

        var credentials = ResolveCredentials();
        if (!credentials.IsConfigured)
        {
            throw new InvalidOperationException(
                "Google Drive backend credentials are not configured. Set GOOGLE_CLIENT_ID, GOOGLE_CLIENT_SECRET, and GOOGLE_REFRESH_TOKEN.");
        }

        var requestBody = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = credentials.ClientId!,
            ["client_secret"] = credentials.ClientSecret!,
            ["refresh_token"] = credentials.RefreshToken!,
            ["grant_type"] = "refresh_token"
        });

        var client = _httpClientFactory.CreateClient();
        using var response = await client.PostAsync(
            "https://oauth2.googleapis.com/token",
            requestBody,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                "Google Drive credentials are invalid or expired. Update backend Google configuration.");
        }

        var token = await ReadRequiredAsync<GoogleTokenResponse>(response, cancellationToken);
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

    private static Dictionary<string, string> ParseQuery(string queryString)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(queryString))
        {
            return values;
        }

        foreach (var pair in queryString.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            var key = Uri.UnescapeDataString(parts[0]);
            var value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;
            values[key] = value;
        }

        return values;
    }

    private GoogleDriveCredentials ResolveCredentials()
    {
        var clientId = FirstConfiguredValue(_options.ClientId, Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID"));
        var clientSecret = FirstConfiguredValue(_options.ClientSecret, Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET"));
        var refreshToken = FirstConfiguredValue(_options.RefreshToken, Environment.GetEnvironmentVariable("GOOGLE_REFRESH_TOKEN"));

        return new GoogleDriveCredentials(clientId, clientSecret, refreshToken);
    }

    private static string? FirstConfiguredValue(params string?[] values)
        => values.Select(value => value?.Trim()).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private sealed record GoogleDriveCredentials(string? ClientId, string? ClientSecret, string? RefreshToken)
    {
        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(ClientId)
            && !string.IsNullOrWhiteSpace(ClientSecret)
            && !string.IsNullOrWhiteSpace(RefreshToken);
    }

    private sealed record CachedToken(string AccessToken, DateTimeOffset ExpiresUtc);

    private sealed class GoogleTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }

    private sealed class GoogleDriveFileListResponse
    {
        [JsonPropertyName("nextPageToken")]
        public string? NextPageToken { get; set; }

        [JsonPropertyName("files")]
        public IReadOnlyCollection<GoogleDriveItemResponse> Files { get; set; } = Array.Empty<GoogleDriveItemResponse>();
    }

    private sealed class GoogleDriveItemResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("mimeType")]
        public string? MimeType { get; set; }

        [JsonPropertyName("size")]
        public long? Size { get; set; }

        [JsonPropertyName("modifiedTime")]
        public DateTimeOffset? ModifiedTime { get; set; }
    }

    private sealed class OwnedHttpResponseStream : Stream
    {
        private readonly HttpRequestMessage _request;
        private readonly HttpResponseMessage _response;
        private readonly Stream _innerStream;

        public OwnedHttpResponseStream(
            HttpRequestMessage request,
            HttpResponseMessage response,
            Stream innerStream)
        {
            _request = request;
            _response = response;
            _innerStream = innerStream;
        }

        public override bool CanRead => _innerStream.CanRead;
        public override bool CanSeek => _innerStream.CanSeek;
        public override bool CanWrite => _innerStream.CanWrite;
        public override long Length => _innerStream.Length;
        public override long Position
        {
            get => _innerStream.Position;
            set => _innerStream.Position = value;
        }

        public override void Flush() => _innerStream.Flush();

        public override int Read(byte[] buffer, int offset, int count) => _innerStream.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => _innerStream.Seek(offset, origin);

        public override void SetLength(long value) => _innerStream.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count) => _innerStream.Write(buffer, offset, count);

        public override ValueTask DisposeAsync()
        {
            _response.Dispose();
            _request.Dispose();
            return _innerStream.DisposeAsync();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _innerStream.Dispose();
                _response.Dispose();
                _request.Dispose();
            }

            base.Dispose(disposing);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => _innerStream.ReadAsync(buffer, offset, count, cancellationToken);

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => _innerStream.ReadAsync(buffer, cancellationToken);

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => _innerStream.WriteAsync(buffer, offset, count, cancellationToken);

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            => _innerStream.WriteAsync(buffer, cancellationToken);
    }
}

public sealed record GoogleDriveItemDescriptor(
    string Id,
    string Name,
    string MimeType,
    long? Size,
    DateTimeOffset? ModifiedTime);

public sealed record GoogleDriveDownloadDescriptor(string FileName, string? ExportMimeType);
