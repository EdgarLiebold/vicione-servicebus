using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Sagas;

public partial class ViciOneServiceBusStateMachine<TInstance>
    where TInstance : class, ISagaStateMachineInstance
{
    /// <summary>
    /// Forwards notifications only when the context's event equals the selected event and requires a task from the observer.
    /// </summary>
    public class SelectedEventObserver :
        IEventObserver<TInstance>
    {
        readonly IEvent _event;
        readonly IEventObserver<TInstance> _observer;

        /// <summary>Selects the event whose notifications are forwarded to an observer.</summary>
        /// <param name="event">The event matched against each notification's context.</param>
        /// <param name="observer">The observer that receives matching notifications.</param>
        public SelectedEventObserver(IEvent @event, IEventObserver<TInstance> observer)
        {
            ArgumentNullException.ThrowIfNull(@event, "event");
            ArgumentNullException.ThrowIfNull(observer);

            _event = @event;
            _observer = observer;
        }

        /// <summary>Forwards a pre-execution notification for the selected event.</summary>
        /// <param name="context">The saga and event being observed.</param>
        /// <returns>The observer's task for a match; otherwise, an already completed task.</returns>
        public Task PreExecuteAsync(IBehaviorContext<TInstance> context)
        {
            ArgumentNullException.ThrowIfNull(context);
            return _event.Equals(context.Event)
                ? RequireObserverTaskAsync(_observer.PreExecuteAsync(context))
                : Task.CompletedTask;
        }

        /// <summary>Forwards a message pre-execution notification for the selected event.</summary>
        /// <typeparam name="T">The event's message contract.</typeparam>
        /// <param name="context">The saga, event and message being observed.</param>
        /// <returns>The observer's task for a match; otherwise, an already completed task.</returns>
        public Task PreExecuteAsync<T>(IBehaviorContext<TInstance, T> context)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(context);
            return _event.Equals(context.Event)
                ? RequireObserverTaskAsync(_observer.PreExecuteAsync(context))
                : Task.CompletedTask;
        }

        /// <summary>Forwards a post-execution notification for the selected event.</summary>
        /// <param name="context">The saga and event being observed.</param>
        /// <returns>The observer's task for a match; otherwise, an already completed task.</returns>
        public Task PostExecuteAsync(IBehaviorContext<TInstance> context)
        {
            ArgumentNullException.ThrowIfNull(context);
            return _event.Equals(context.Event)
                ? RequireObserverTaskAsync(_observer.PostExecuteAsync(context))
                : Task.CompletedTask;
        }

        /// <summary>Forwards a message post-execution notification for the selected event.</summary>
        /// <typeparam name="T">The event's message contract.</typeparam>
        /// <param name="context">The saga, event and message being observed.</param>
        /// <returns>The observer's task for a match; otherwise, an already completed task.</returns>
        public Task PostExecuteAsync<T>(IBehaviorContext<TInstance, T> context)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(context);
            return _event.Equals(context.Event)
                ? RequireObserverTaskAsync(_observer.PostExecuteAsync(context))
                : Task.CompletedTask;
        }

        /// <summary>Forwards an execution failure for the selected event.</summary>
        /// <param name="context">The saga and event whose execution failed.</param>
        /// <param name="exception">The execution failure supplied to the observer.</param>
        /// <returns>The observer's task for a match; otherwise, an already completed task.</returns>
        public Task ExecuteFaultAsync(IBehaviorContext<TInstance> context, Exception exception)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(exception);
            return _event.Equals(context.Event)
                ? RequireObserverTaskAsync(_observer.ExecuteFaultAsync(context, exception))
                : Task.CompletedTask;
        }

        /// <summary>Forwards a message execution failure for the selected event.</summary>
        /// <typeparam name="T">The event's message contract.</typeparam>
        /// <param name="context">The saga, event and message whose execution failed.</param>
        /// <param name="exception">The execution failure supplied to the observer.</param>
        /// <returns>The observer's task for a match; otherwise, an already completed task.</returns>
        public Task ExecuteFaultAsync<T>(IBehaviorContext<TInstance, T> context, Exception exception)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(exception);
            return _event.Equals(context.Event)
                ? RequireObserverTaskAsync(_observer.ExecuteFaultAsync(context, exception))
                : Task.CompletedTask;
        }

        static Task RequireObserverTaskAsync(Task? task)
        {
            return task ?? throw new InvalidOperationException("The event observer returned no notification task.");
        }
    }
}
