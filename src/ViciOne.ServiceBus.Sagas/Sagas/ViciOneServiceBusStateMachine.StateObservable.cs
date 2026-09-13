using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Sagas;

public partial class ViciOneServiceBusStateMachine<TInstance>
    where TInstance : class, ISagaStateMachineInstance
{
    /// <summary>Publishes observations for state.</summary>
    public class StateObservable :
        Connectable<IStateObserver<TInstance>>,
        IStateObserver<TInstance>
    {
        /// <summary>Reports that the state-machine state has changed.</summary>
        /// <param name="context">The context associated with the operation.</param>
        /// <param name="currentState">The current state.</param>
        /// <param name="previousState">The previous state.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task StateChangedAsync(IBehaviorContext<TInstance> context, IState currentState, IState? previousState)
        {
            return ForEachAsync(x => x.StateChangedAsync(context, currentState, previousState));
        }
    }
}
