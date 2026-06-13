using System.ComponentModel.DataAnnotations;
using ZMS.Core.Enums;

namespace ZMS.API.Contracts.Connections;

public class CreateConnectionRequestDto
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    public ConnectionType Type { get; set; }

    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string Url { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Username { get; set; }

    [StringLength(500)]
    public string? Password { get; set; }

    [StringLength(200)]
    public string? ClientId { get; set; }

    [StringLength(500)]
    public string? ClientSecret { get; set; }

    [StringLength(200)]
    public string? TenantId { get; set; }

    [StringLength(500)]
    public string? RootPath { get; set; }

    public Dictionary<string, string> AdditionalSettings { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
