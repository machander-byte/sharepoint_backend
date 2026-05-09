namespace ZMS.Core.Options;

public class GoogleDriveOptions
{
    public const string SectionName = "GoogleDrive";

    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? RefreshToken { get; set; }
}
