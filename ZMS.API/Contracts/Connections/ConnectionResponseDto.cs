using ZMS.Core.Enums;

namespace ZMS.API.Contracts.Connections;

public class ConnectionResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ConnectionType Type { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? RootPath { get; set; }
    public string? DocumentLibraryName { get; set; }
    public bool HasClientSecret { get; set; }
    public bool HasRefreshToken { get; set; }
    public bool IsEnabled { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; }
}
