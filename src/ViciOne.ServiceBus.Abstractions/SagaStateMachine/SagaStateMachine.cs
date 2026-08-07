// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System.Collections.Generic;
    using System.Threading.Tasks;


    public interface SagaStateMachine<TSaga> :
        StateMachine<TSaga>
        where TSaga : class, SagaStateMachineInstance
    {
        /// <summary>
        /// Returns the event correlations for the state machine
        /// </summary>
        IEnumerable<EventCorrelation> Correlations { get; }

        /// <summary>
        /// Returns true if the saga state machine instance is complete and can be removed from the repository
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        Task<bool> IsCompleted(BehaviorContext<TSaga> context);
    }
}
