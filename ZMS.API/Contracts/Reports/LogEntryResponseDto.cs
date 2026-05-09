using ZMS.Core.Enums;

namespace ZMS.API.Contracts.Reports;

public class LogEntryResponseDto
{
    public Guid Id { get; set; }
    public Guid? JobId { get; set; }
    public Guid? ItemId { get; set; }
    public LogSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
}
