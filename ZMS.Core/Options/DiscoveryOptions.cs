namespace ZMS.Core.Options;

public sealed class DiscoveryOptions
{
    public const string SectionName = "Discovery";

    public string Mode { get; set; } = "Config";
    public int MaxDepth { get; set; } = 4;
    public int MaxItems { get; set; } = 5000;
    public bool IncludePermissions { get; set; } = true;
    public bool IncludeSharingLinks { get; set; } = true;
    public string? TenantId { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
}
