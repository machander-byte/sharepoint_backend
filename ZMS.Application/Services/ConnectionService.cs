using ZMS.Application.Contracts;
using ZMS.Core.Enums;
using ZMS.Core.Interfaces;
using ZMS.Core.Models;

namespace ZMS.Application.Services;

public class ConnectionService : IConnectionService
{
    private const string GoogleFolderIdKey = "FolderId";
    private const string GoogleFolderUrlKey = "FolderUrl";
    private const string SharePointDocumentLibraryNameKey = "DocumentLibraryName";

    private readonly IConnectionRepository _connectionRepository;
    private readonly ConnectorResolver _connectorResolver;
    private readonly ISecretProtector _secretProtector;

    public ConnectionService(
        IConnectionRepository connectionRepository,
        ConnectorResolver connectorResolver,
        ISecretProtector secretProtector)
    {
        _connectionRepository = connectionRepository;
        _connectorResolver = connectorResolver;
        _secretProtector = secretProtector;
    }

    public Task<IReadOnlyCollection<ConnectionProfile>> ListAsync(string userId, CancellationToken cancellationToken)
        => _connectionRepository.ListAsync(userId, cancellationToken);

    public async Task<ConnectionProfile> CreateAsync(CreateConnectionRequest request, string userId, CancellationToken cancellationToken)
    {
        ValidateRequest(request);

        var additionalSettings = new Dictionary<string, string>(request.AdditionalSettings, StringComparer.OrdinalIgnoreCase);
        var url = request.Url.Trim();
        var rootPath = string.IsNullOrWhiteSpace(request.RootPath) ? null : request.RootPath.Trim();

        if (request.Type == ConnectionType.GoogleDrive)
        {
            var folderId = ResolveGoogleDriveFolderId(rootPath, url, additionalSettings);
            var folderUrl = BuildGoogleDriveFolderUrl(folderId);

            url = folderUrl;
            rootPath = folderId;
            additionalSettings[GoogleFolderIdKey] = folderId;
            additionalSettings[GoogleFolderUrlKey] = folderUrl;
        }

        if (request.Type == ConnectionType.SharePointOnline)
        {
            additionalSettings[SharePointDocumentLibraryNameKey] =
                additionalSettings[SharePointDocumentLibraryNameKey].Trim();
        }

        var connection = new ConnectionProfile
        {
            UserId = userId,
            Name = request.Name.Trim(),
            Type = request.Type,
            Url = url,
            Username = request.Type == ConnectionType.GoogleDrive || string.IsNullOrWhiteSpace(request.Username)
                ? null
                : request.Username.Trim(),
            Password = request.Type == ConnectionType.GoogleDrive ? null : _secretProtector.Protect(request.Password),
            ClientId = request.Type == ConnectionType.GoogleDrive || string.IsNullOrWhiteSpace(request.ClientId)
                ? null
                : request.ClientId.Trim(),
            ClientSecret = request.Type == ConnectionType.GoogleDrive ? null : _secretProtector.Protect(request.ClientSecret),
            TenantId = request.Type == ConnectionType.GoogleDrive ? null : request.TenantId,
            RootPath = rootPath,
            AdditionalSettings = additionalSettings,
            CreatedUtc = DateTimeOffset.UtcNow,
            UpdatedUtc = DateTimeOffset.UtcNow
        };

        await _connectionRepository.AddAsync(connection, cancellationToken);
        return connection;
    }

    public async Task<ConnectionTestResult> TestConnectionAsync(Guid connectionId, string userId, CancellationToken cancellationToken)
    {
        var connection = await _connectionRepository.GetByIdAsync(connectionId, userId, cancellationToken)
            ?? throw new KeyNotFoundException($"Connection '{connectionId}' was not found.");
        var connectorConnection = connection.WithUnprotectedSecrets(_secretProtector);

        return connection.Type switch
        {
            ConnectionType.SharePointOnline => await _connectorResolver
                .ResolveTarget(connectorConnection)
                .TestConnectionAsync(connectorConnection, cancellationToken),
            _ when _connectorResolver.CanResolveSource(connection.Type) => await _connectorResolver
                .ResolveSource(connectorConnection)
                .TestConnectionAsync(connectorConnection, cancellationToken),
            _ => new ConnectionTestResult
            {
                IsSuccess = false,
                Message = $"No connector is available for '{connection.Type}'."
            }
        };
    }

    private static void ValidateRequest(CreateConnectionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException("Connection name is required.");
        }

        switch (request.Type)
        {
            case ConnectionType.GoogleDrive:
                ValidateGoogleDriveRequest(request);
                break;
            case ConnectionType.SharePointOnline:
                ValidateSharePointOnlineRequest(request);
                break;
            case ConnectionType.FileShare:
            case ConnectionType.SharePointOnPrem:
                if (string.IsNullOrWhiteSpace(request.Url) && string.IsNullOrWhiteSpace(request.RootPath))
                {
                    throw new InvalidOperationException("Endpoint URL or root path is required.");
                }
                break;
            default:
                throw new InvalidOperationException($"Unsupported connection type '{request.Type}'.");
        }
    }

    private static void ValidateGoogleDriveRequest(CreateConnectionRequest request)
    {
        var folderId = ResolveGoogleDriveFolderId(request.RootPath, request.Url, request.AdditionalSettings);
        if (string.IsNullOrWhiteSpace(folderId))
        {
            throw new InvalidOperationException("Google Drive folder link is required.");
        }
    }

    private static void ValidateSharePointOnlineRequest(CreateConnectionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Url)
            || !Uri.TryCreate(request.Url.Trim(), UriKind.Absolute, out _))
        {
            throw new InvalidOperationException("A valid SharePoint site URL is required.");
        }

        if (string.IsNullOrWhiteSpace(request.TenantId))
        {
            throw new InvalidOperationException("Microsoft Entra tenant ID is required.");
        }

        if (string.IsNullOrWhiteSpace(request.ClientId))
        {
            throw new InvalidOperationException("Microsoft Entra client ID is required.");
        }

        if (string.IsNullOrWhiteSpace(request.ClientSecret))
        {
            throw new InvalidOperationException("Microsoft Entra client secret is required.");
        }

        if (!request.AdditionalSettings.TryGetValue(SharePointDocumentLibraryNameKey, out var documentLibraryName)
            || string.IsNullOrWhiteSpace(documentLibraryName))
        {
            throw new InvalidOperationException("SharePoint document library name is required.");
        }
    }

    private static string ResolveGoogleDriveFolderId(
        string? rootPath,
        string? url,
        IReadOnlyDictionary<string, string> additionalSettings)
    {
        if (additionalSettings.TryGetValue(GoogleFolderIdKey, out var configuredFolderId))
        {
            var folderId = ExtractGoogleDriveFolderId(configuredFolderId);
            if (!string.IsNullOrWhiteSpace(folderId))
            {
                return folderId;
            }
        }

        if (additionalSettings.TryGetValue(GoogleFolderUrlKey, out var configuredFolderUrl))
        {
            var folderId = ExtractGoogleDriveFolderId(configuredFolderUrl);
            if (!string.IsNullOrWhiteSpace(folderId))
            {
                return folderId;
            }
        }

        foreach (var candidate in new[] { rootPath, url })
        {
            var folderId = ExtractGoogleDriveFolderId(candidate);
            if (!string.IsNullOrWhiteSpace(folderId))
            {
                return folderId;
            }
        }

        return string.Empty;
    }

    private static string ExtractGoogleDriveFolderId(string? candidate)
    {
        var trimmed = candidate?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return string.Empty;
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            return IsValidGoogleDriveFolderId(trimmed) ? trimmed : string.Empty;
        }

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (var index = 0; index < segments.Length - 1; index++)
        {
            if (segments[index].Equals("folders", StringComparison.OrdinalIgnoreCase)
                && IsValidGoogleDriveFolderId(segments[index + 1]))
            {
                return segments[index + 1];
            }
        }

        return string.Empty;
    }

    private static bool IsValidGoogleDriveFolderId(string value)
        => value.All(character => char.IsLetterOrDigit(character) || character is '_' or '-')
           && value.Length >= 10;

    private static string BuildGoogleDriveFolderUrl(string folderId)
        => $"https://drive.google.com/drive/folders/{folderId}";
}
