namespace ZMS.Core.Models;

public sealed class ReportFile
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "text/csv";
    public byte[] Content { get; set; } = [];
}
