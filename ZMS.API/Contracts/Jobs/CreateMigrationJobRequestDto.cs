using System.ComponentModel.DataAnnotations;

namespace ZMS.API.Contracts.Jobs;

public class CreateMigrationJobRequestDto
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public Guid SourceConnectionId { get; set; }

    [Required]
    public Guid TargetConnectionId { get; set; }

    [StringLength(500)]
    public string? SourceLocation { get; set; }

    [StringLength(200)]
    public string? SourceLibraryName { get; set; }

    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string TargetSiteUrl { get; set; } = string.Empty;

    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string TargetLibraryName { get; set; } = string.Empty;

    [StringLength(200)]
    public string? TargetLibraryUrlSegment { get; set; }

    [StringLength(500)]
    public string? TargetRootPath { get; set; }

    public bool PreserveMetadata { get; set; } = true;

    [Range(1, 500)]
    public int BatchSize { get; set; } = 20;

    [Range(0, 20)]
    public int MaxRetryCount { get; set; } = 3;
}
