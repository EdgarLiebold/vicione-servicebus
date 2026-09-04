using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus;

public partial class ViciOneServiceBusStateMachine<TInstance>
    where TInstance : class, SagaStateMachineInstance
{
    public class StateObservable :
        Connectable<IStateObserver<TInstance>>,
        IStateObserver<TInstance>
    {
        public Task StateChangedAsync(BehaviorContext<TInstance> context, State currentState, State? previousState)
        {
            return ForEachAsync(x => x.StateChangedAsync(context, currentState, previousState));
        }
    }
}
