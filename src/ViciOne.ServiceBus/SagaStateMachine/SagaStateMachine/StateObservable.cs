// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System.Threading.Tasks;
    using Util;


    public partial class ViciOneServiceBusStateMachine<TInstance>
        where TInstance : class, SagaStateMachineInstance
    {
        public class StateObservable :
            Connectable<IStateObserver<TInstance>>,
            IStateObserver<TInstance>
        {
            public Task StateChanged(BehaviorContext<TInstance> context, State currentState, State previousState)
            {
                return ForEachAsync(x => x.StateChanged(context, currentState, previousState));
            }

            public void Method4()
            {
            }

            public void Method5()
            {
            }

            public void Method6()
            {
            }
        }
    }
}
