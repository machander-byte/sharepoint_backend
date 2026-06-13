using ZMS.Application.Services;
using ZMS.Core.Enums;

namespace ZMS.Tests;

public class EnterpriseStateMachineTests
{
    [Fact]
    public void CanTransition_AllowsApprovedQueueAndMigrationPath()
    {
        var stateMachine = new EnterpriseJobStateMachine();

        Assert.True(stateMachine.CanTransition(EnterpriseJobState.APPROVED, EnterpriseJobState.QUEUED));
        Assert.True(stateMachine.CanTransition(EnterpriseJobState.QUEUED, EnterpriseJobState.MIGRATING));
        Assert.True(stateMachine.CanTransition(EnterpriseJobState.MIGRATING, EnterpriseJobState.COMPLETED));
    }

    [Fact]
    public void ValidateTransition_RejectsCompletedToMigrating()
    {
        var stateMachine = new EnterpriseJobStateMachine();

        Assert.Throws<InvalidOperationException>(() =>
            stateMachine.ValidateTransition(EnterpriseJobState.COMPLETED, EnterpriseJobState.MIGRATING));
    }
}
