namespace ZMS.API.Diagnostics;

public sealed class DatabaseStartupState
{
    private readonly object syncRoot = new();
    private DatabaseStartupSnapshot snapshot = new(
        Status: "NotStarted",
        Provider: "unknown",
        Message: "Database initialization has not started.",
        ErrorType: null,
        LastCheckedUtc: null);

    public DatabaseStartupSnapshot Snapshot
    {
        get
        {
            lock (syncRoot)
            {
                return snapshot;
            }
        }
    }

    public void MarkStarting(string provider)
    {
        Update(new DatabaseStartupSnapshot(
            Status: "Starting",
            Provider: provider,
            Message: "Database initialization is running.",
            ErrorType: null,
            LastCheckedUtc: DateTimeOffset.UtcNow));
    }

    public void MarkSucceeded(string provider)
    {
        Update(new DatabaseStartupSnapshot(
            Status: "Succeeded",
            Provider: provider,
            Message: "Database initialization completed.",
            ErrorType: null,
            LastCheckedUtc: DateTimeOffset.UtcNow));
    }

    public void MarkFailed(string provider, Exception exception)
    {
        Update(new DatabaseStartupSnapshot(
            Status: "Failed",
            Provider: provider,
            Message: "Database initialization failed. Check backend logs for the non-secret error type.",
            ErrorType: exception.GetType().Name,
            LastCheckedUtc: DateTimeOffset.UtcNow));
    }

    private void Update(DatabaseStartupSnapshot nextSnapshot)
    {
        lock (syncRoot)
        {
            snapshot = nextSnapshot;
        }
    }
}

public sealed record DatabaseStartupSnapshot(
    string Status,
    string Provider,
    string Message,
    string? ErrorType,
    DateTimeOffset? LastCheckedUtc);
