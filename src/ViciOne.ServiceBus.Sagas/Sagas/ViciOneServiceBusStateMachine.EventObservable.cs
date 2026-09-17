using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Sagas;

public partial class ViciOneServiceBusStateMachine<TInstance>
    where TInstance : class, ISagaStateMachineInstance
{
    /// <summary>
    /// Fans event execution notifications out to every connected saga observer and propagates observer task failures.
    /// </summary>
    public class EventObservable :
        Connectable<IEventObserver<TInstance>>,
        IEventObserver<TInstance>
    {
        /// <summary>Notifies connected observers before the event's behavior executes.</summary>
        /// <param name="context">The saga and event being observed.</param>
        /// <returns>The task for forwarding the notification.</returns>
        public Task PreExecuteAsync(IBehaviorContext<TInstance> context)
        {
            ArgumentNullException.ThrowIfNull(context);
            return ForEachAsync(x => x.PreExecuteAsync(context));
        }

        /// <summary>Notifies connected observers before the message event's behavior executes.</summary>
        /// <typeparam name="T">The event's message contract.</typeparam>
        /// <param name="context">The saga, event and message being observed.</param>
        /// <returns>The task for forwarding the notification.</returns>
        public Task PreExecuteAsync<T>(IBehaviorContext<TInstance, T> context)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(context);
            return ForEachAsync(x => x.PreExecuteAsync(context));
        }

        /// <summary>Notifies connected observers after the event's behavior executes successfully.</summary>
        /// <param name="context">The saga and event being observed.</param>
        /// <returns>The task for forwarding the notification.</returns>
        public Task PostExecuteAsync(IBehaviorContext<TInstance> context)
        {
            ArgumentNullException.ThrowIfNull(context);
            return ForEachAsync(x => x.PostExecuteAsync(context));
        }

        /// <summary>Notifies connected observers after the message event's behavior executes successfully.</summary>
        /// <typeparam name="T">The event's message contract.</typeparam>
        /// <param name="context">The saga, event and message being observed.</param>
        /// <returns>The task for forwarding the notification.</returns>
        public Task PostExecuteAsync<T>(IBehaviorContext<TInstance, T> context)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(context);
            return ForEachAsync(x => x.PostExecuteAsync(context));
        }

        /// <summary>Forwards an event execution failure to the connected observers.</summary>
        /// <param name="context">The saga and event whose execution failed.</param>
        /// <param name="exception">The execution failure supplied to the observers.</param>
        /// <returns>The task for forwarding the notification.</returns>
        public Task ExecuteFaultAsync(IBehaviorContext<TInstance> context, Exception exception)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(exception);
            return ForEachAsync(x => x.ExecuteFaultAsync(context, exception));
        }

        /// <summary>Forwards a message event execution failure to the connected observers.</summary>
        /// <typeparam name="T">The event's message contract.</typeparam>
        /// <param name="context">The saga, event and message whose execution failed.</param>
        /// <param name="exception">The execution failure supplied to the observers.</param>
        /// <returns>The task for forwarding the notification.</returns>
        public Task ExecuteFaultAsync<T>(IBehaviorContext<TInstance, T> context, Exception exception)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(exception);
            return ForEachAsync(x => x.ExecuteFaultAsync(context, exception));
        }
    }
}
