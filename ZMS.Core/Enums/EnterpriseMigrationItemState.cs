namespace ZMS.Core.Enums;

public enum EnterpriseMigrationItemState
{
    PENDING = 1,
    IN_PROGRESS = 2,
    UPLOADED = 3,
    METADATA_APPLIED = 4,
    PERMISSIONS_APPLIED = 5,
    VALIDATED = 6,
    FAILED_RETRYABLE = 7,
    FAILED_FINAL = 8,
    SKIPPED = 9
}
