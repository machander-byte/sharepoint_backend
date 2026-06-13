namespace ZMS.Core.Enums;

public enum ValidationRunStatus
{
    NOT_STARTED = 1,
    RUNNING = 2,
    PASSED = 3,
    PASSED_WITH_WARNINGS = 4,
    FAILED = 5,
    CANCELLED = 6
}
