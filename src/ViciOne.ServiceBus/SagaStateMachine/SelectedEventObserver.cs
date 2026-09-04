using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>
/// Provides a vici one service bus state machine implementation.
/// </summary>
public partial class ViciOneServiceBusStateMachine<TInstance>
    where TInstance : class, SagaStateMachineInstance
{
    /// <summary>
    /// Provides a selected event observer implementation.
    /// </summary>
    public class SelectedEventObserver :
        IEventObserver<TInstance>
    {
        readonly Event _event;
        readonly IEventObserver<TInstance> _observer;

        /// <summary>
        /// Initializes a new instance of the containing type.
        /// </summary>
        /// <param name="event">The event value.</param>
        /// <param name="observer">The observer value.</param>
        public SelectedEventObserver(Event @event, IEventObserver<TInstance> observer)
        {
            _event = @event;
            _observer = observer;
        }

        /// <summary>
        /// Performs the pre execute operation.
        /// </summary>
        /// <param name="context">The operation context.</param>
        /// <returns>The result of the operation.</returns>
        public Task PreExecuteAsync(BehaviorContext<TInstance> context)
        {
            return _event.Equals(context.Event)
                ? _observer.PreExecuteAsync(context)
                : Task.CompletedTask;
        }

        /// <summary>
        /// Performs the pre execute operation.
        /// </summary>
        /// <typeparam name="T">The t type.</typeparam>
        /// <param name="context">The operation context.</param>
        /// <returns>The result of the operation.</returns>
        public Task PreExecuteAsync<T>(BehaviorContext<TInstance, T> context)
            where T : class
        {
            return _event.Equals(context.Event)
                ? _observer.PreExecuteAsync(context)
                : Task.CompletedTask;
        }

        /// <summary>
        /// Performs the post execute operation.
        /// </summary>
        /// <param name="context">The operation context.</param>
        /// <returns>The result of the operation.</returns>
        public Task PostExecuteAsync(BehaviorContext<TInstance> context)
        {
            return _event.Equals(context.Event)
                ? _observer.PostExecuteAsync(context)
                : Task.CompletedTask;
        }

        /// <summary>
        /// Performs the post execute operation.
        /// </summary>
        /// <typeparam name="T">The t type.</typeparam>
        /// <param name="context">The operation context.</param>
        /// <returns>The result of the operation.</returns>
        public Task PostExecuteAsync<T>(BehaviorContext<TInstance, T> context)
            where T : class
        {
            return _event.Equals(context.Event)
                ? _observer.PostExecuteAsync(context)
                : Task.CompletedTask;
        }

        /// <summary>
        /// Performs the execute fault operation.
        /// </summary>
        /// <param name="context">The operation context.</param>
        /// <param name="exception">The exception associated with the operation.</param>
        /// <returns>The result of the operation.</returns>
        public Task ExecuteFaultAsync(BehaviorContext<TInstance> context, Exception exception)
        {
            return _event.Equals(context.Event)
                ? _observer.ExecuteFaultAsync(context, exception)
                : Task.CompletedTask;
        }

        /// <summary>
        /// Performs the execute fault operation.
        /// </summary>
        /// <typeparam name="T">The t type.</typeparam>
        /// <param name="context">The operation context.</param>
        /// <param name="exception">The exception associated with the operation.</param>
        /// <returns>The result of the operation.</returns>
        public Task ExecuteFaultAsync<T>(BehaviorContext<TInstance, T> context, Exception exception)
            where T : class
        {
            return _event.Equals(context.Event)
                ? _observer.ExecuteFaultAsync(context, exception)
                : Task.CompletedTask;
        }
    }
}
