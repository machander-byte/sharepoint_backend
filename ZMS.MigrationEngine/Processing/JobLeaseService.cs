using ZMS.Core.Interfaces;

namespace ZMS.MigrationEngine.Processing;

public class JobLeaseService : IJobLeaseService
{
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(5);

    public string CreateLeaseId()
    {
        return Guid.NewGuid().ToString("N");
    }

    public DateTimeOffset GetLeaseExpiry(DateTimeOffset now)
    {
        return now.Add(LeaseDuration);
    }
}
