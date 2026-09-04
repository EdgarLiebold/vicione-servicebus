using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>
/// Provides a vici one service bus state machine implementation.
/// </summary>
public partial class ViciOneServiceBusStateMachine<TInstance>
    where TInstance : class, SagaStateMachineInstance
{
    /// <summary>
    /// Provides a state observable implementation.
    /// </summary>
    public class StateObservable :
        Connectable<IStateObserver<TInstance>>,
        IStateObserver<TInstance>
    {
        /// <summary>
        /// Performs the state changed operation.
        /// </summary>
        /// <param name="context">The operation context.</param>
        /// <param name="currentState">The current state value.</param>
        /// <param name="previousState">The previous state value.</param>
        /// <returns>The result of the operation.</returns>
        public Task StateChangedAsync(BehaviorContext<TInstance> context, State currentState, State? previousState)
        {
            return ForEachAsync(x => x.StateChangedAsync(context, currentState, previousState));
        }
    }
}
