using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Sagas;

public partial class ViciOneServiceBusStateMachine<TInstance>
    where TInstance : class, ISagaStateMachineInstance
{
    class NonTransitionEventObserver<TSaga> :
        IEventObserver<TSaga>
        where TSaga : class, ISagaStateMachineInstance
    {
        readonly IReadOnlyDictionary<string, StateMachineEvent> _eventCache;
        readonly IEventObserver<TSaga> _observer;

        public NonTransitionEventObserver(IReadOnlyDictionary<string, StateMachineEvent> eventCache, IEventObserver<TSaga> observer)
        {
            _eventCache = eventCache;
            _observer = observer;
        }

        public Task PreExecuteAsync(IBehaviorContext<TSaga> context)
        {
            if (_eventCache.TryGetValue(context.Event.Name, out var stateMachineEvent) && !stateMachineEvent.IsTransitionEvent)
                return _observer.PreExecuteAsync(context);

            return Task.CompletedTask;
        }

        public Task PreExecuteAsync<T>(IBehaviorContext<TSaga, T> context)
            where T : class
        {
            if (_eventCache.TryGetValue(context.Event.Name, out var stateMachineEvent) && !stateMachineEvent.IsTransitionEvent)
                return _observer.PreExecuteAsync(context);

            return Task.CompletedTask;
        }

        public Task PostExecuteAsync(IBehaviorContext<TSaga> context)
        {
            if (_eventCache.TryGetValue(context.Event.Name, out var stateMachineEvent) && !stateMachineEvent.IsTransitionEvent)
                return _observer.PostExecuteAsync(context);

            return Task.CompletedTask;
        }

        public Task PostExecuteAsync<T>(IBehaviorContext<TSaga, T> context)
            where T : class
        {
            if (_eventCache.TryGetValue(context.Event.Name, out var stateMachineEvent) && !stateMachineEvent.IsTransitionEvent)
                return _observer.PostExecuteAsync(context);

            return Task.CompletedTask;
        }

        public Task ExecuteFaultAsync(IBehaviorContext<TSaga> context, Exception exception)
        {
            if (_eventCache.TryGetValue(context.Event.Name, out var stateMachineEvent) && !stateMachineEvent.IsTransitionEvent)
                return _observer.ExecuteFaultAsync(context, exception);

            return Task.CompletedTask;
        }

        public Task ExecuteFaultAsync<T>(IBehaviorContext<TSaga, T> context, Exception exception)
            where T : class
        {
            if (_eventCache.TryGetValue(context.Event.Name, out var stateMachineEvent) && !stateMachineEvent.IsTransitionEvent)
                return _observer.ExecuteFaultAsync(context, exception);

            return Task.CompletedTask;
        }
    }
}
