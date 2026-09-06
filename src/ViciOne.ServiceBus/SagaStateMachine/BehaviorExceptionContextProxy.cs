using System;

namespace ViciOne.ServiceBus.Sagas;

public partial class ViciOneServiceBusStateMachine<TInstance>
    where TInstance : class, SagaStateMachineInstance
{
    /// <summary>Forwards behavior exception context operations to an underlying context.</summary>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    public class BehaviorExceptionContextProxy<TException> :
        BehaviorContextProxy,
        BehaviorExceptionContext<TInstance, TException>
        where TException : Exception
    {
        readonly BehaviorContext<TInstance> _context;

        /// <summary>Initializes a new instance.</summary>
        /// <param name="context">The context associated with the operation.</param>
        /// <param name="exception">The exception associated with the operation.</param>
        public BehaviorExceptionContextProxy(BehaviorContext<TInstance> context, TException exception)
            : base(context.StateMachine, context, context.Event)
        {
            _context = context;
            Exception = exception;
        }

        /// <summary>Gets the exception.</summary>
        public TException Exception { get; }

        /// <summary>Creates proxy.</summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="event">The event.</param>
        /// <param name="data">The data.</param>
        /// <returns>The created proxy.</returns>
        public new BehaviorExceptionContext<TInstance, T, TException> CreateProxy<T>(Event<T> @event, T data)
            where T : class
        {
            return new BehaviorExceptionContextProxy<T, TException>(_context.CreateProxy(@event, data), Exception);
        }
    }


    /// <summary>Forwards behavior exception context operations to an underlying context.</summary>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    public class BehaviorExceptionContextProxy<TData, TException> :
        BehaviorContextProxy<TData>,
        BehaviorExceptionContext<TInstance, TData, TException>
        where TData : class
        where TException : Exception
    {
        readonly BehaviorContext<TInstance, TData> _context;

        /// <summary>Initializes a new instance.</summary>
        /// <param name="context">The context associated with the operation.</param>
        /// <param name="exception">The exception associated with the operation.</param>
        public BehaviorExceptionContextProxy(BehaviorContext<TInstance, TData> context, TException exception)
            : base(context.StateMachine, context, context, context.Event)
        {
            _context = context;
            Exception = exception;
        }

        /// <summary>Gets the exception.</summary>
        public TException Exception { get; }

        /// <summary>Creates proxy.</summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="event">The event.</param>
        /// <param name="data">The data.</param>
        /// <returns>The created proxy.</returns>
        public new BehaviorExceptionContext<TInstance, T, TException> CreateProxy<T>(Event<T> @event, T data)
            where T : class
        {
            return new BehaviorExceptionContextProxy<T, TException>(_context.CreateProxy(@event, data), Exception);
        }
    }
}
