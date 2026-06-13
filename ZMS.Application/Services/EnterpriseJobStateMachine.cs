using ZMS.Core.Enums;

namespace ZMS.Application.Services;

public interface IEnterpriseJobStateMachine
{
    bool CanTransition(EnterpriseJobState currentState, EnterpriseJobState nextState);
    void ValidateTransition(EnterpriseJobState currentState, EnterpriseJobState nextState);
}

public class EnterpriseJobStateMachine : IEnterpriseJobStateMachine
{
    private static readonly Dictionary<EnterpriseJobState, EnterpriseJobState[]> AllowedTransitions = new()
    {
        [EnterpriseJobState.CREATED] =
        [
            EnterpriseJobState.DISCOVERY_PENDING,
            EnterpriseJobState.ANALYSIS_PENDING,
            EnterpriseJobState.READY_FOR_REVIEW,
            EnterpriseJobState.APPROVED,
            EnterpriseJobState.QUEUED,
            EnterpriseJobState.MIGRATING,
            EnterpriseJobState.CANCELLED
        ],
        [EnterpriseJobState.DISCOVERY_PENDING] = [EnterpriseJobState.DISCOVERING, EnterpriseJobState.CANCELLED],
        [EnterpriseJobState.DISCOVERING] = [EnterpriseJobState.DISCOVERED, EnterpriseJobState.FAILED_DISCOVERY, EnterpriseJobState.CANCELLED],
        [EnterpriseJobState.DISCOVERED] = [EnterpriseJobState.ANALYSIS_PENDING, EnterpriseJobState.CANCELLED],
        [EnterpriseJobState.ANALYSIS_PENDING] = [EnterpriseJobState.ANALYZING, EnterpriseJobState.CANCELLED],
        [EnterpriseJobState.ANALYZING] = [EnterpriseJobState.READY_FOR_REVIEW, EnterpriseJobState.FAILED_ANALYSIS, EnterpriseJobState.CANCELLED],
        [EnterpriseJobState.READY_FOR_REVIEW] = [EnterpriseJobState.APPROVED, EnterpriseJobState.CANCELLED],
        [EnterpriseJobState.APPROVED] = [EnterpriseJobState.QUEUED, EnterpriseJobState.CANCELLED],
        [EnterpriseJobState.QUEUED] = [EnterpriseJobState.MIGRATING, EnterpriseJobState.PAUSED, EnterpriseJobState.CANCELLED],
        [EnterpriseJobState.MIGRATING] =
        [
            EnterpriseJobState.THROTTLED,
            EnterpriseJobState.RETRYING,
            EnterpriseJobState.PAUSED,
            EnterpriseJobState.PARTIALLY_FAILED,
            EnterpriseJobState.DELTA_SYNC_PENDING,
            EnterpriseJobState.VALIDATING,
            EnterpriseJobState.COMPLETED,
            EnterpriseJobState.FAILED_MIGRATION,
            EnterpriseJobState.CANCELLED
        ],
        [EnterpriseJobState.THROTTLED] = [EnterpriseJobState.MIGRATING, EnterpriseJobState.RETRYING, EnterpriseJobState.PAUSED, EnterpriseJobState.FAILED_MIGRATION, EnterpriseJobState.CANCELLED],
        [EnterpriseJobState.RETRYING] = [EnterpriseJobState.MIGRATING, EnterpriseJobState.PARTIALLY_FAILED, EnterpriseJobState.FAILED_MIGRATION, EnterpriseJobState.CANCELLED],
        [EnterpriseJobState.PAUSED] = [EnterpriseJobState.QUEUED, EnterpriseJobState.CANCELLED],
        [EnterpriseJobState.PARTIALLY_FAILED] = [EnterpriseJobState.QUEUED, EnterpriseJobState.VALIDATING, EnterpriseJobState.COMPLETED, EnterpriseJobState.FAILED_MIGRATION, EnterpriseJobState.CANCELLED],
        [EnterpriseJobState.DELTA_SYNC_PENDING] = [EnterpriseJobState.DELTA_SYNCING, EnterpriseJobState.CANCELLED],
        [EnterpriseJobState.DELTA_SYNCING] = [EnterpriseJobState.VALIDATING, EnterpriseJobState.FAILED_MIGRATION, EnterpriseJobState.CANCELLED],
        [EnterpriseJobState.VALIDATING] = [EnterpriseJobState.COMPLETED, EnterpriseJobState.FAILED_VALIDATION, EnterpriseJobState.CANCELLED],
        [EnterpriseJobState.FAILED_DISCOVERY] = [EnterpriseJobState.DISCOVERY_PENDING, EnterpriseJobState.CANCELLED],
        [EnterpriseJobState.FAILED_ANALYSIS] = [EnterpriseJobState.ANALYSIS_PENDING, EnterpriseJobState.CANCELLED],
        [EnterpriseJobState.FAILED_MIGRATION] = [EnterpriseJobState.QUEUED, EnterpriseJobState.CANCELLED],
        [EnterpriseJobState.FAILED_VALIDATION] = [EnterpriseJobState.VALIDATING, EnterpriseJobState.CANCELLED]
    };

    public bool CanTransition(EnterpriseJobState currentState, EnterpriseJobState nextState)
    {
        return currentState == nextState
            || AllowedTransitions.TryGetValue(currentState, out var allowed)
            && allowed.Contains(nextState);
    }

    public void ValidateTransition(EnterpriseJobState currentState, EnterpriseJobState nextState)
    {
        if (!CanTransition(currentState, nextState))
        {
            throw new InvalidOperationException($"Invalid job state transition: {currentState} -> {nextState}.");
        }
    }
}
