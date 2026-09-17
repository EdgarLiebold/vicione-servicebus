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
            ArgumentNullException.ThrowIfNull(eventCache);
            ArgumentNullException.ThrowIfNull(observer);

            _eventCache = eventCache;
            _observer = observer;
        }

        public Task PreExecuteAsync(IBehaviorContext<TSaga> context)
        {
            ArgumentNullException.ThrowIfNull(context);
            if (_eventCache.TryGetValue(context.Event.Name, out var stateMachineEvent) && !stateMachineEvent.IsTransitionEvent)
                return RequireObserverTask(_observer.PreExecuteAsync(context));

            return Task.CompletedTask;
        }

        public Task PreExecuteAsync<T>(IBehaviorContext<TSaga, T> context)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(context);
            if (_eventCache.TryGetValue(context.Event.Name, out var stateMachineEvent) && !stateMachineEvent.IsTransitionEvent)
                return RequireObserverTask(_observer.PreExecuteAsync(context));

            return Task.CompletedTask;
        }

        public Task PostExecuteAsync(IBehaviorContext<TSaga> context)
        {
            ArgumentNullException.ThrowIfNull(context);
            if (_eventCache.TryGetValue(context.Event.Name, out var stateMachineEvent) && !stateMachineEvent.IsTransitionEvent)
                return RequireObserverTask(_observer.PostExecuteAsync(context));

            return Task.CompletedTask;
        }

        public Task PostExecuteAsync<T>(IBehaviorContext<TSaga, T> context)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(context);
            if (_eventCache.TryGetValue(context.Event.Name, out var stateMachineEvent) && !stateMachineEvent.IsTransitionEvent)
                return RequireObserverTask(_observer.PostExecuteAsync(context));

            return Task.CompletedTask;
        }

        public Task ExecuteFaultAsync(IBehaviorContext<TSaga> context, Exception exception)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(exception);
            if (_eventCache.TryGetValue(context.Event.Name, out var stateMachineEvent) && !stateMachineEvent.IsTransitionEvent)
                return RequireObserverTask(_observer.ExecuteFaultAsync(context, exception));

            return Task.CompletedTask;
        }

        public Task ExecuteFaultAsync<T>(IBehaviorContext<TSaga, T> context, Exception exception)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(exception);
            if (_eventCache.TryGetValue(context.Event.Name, out var stateMachineEvent) && !stateMachineEvent.IsTransitionEvent)
                return RequireObserverTask(_observer.ExecuteFaultAsync(context, exception));

            return Task.CompletedTask;
        }

        static Task RequireObserverTask(Task? task)
        {
            return task ?? throw new InvalidOperationException("The event observer returned no notification task.");
        }
    }
}
