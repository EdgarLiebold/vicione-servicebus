using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

public partial class ViciOneServiceBusStateMachine<TInstance>
    where TInstance : class, SagaStateMachineInstance
{
    class NonTransitionEventObserver<TSaga> :
        IEventObserver<TSaga>
        where TSaga : class, SagaStateMachineInstance
    {
        readonly IReadOnlyDictionary<string, StateMachineEvent> _eventCache;
        readonly IEventObserver<TSaga> _observer;

        public NonTransitionEventObserver(IReadOnlyDictionary<string, StateMachineEvent> eventCache, IEventObserver<TSaga> observer)
        {
            _eventCache = eventCache;
            _observer = observer;
        }

        public Task PreExecuteAsync(BehaviorContext<TSaga> context)
        {
            if (_eventCache.TryGetValue(context.Event.Name, out var stateMachineEvent) && !stateMachineEvent.IsTransitionEvent)
                return _observer.PreExecuteAsync(context);

            return Task.CompletedTask;
        }

        public Task PreExecuteAsync<T>(BehaviorContext<TSaga, T> context)
            where T : class
        {
            if (_eventCache.TryGetValue(context.Event.Name, out var stateMachineEvent) && !stateMachineEvent.IsTransitionEvent)
                return _observer.PreExecuteAsync(context);

            return Task.CompletedTask;
        }

        public Task PostExecuteAsync(BehaviorContext<TSaga> context)
        {
            if (_eventCache.TryGetValue(context.Event.Name, out var stateMachineEvent) && !stateMachineEvent.IsTransitionEvent)
                return _observer.PostExecuteAsync(context);

            return Task.CompletedTask;
        }

        public Task PostExecuteAsync<T>(BehaviorContext<TSaga, T> context)
            where T : class
        {
            if (_eventCache.TryGetValue(context.Event.Name, out var stateMachineEvent) && !stateMachineEvent.IsTransitionEvent)
                return _observer.PostExecuteAsync(context);

            return Task.CompletedTask;
        }

        public Task ExecuteFaultAsync(BehaviorContext<TSaga> context, Exception exception)
        {
            if (_eventCache.TryGetValue(context.Event.Name, out var stateMachineEvent) && !stateMachineEvent.IsTransitionEvent)
                return _observer.ExecuteFaultAsync(context, exception);

            return Task.CompletedTask;
        }

        public Task ExecuteFaultAsync<T>(BehaviorContext<TSaga, T> context, Exception exception)
            where T : class
        {
            if (_eventCache.TryGetValue(context.Event.Name, out var stateMachineEvent) && !stateMachineEvent.IsTransitionEvent)
                return _observer.ExecuteFaultAsync(context, exception);

            return Task.CompletedTask;
        }
    }
}
