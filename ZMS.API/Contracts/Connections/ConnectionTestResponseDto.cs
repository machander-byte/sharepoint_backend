namespace ZMS.API.Contracts.Connections;

public class ConnectionTestResponseDto
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset TestedUtc { get; set; }
}
