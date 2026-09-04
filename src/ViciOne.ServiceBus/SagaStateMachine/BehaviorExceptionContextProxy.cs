using System;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>
/// Provides a vici one service bus state machine implementation.
/// </summary>
public partial class ViciOneServiceBusStateMachine<TInstance>
    where TInstance : class, SagaStateMachineInstance
{
    /// <summary>
    /// Provides a behavior exception context proxy implementation.
    /// </summary>
    /// <typeparam name="TException">The t exception type.</typeparam>
    public class BehaviorExceptionContextProxy<TException> :
        BehaviorContextProxy,
        BehaviorExceptionContext<TInstance, TException>
        where TException : Exception
    {
        readonly BehaviorContext<TInstance> _context;

        /// <summary>
        /// Initializes a new instance of the containing type.
        /// </summary>
        /// <param name="context">The operation context.</param>
        /// <param name="exception">The exception associated with the operation.</param>
        public BehaviorExceptionContextProxy(BehaviorContext<TInstance> context, TException exception)
            : base(context.StateMachine, context, context.Event)
        {
            _context = context;
            Exception = exception;
        }

        /// <summary>
        /// Gets the exception value.
        /// </summary>
        public TException Exception { get; }

        /// <summary>
        /// Creates proxy.
        /// </summary>
        /// <typeparam name="T">The t type.</typeparam>
        /// <param name="event">The event value.</param>
        /// <param name="data">The data value.</param>
        /// <returns>The result of the operation.</returns>
        public new BehaviorExceptionContext<TInstance, T, TException> CreateProxy<T>(Event<T> @event, T data)
            where T : class
        {
            return new BehaviorExceptionContextProxy<T, TException>(_context.CreateProxy(@event, data), Exception);
        }
    }


    /// <summary>
    /// Provides a behavior exception context proxy implementation.
    /// </summary>
    /// <typeparam name="TData">The t data type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    public class BehaviorExceptionContextProxy<TData, TException> :
        BehaviorContextProxy<TData>,
        BehaviorExceptionContext<TInstance, TData, TException>
        where TData : class
        where TException : Exception
    {
        readonly BehaviorContext<TInstance, TData> _context;

        /// <summary>
        /// Initializes a new instance of the containing type.
        /// </summary>
        /// <param name="context">The operation context.</param>
        /// <param name="exception">The exception associated with the operation.</param>
        public BehaviorExceptionContextProxy(BehaviorContext<TInstance, TData> context, TException exception)
            : base(context.StateMachine, context, context, context.Event)
        {
            _context = context;
            Exception = exception;
        }

        /// <summary>
        /// Gets the exception value.
        /// </summary>
        public TException Exception { get; }

        /// <summary>
        /// Creates proxy.
        /// </summary>
        /// <typeparam name="T">The t type.</typeparam>
        /// <param name="event">The event value.</param>
        /// <param name="data">The data value.</param>
        /// <returns>The result of the operation.</returns>
        public new BehaviorExceptionContext<TInstance, T, TException> CreateProxy<T>(Event<T> @event, T data)
            where T : class
        {
            return new BehaviorExceptionContextProxy<T, TException>(_context.CreateProxy(@event, data), Exception);
        }
    }
}
