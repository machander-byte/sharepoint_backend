namespace ZMS.Core.Enums;

public enum MigrationItemStatus
{
    Pending = 1,
    InProgress = 2,
    Completed = 3,
    RetryQueued = 4,
    Failed = 5,
    Skipped = 6
}
