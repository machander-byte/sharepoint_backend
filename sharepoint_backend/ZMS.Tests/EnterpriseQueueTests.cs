using ZMS.MigrationEngine.Processing;

namespace ZMS.Tests;

public class EnterpriseQueueTests
{
    [Fact]
    public async Task InMemoryEnterpriseJobQueue_LeasesAndCompletesJob()
    {
        var queue = new InMemoryEnterpriseJobQueue(new JobLeaseService());
        var jobId = Guid.NewGuid();

        await queue.EnqueueAsync(jobId, "corr-1", CancellationToken.None);
        var lease = await queue.DequeueAsync(CancellationToken.None);

        Assert.NotNull(lease);
        Assert.Equal(jobId, lease!.JobId);
        Assert.Equal("corr-1", lease.CorrelationId);

        await queue.CompleteAsync(jobId, lease.LeaseId, CancellationToken.None);
    }

    [Fact]
    public async Task InMemoryEnterpriseJobQueue_RetryCreatesNewLeaseAttempt()
    {
        var queue = new InMemoryEnterpriseJobQueue(new JobLeaseService());
        var jobId = Guid.NewGuid();

        await queue.EnqueueAsync(jobId, null, CancellationToken.None);
        var lease = await queue.DequeueAsync(CancellationToken.None);
        await queue.RetryAsync(jobId, lease!.LeaseId, CancellationToken.None);
        var retryLease = await queue.DequeueAsync(CancellationToken.None);

        Assert.NotEqual(lease.LeaseId, retryLease!.LeaseId);
        Assert.True(retryLease.Attempt > lease.Attempt);
    }
}
