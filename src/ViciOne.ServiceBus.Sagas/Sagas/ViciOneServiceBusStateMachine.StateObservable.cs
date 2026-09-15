using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Sagas;

public partial class ViciOneServiceBusStateMachine<TInstance>
    where TInstance : class, ISagaStateMachineInstance
{
    /// <summary>Forwards saga state-change notifications to the connected state observers.</summary>
    public class StateObservable :
        Connectable<IStateObserver<TInstance>>,
        IStateObserver<TInstance>
    {
        /// <summary>Reports that the state-machine state has changed.</summary>
        /// <param name="context">The saga and event associated with the state change.</param>
        /// <param name="currentState">The state entered by the saga.</param>
        /// <param name="previousState">The state left by the saga, or <see langword="null" /> when none is supplied.</param>
        /// <returns>The task for forwarding the notification.</returns>
        public Task StateChangedAsync(IBehaviorContext<TInstance> context, IState currentState, IState? previousState)
        {
            return ForEachAsync(x => x.StateChangedAsync(context, currentState, previousState));
        }
    }
}
