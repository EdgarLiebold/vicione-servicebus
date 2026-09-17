using System;

namespace ViciOne.ServiceBus.Sagas;

public partial class ViciOneServiceBusStateMachine<TInstance>
    where TInstance : class, ISagaStateMachineInstance
{
    /// <summary>Associates an exception with an event behavior context while preserving its saga consume operations.</summary>
    /// <typeparam name="TException">The exception carried by the behavior context.</typeparam>
    public class BehaviorExceptionContextProxy<TException> :
        BehaviorContextProxy,
        IBehaviorExceptionContext<TInstance, TException>
        where TException : Exception
    {
        readonly IBehaviorContext<TInstance> _context;

        /// <summary>Creates an exception behavior context over the supplied event context.</summary>
        /// <param name="context">The context supplying the state machine, selected saga and event.</param>
        /// <param name="exception">The exception exposed by this context and its typed message proxies.</param>
        public BehaviorExceptionContextProxy(IBehaviorContext<TInstance> context, TException exception)
            : this(context ?? throw new ArgumentNullException(nameof(context)), exception, true)
        {
        }

        BehaviorExceptionContextProxy(IBehaviorContext<TInstance> context, TException exception, bool _)
            : base(context.StateMachine, context, context.Event)
        {
            ArgumentNullException.ThrowIfNull(exception);

            _context = context;
            Exception = exception;
        }

        /// <summary>Gets the exception associated with the behavior context.</summary>
        public TException Exception { get; }

        /// <summary>Creates a typed message-event proxy retaining the same exception and selected saga.</summary>
        /// <typeparam name="T">The message contract exposed by the new proxy.</typeparam>
        /// <param name="event">The event represented by the new proxy.</param>
        /// <param name="data">The message exposed by the new proxy.</param>
        /// <returns>A message exception context retaining the current exception.</returns>
        public new IBehaviorExceptionContext<TInstance, T, TException> CreateProxy<T>(IEvent<T> @event, T data)
            where T : class
        {
            return new BehaviorExceptionContextProxy<T, TException>(_context.CreateProxy(@event, data), Exception);
        }
    }


    /// <summary>Associates an exception with a message-event behavior context and preserves its saga and message views.</summary>
    /// <typeparam name="TData">The message contract exposed by the behavior context.</typeparam>
    /// <typeparam name="TException">The exception carried by the behavior context.</typeparam>
    public class BehaviorExceptionContextProxy<TData, TException> :
        BehaviorContextProxy<TData>,
        IBehaviorExceptionContext<TInstance, TData, TException>
        where TData : class
        where TException : Exception
    {
        readonly IBehaviorContext<TInstance, TData> _context;

        /// <summary>Creates an exception behavior context over the supplied message-event context.</summary>
        /// <param name="context">The context supplying the state machine, selected saga, message and event.</param>
        /// <param name="exception">The exception exposed by this context and its message proxies.</param>
        public BehaviorExceptionContextProxy(IBehaviorContext<TInstance, TData> context, TException exception)
            : this(context ?? throw new ArgumentNullException(nameof(context)), exception, true)
        {
        }

        BehaviorExceptionContextProxy(IBehaviorContext<TInstance, TData> context, TException exception, bool _)
            : base(context.StateMachine, context, context, context.Event)
        {
            ArgumentNullException.ThrowIfNull(exception);

            _context = context;
            Exception = exception;
        }

        /// <summary>Gets the exception associated with the message-event behavior context.</summary>
        public TException Exception { get; }

        /// <summary>Creates another typed message-event proxy retaining the same exception and selected saga.</summary>
        /// <typeparam name="T">The message contract exposed by the new proxy.</typeparam>
        /// <param name="event">The event represented by the new proxy.</param>
        /// <param name="data">The message exposed by the new proxy.</param>
        /// <returns>A message exception context retaining the current exception.</returns>
        public new IBehaviorExceptionContext<TInstance, T, TException> CreateProxy<T>(IEvent<T> @event, T data)
            where T : class
        {
            return new BehaviorExceptionContextProxy<T, TException>(_context.CreateProxy(@event, data), Exception);
        }
    }
}
