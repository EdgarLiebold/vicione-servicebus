using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>
/// Provides a vici one service bus state machine implementation.
/// </summary>
public partial class ViciOneServiceBusStateMachine<TInstance>
    where TInstance : class, SagaStateMachineInstance
{
    /// <summary>
    /// Provides an event observable implementation.
    /// </summary>
    public class EventObservable :
        Connectable<IEventObserver<TInstance>>,
        IEventObserver<TInstance>
    {
        /// <summary>
        /// Performs the pre execute operation.
        /// </summary>
        /// <param name="context">The operation context.</param>
        /// <returns>The result of the operation.</returns>
        public Task PreExecuteAsync(BehaviorContext<TInstance> context)
        {
            return ForEachAsync(x => x.PreExecuteAsync(context));
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
            return ForEachAsync(x => x.PreExecuteAsync(context));
        }

        /// <summary>
        /// Performs the post execute operation.
        /// </summary>
        /// <param name="context">The operation context.</param>
        /// <returns>The result of the operation.</returns>
        public Task PostExecuteAsync(BehaviorContext<TInstance> context)
        {
            return ForEachAsync(x => x.PostExecuteAsync(context));
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
            return ForEachAsync(x => x.PostExecuteAsync(context));
        }

        /// <summary>
        /// Performs the execute fault operation.
        /// </summary>
        /// <param name="context">The operation context.</param>
        /// <param name="exception">The exception associated with the operation.</param>
        /// <returns>The result of the operation.</returns>
        public Task ExecuteFaultAsync(BehaviorContext<TInstance> context, Exception exception)
        {
            return ForEachAsync(x => x.ExecuteFaultAsync(context, exception));
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
            return ForEachAsync(x => x.ExecuteFaultAsync(context, exception));
        }
    }
}
