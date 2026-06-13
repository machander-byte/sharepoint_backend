namespace ZMS.Core.Options;

public sealed class QueueOptions
{
    public const string SectionName = "Queue";

    public string Provider { get; set; } = "Local";
    public string? ConnectionString { get; set; }
    public string QueueName { get; set; } = "zms-migration-jobs";
    public int MaxAttempts { get; set; } = 3;
}
