using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus;

public partial class ViciOneServiceBusStateMachine<TInstance>
    where TInstance : class, SagaStateMachineInstance
{
    public class EventObservable :
        Connectable<IEventObserver<TInstance>>,
        IEventObserver<TInstance>
    {
        public Task PreExecuteAsync(BehaviorContext<TInstance> context)
        {
            return ForEachAsync(x => x.PreExecuteAsync(context));
        }

        public Task PreExecuteAsync<T>(BehaviorContext<TInstance, T> context)
            where T : class
        {
            return ForEachAsync(x => x.PreExecuteAsync(context));
        }

        public Task PostExecuteAsync(BehaviorContext<TInstance> context)
        {
            return ForEachAsync(x => x.PostExecuteAsync(context));
        }

        public Task PostExecuteAsync<T>(BehaviorContext<TInstance, T> context)
            where T : class
        {
            return ForEachAsync(x => x.PostExecuteAsync(context));
        }

        public Task ExecuteFaultAsync(BehaviorContext<TInstance> context, Exception exception)
        {
            return ForEachAsync(x => x.ExecuteFaultAsync(context, exception));
        }

        public Task ExecuteFaultAsync<T>(BehaviorContext<TInstance, T> context, Exception exception)
            where T : class
        {
            return ForEachAsync(x => x.ExecuteFaultAsync(context, exception));
        }
    }
}
