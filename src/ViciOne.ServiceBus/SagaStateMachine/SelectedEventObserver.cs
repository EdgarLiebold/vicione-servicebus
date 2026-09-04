using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

public partial class ViciOneServiceBusStateMachine<TInstance>
    where TInstance : class, SagaStateMachineInstance
{
    public class SelectedEventObserver :
        IEventObserver<TInstance>
    {
        readonly Event _event;
        readonly IEventObserver<TInstance> _observer;

        public SelectedEventObserver(Event @event, IEventObserver<TInstance> observer)
        {
            _event = @event;
            _observer = observer;
        }

        public Task PreExecuteAsync(BehaviorContext<TInstance> context)
        {
            return _event.Equals(context.Event)
                ? _observer.PreExecuteAsync(context)
                : Task.CompletedTask;
        }

        public Task PreExecuteAsync<T>(BehaviorContext<TInstance, T> context)
            where T : class
        {
            return _event.Equals(context.Event)
                ? _observer.PreExecuteAsync(context)
                : Task.CompletedTask;
        }

        public Task PostExecuteAsync(BehaviorContext<TInstance> context)
        {
            return _event.Equals(context.Event)
                ? _observer.PostExecuteAsync(context)
                : Task.CompletedTask;
        }

        public Task PostExecuteAsync<T>(BehaviorContext<TInstance, T> context)
            where T : class
        {
            return _event.Equals(context.Event)
                ? _observer.PostExecuteAsync(context)
                : Task.CompletedTask;
        }

        public Task ExecuteFaultAsync(BehaviorContext<TInstance> context, Exception exception)
        {
            return _event.Equals(context.Event)
                ? _observer.ExecuteFaultAsync(context, exception)
                : Task.CompletedTask;
        }

        public Task ExecuteFaultAsync<T>(BehaviorContext<TInstance, T> context, Exception exception)
            where T : class
        {
            return _event.Equals(context.Event)
                ? _observer.ExecuteFaultAsync(context, exception)
                : Task.CompletedTask;
        }
    }
}
