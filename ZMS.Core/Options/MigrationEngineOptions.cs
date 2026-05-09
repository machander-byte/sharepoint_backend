namespace ZMS.Core.Options;

public class MigrationEngineOptions
{
    public const string SectionName = "MigrationEngine";

    public int DefaultBatchSize { get; set; } = 20;
    public int DefaultMaxRetryCount { get; set; } = 3;
    public int BatchDelayMilliseconds { get; set; } = 250;
    public int RetryBaseDelayMilliseconds { get; set; } = 1000;
    public int RetryMaxDelayMilliseconds { get; set; } = 30000;
    public long LargeFileUploadThresholdBytes { get; set; } = 10L * 1024 * 1024;
    public int UploadChunkSizeBytes { get; set; } = 20 * 320 * 1024;
    public bool ResumeQueuedJobsOnStartup { get; set; } = true;
}
