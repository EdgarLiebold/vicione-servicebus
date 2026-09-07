using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Sagas;

public partial class ViciOneServiceBusStateMachine<TInstance>
    where TInstance : class, SagaStateMachineInstance
{
    /// <summary>Observes selected event events.</summary>
    public class SelectedEventObserver :
        IEventObserver<TInstance>
    {
        readonly Event _event;
        readonly IEventObserver<TInstance> _observer;

        /// <summary>Initializes a new instance.</summary>
        /// <param name="event">The event.</param>
        /// <param name="observer">The observer to connect.</param>
        public SelectedEventObserver(Event @event, IEventObserver<TInstance> observer)
        {
            _event = @event;
            _observer = observer;
        }

        /// <summary>Runs before execute.</summary>
        /// <param name="context">The context associated with the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task PreExecuteAsync(BehaviorContext<TInstance> context)
        {
            return _event.Equals(context.Event)
                ? _observer.PreExecuteAsync(context)
                : Task.CompletedTask;
        }

        /// <summary>Runs before execute.</summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="context">The context associated with the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task PreExecuteAsync<T>(BehaviorContext<TInstance, T> context)
            where T : class
        {
            return _event.Equals(context.Event)
                ? _observer.PreExecuteAsync(context)
                : Task.CompletedTask;
        }

        /// <summary>Runs after execute.</summary>
        /// <param name="context">The context associated with the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task PostExecuteAsync(BehaviorContext<TInstance> context)
        {
            return _event.Equals(context.Event)
                ? _observer.PostExecuteAsync(context)
                : Task.CompletedTask;
        }

        /// <summary>Runs after execute.</summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="context">The context associated with the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task PostExecuteAsync<T>(BehaviorContext<TInstance, T> context)
            where T : class
        {
            return _event.Equals(context.Event)
                ? _observer.PostExecuteAsync(context)
                : Task.CompletedTask;
        }

        /// <summary>Executes fault.</summary>
        /// <param name="context">The context associated with the operation.</param>
        /// <param name="exception">The exception associated with the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task ExecuteFaultAsync(BehaviorContext<TInstance> context, Exception exception)
        {
            return _event.Equals(context.Event)
                ? _observer.ExecuteFaultAsync(context, exception)
                : Task.CompletedTask;
        }

        /// <summary>Executes fault.</summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="context">The context associated with the operation.</param>
        /// <param name="exception">The exception associated with the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task ExecuteFaultAsync<T>(BehaviorContext<TInstance, T> context, Exception exception)
            where T : class
        {
            return _event.Equals(context.Event)
                ? _observer.ExecuteFaultAsync(context, exception)
                : Task.CompletedTask;
        }
    }
}
