namespace ZMS.Core.Models;

public class ConnectionTestResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset TestedUtc { get; set; } = DateTimeOffset.UtcNow;
}
