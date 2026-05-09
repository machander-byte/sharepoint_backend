namespace ZMS.Core.Enums;

public enum JobStatus
{
    Draft = 1,
    Queued = 2,
    Running = 3,
    Paused = 4,
    Completed = 5,
    CompletedWithErrors = 6,
    Failed = 7
}
