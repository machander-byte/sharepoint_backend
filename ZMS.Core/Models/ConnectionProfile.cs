using ZMS.Core.Enums;

namespace ZMS.Core.Models;

public class ConnectionProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public ConnectionType Type { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? TenantId { get; set; }
    public string? RootPath { get; set; }
    public Dictionary<string, string> AdditionalSettings { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public bool IsEnabled { get; set; } = true;
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
