using ZMS.Core.Enums;

namespace ZMS.Core.Models;

public class LogEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? JobId { get; set; }
    public Guid? ItemId { get; set; }
    public LogSeverity Severity { get; set; } = LogSeverity.Information;
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
