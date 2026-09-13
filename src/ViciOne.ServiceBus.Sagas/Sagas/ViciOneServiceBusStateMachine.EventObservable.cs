using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Sagas;

public partial class ViciOneServiceBusStateMachine<TInstance>
    where TInstance : class, ISagaStateMachineInstance
{
    /// <summary>Publishes observations for event.</summary>
    public class EventObservable :
        Connectable<IEventObserver<TInstance>>,
        IEventObserver<TInstance>
    {
        /// <summary>Runs before execute.</summary>
        /// <param name="context">The context associated with the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task PreExecuteAsync(IBehaviorContext<TInstance> context)
        {
            return ForEachAsync(x => x.PreExecuteAsync(context));
        }

        /// <summary>Runs before execute.</summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="context">The context associated with the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task PreExecuteAsync<T>(IBehaviorContext<TInstance, T> context)
            where T : class
        {
            return ForEachAsync(x => x.PreExecuteAsync(context));
        }

        /// <summary>Runs after execute.</summary>
        /// <param name="context">The context associated with the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task PostExecuteAsync(IBehaviorContext<TInstance> context)
        {
            return ForEachAsync(x => x.PostExecuteAsync(context));
        }

        /// <summary>Runs after execute.</summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="context">The context associated with the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task PostExecuteAsync<T>(IBehaviorContext<TInstance, T> context)
            where T : class
        {
            return ForEachAsync(x => x.PostExecuteAsync(context));
        }

        /// <summary>Executes fault.</summary>
        /// <param name="context">The context associated with the operation.</param>
        /// <param name="exception">The exception associated with the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task ExecuteFaultAsync(IBehaviorContext<TInstance> context, Exception exception)
        {
            return ForEachAsync(x => x.ExecuteFaultAsync(context, exception));
        }

        /// <summary>Executes fault.</summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="context">The context associated with the operation.</param>
        /// <param name="exception">The exception associated with the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task ExecuteFaultAsync<T>(IBehaviorContext<TInstance, T> context, Exception exception)
            where T : class
        {
            return ForEachAsync(x => x.ExecuteFaultAsync(context, exception));
        }
    }
}
