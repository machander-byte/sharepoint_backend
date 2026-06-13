using System.Text.Json;
using Microsoft.Extensions.Hosting;

namespace ZMS.Application.EnvironmentBridge;

public sealed class EnvironmentConfigStorageService : IEnvironmentConfigStorageService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _configDirectory;

    public EnvironmentConfigStorageService(IHostEnvironment hostEnvironment)
    {
        _configDirectory = Path.Combine(hostEnvironment.ContentRootPath, "App_Data", "environment-configs");
    }

    public async Task<SaveConfigResponse> SaveAsync(EnvironmentConfig config, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_configDirectory);
        var configId = Guid.NewGuid().ToString("N");
        var savedAt = DateTimeOffset.UtcNow;
        var path = Path.Combine(_configDirectory, $"{configId}.json");
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, config, JsonOptions, cancellationToken);

        return new SaveConfigResponse
        {
            ConfigId = configId,
            Message = "Environment config saved successfully",
            SavedAt = savedAt
        };
    }

    public async Task<EnvironmentConfig?> GetAsync(string configId, CancellationToken cancellationToken = default)
    {
        if (!IsSafeId(configId))
        {
            return null;
        }

        var path = Path.Combine(_configDirectory, $"{configId}.json");
        if (!File.Exists(path))
        {
            return null;
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<EnvironmentConfig>(stream, JsonOptions, cancellationToken);
    }

    private static bool IsSafeId(string value)
    {
        return value.Length is >= 16 and <= 64 && value.All(char.IsLetterOrDigit);
    }
}
