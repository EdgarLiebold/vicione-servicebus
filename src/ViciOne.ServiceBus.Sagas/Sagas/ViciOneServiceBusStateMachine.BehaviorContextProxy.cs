using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Initializers;

namespace ViciOne.ServiceBus.Sagas;

public partial class ViciOneServiceBusStateMachine<TInstance>
    where TInstance : class, ISagaStateMachineInstance
{
    /// <summary>Exposes a state-machine event over the selected saga consume context.</summary>
    public class BehaviorContextProxy :
        ConsumeContextProxy,
        IBehaviorContext<TInstance>
    {
        readonly SagaConsumeContext<TInstance> _context;
        readonly IEvent _event;
        readonly CancellationToken? _operationCancellationToken;
        readonly IBehaviorContext<TInstance>? _operationSourceContext;

        /// <summary>Associates a state machine and event with the selected saga consume context.</summary>
        /// <param name="machine">The state machine executing raised events.</param>
        /// <param name="context">The saga consume context providing instance state, completion and messaging operations.</param>
        /// <param name="event">The event represented by this behavior context.</param>
        public BehaviorContextProxy(IStateMachine<TInstance> machine, SagaConsumeContext<TInstance> context, IEvent @event)
            : base(context ?? throw new ArgumentNullException(nameof(context)))
        {
            ArgumentNullException.ThrowIfNull(machine);
            ArgumentNullException.ThrowIfNull(@event, "event");

            StateMachine = machine;
            _context = context;
            _event = @event;
        }

        internal BehaviorContextProxy(IBehaviorContext<TInstance> context, CancellationToken cancellationToken)
            : this(context.StateMachine, context, context.Event)
        {
            _operationCancellationToken = cancellationToken;
            _operationSourceContext = context;
        }

        /// <inheritdoc />
        public override CancellationToken CancellationToken => _operationCancellationToken ?? base.CancellationToken;

        /// <summary>Gets the state machine executing events raised from this context.</summary>
        public IStateMachine<TInstance> StateMachine { get; }

        /// <summary>Gets the selected saga instance's correlation identifier.</summary>
        public override Guid? CorrelationId => Saga.CorrelationId;

        /// <summary>Gets the saga instance selected by the underlying consume context.</summary>
        public TInstance Saga => _context.Saga;

        /// <summary>Marks the underlying saga consume context as completed.</summary>
        /// <param name="cancellationToken">The cancellation token forwarded to saga completion.</param>
        /// <returns>The underlying saga completion task.</returns>
        public Task SetCompletedAsync(CancellationToken cancellationToken = default)
        {
            return _context.SetCompletedAsync(cancellationToken: cancellationToken);
        }

        /// <summary>Gets whether the underlying saga consume context is completed.</summary>
        public bool IsCompleted => _context.IsCompleted;

        /// <summary>Raises another event using the same saga consume context.</summary>
        /// <param name="event">The event raised on the associated state machine.</param>
        /// <param name="cancellationToken">The cancellation token forwarded to event execution.</param>
        /// <returns>The state machine's event execution task.</returns>
        public Task RaiseAsync(IEvent @event, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(@event, "event");
            return StateMachine.RaiseEventAsync(CreateProxy(@event), cancellationToken: cancellationToken);
        }

        /// <summary>Raises a message event using the same saga and a consume-context view of the supplied message.</summary>
        /// <typeparam name="T">The message contract carried by the raised event.</typeparam>
        /// <param name="event">The message event raised on the associated state machine.</param>
        /// <param name="data">The message supplied to the raised event.</param>
        /// <param name="cancellationToken">The cancellation token forwarded to event execution.</param>
        /// <returns>The state machine's message-event execution task.</returns>
        public Task RaiseAsync<T>(IEvent<T> @event, T data, CancellationToken cancellationToken = default)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(@event, "event");
            ArgumentNullException.ThrowIfNull(data);
            return StateMachine.RaiseEventAsync(CreateProxy(@event, data), cancellationToken: cancellationToken);
        }

        Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> IBehaviorContext<TInstance>.InitAsync<T>(object values, CancellationToken cancellationToken)
        {
            return MessageInitializerCache<T>.InitializeMessageAsync(this, values, cancellationToken: cancellationToken);
        }

        IEvent IBehaviorContext<TInstance>.Event => _event;

        /// <summary>Gets the selected saga instance through the state-machine instance contract.</summary>
        public TInstance Instance => _context.Saga;

        /// <summary>Creates a behavior context for another event over the same saga consume context.</summary>
        /// <param name="event">The event represented by the new context.</param>
        /// <returns>A behavior context sharing the selected saga and underlying consume context.</returns>
        public IBehaviorContext<TInstance> CreateProxy(IEvent @event)
        {
            ArgumentNullException.ThrowIfNull(@event, "event");
            if (_operationCancellationToken is { } cancellationToken)
            {
                IBehaviorContext<TInstance> source = _operationSourceContext!.CreateProxy(@event);
                return source is null ? source! : new BehaviorContextProxy(source, cancellationToken);
            }

            return new BehaviorContextProxy(StateMachine, _context, @event);
        }

        /// <summary>Creates a message-event behavior context with a message view over the same saga consume context.</summary>
        /// <typeparam name="T">The message contract carried by the new event context.</typeparam>
        /// <param name="event">The event represented by the new context.</param>
        /// <param name="data">The message exposed by the new context.</param>
        /// <returns>A message behavior context sharing the selected saga and underlying consume context.</returns>
        public IBehaviorContext<TInstance, T> CreateProxy<T>(IEvent<T> @event, T data)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(@event, "event");
            ArgumentNullException.ThrowIfNull(data);
            if (_operationCancellationToken is { } cancellationToken)
            {
                IBehaviorContext<TInstance, T> source = _operationSourceContext!.CreateProxy(@event, data);
                return source is null ? source! : new BehaviorContextProxy<T>(source, cancellationToken);
            }

            return new BehaviorContextProxy<T>(StateMachine, _context, new MessageConsumeContext<T>(_context, data), @event);
        }
    }


    /// <summary>Exposes a state-machine message event over separate saga and message consume-context views.</summary>
    /// <typeparam name="TMessage">The message contract exposed by the behavior context.</typeparam>
    public class BehaviorContextProxy<TMessage> :
        ConsumeContextProxy<TMessage>,
        IBehaviorContext<TInstance, TMessage>
        where TMessage : class
    {
        readonly SagaConsumeContext<TInstance> _context;
        readonly IEvent<TMessage> _event;
        readonly CancellationToken? _operationCancellationToken;
        readonly IBehaviorContext<TInstance, TMessage>? _operationSourceContext;

        /// <summary>Associates a state-machine event with its selected saga and message consume contexts.</summary>
        /// <param name="machine">The state machine executing raised events.</param>
        /// <param name="context">The saga consume context providing the selected instance and completion state.</param>
        /// <param name="consumeContext">The message consume context providing message data and messaging operations.</param>
        /// <param name="event">The message event represented by this behavior context.</param>
        public BehaviorContextProxy(IStateMachine<TInstance> machine, SagaConsumeContext<TInstance> context, ConsumeContext<TMessage> consumeContext,
            IEvent<TMessage> @event)
            : base(consumeContext ?? throw new ArgumentNullException(nameof(consumeContext)))
        {
            ArgumentNullException.ThrowIfNull(machine);
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(@event, "event");

            StateMachine = machine;
            _context = context;
            _event = @event;
        }

        internal BehaviorContextProxy(IBehaviorContext<TInstance, TMessage> context, CancellationToken cancellationToken)
            : this(context.StateMachine, context, context, context.Event)
        {
            _operationCancellationToken = cancellationToken;
            _operationSourceContext = context;
        }

        /// <inheritdoc />
        public override CancellationToken CancellationToken => _operationCancellationToken ?? base.CancellationToken;

        /// <summary>Gets the state machine executing events raised from this context.</summary>
        public IStateMachine<TInstance> StateMachine { get; }

        /// <summary>Gets the selected saga instance's correlation identifier.</summary>
        public override Guid? CorrelationId => Saga.CorrelationId;

        /// <summary>Gets the saga instance selected by the underlying saga consume context.</summary>
        public TInstance Saga => _context.Saga;

        /// <summary>Marks the underlying saga consume context as completed.</summary>
        /// <param name="cancellationToken">The cancellation token forwarded to saga completion.</param>
        /// <returns>The underlying saga completion task.</returns>
        public Task SetCompletedAsync(CancellationToken cancellationToken = default)
        {
            return _context.SetCompletedAsync(cancellationToken: cancellationToken);
        }

        /// <summary>Gets whether the underlying saga consume context is completed.</summary>
        public bool IsCompleted => _context.IsCompleted;

        /// <summary>Raises another event using the same selected saga consume context.</summary>
        /// <param name="event">The event raised on the associated state machine.</param>
        /// <param name="cancellationToken">The cancellation token forwarded to event execution.</param>
        /// <returns>The state machine's event execution task.</returns>
        public Task RaiseAsync(IEvent @event, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(@event, "event");
            return StateMachine.RaiseEventAsync(CreateProxy(@event), cancellationToken: cancellationToken);
        }

        /// <summary>Raises a message event with the same saga and a consume-context view of the supplied message.</summary>
        /// <typeparam name="T">The message contract carried by the raised event.</typeparam>
        /// <param name="event">The message event raised on the associated state machine.</param>
        /// <param name="data">The message supplied to the raised event.</param>
        /// <param name="cancellationToken">The cancellation token forwarded to event execution.</param>
        /// <returns>The state machine's message-event execution task.</returns>
        public Task RaiseAsync<T>(IEvent<T> @event, T data, CancellationToken cancellationToken = default)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(@event, "event");
            ArgumentNullException.ThrowIfNull(data);
            return StateMachine.RaiseEventAsync(CreateProxy(@event, data), cancellationToken: cancellationToken);
        }

        Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> IBehaviorContext<TInstance, TMessage>.InitAsync<T>(object values, CancellationToken cancellationToken)
        {
            return MessageInitializerCache<T>.InitializeMessageAsync(this, values, cancellationToken: cancellationToken);
        }

        Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> IBehaviorContext<TInstance>.InitAsync<T>(object values, CancellationToken cancellationToken)
        {
            return MessageInitializerCache<T>.InitializeMessageAsync(this, values, cancellationToken: cancellationToken);
        }

        /// <summary>Gets the message exposed by the underlying message consume context.</summary>
        public TMessage Data => Message;
        IEvent IBehaviorContext<TInstance>.Event => _event;
        IEvent<TMessage> IBehaviorContext<TInstance, TMessage>.Event => _event;

        /// <summary>Gets the selected saga instance through the state-machine instance contract.</summary>
        public TInstance Instance => _context.Saga;

        /// <summary>Creates an event behavior context over the same selected saga consume context.</summary>
        /// <param name="event">The event represented by the new context.</param>
        /// <returns>A behavior context sharing the selected saga and underlying saga consume context.</returns>
        public IBehaviorContext<TInstance> CreateProxy(IEvent @event)
        {
            ArgumentNullException.ThrowIfNull(@event, "event");
            if (_operationCancellationToken is { } cancellationToken)
            {
                IBehaviorContext<TInstance> source = _operationSourceContext!.CreateProxy(@event);
                return source is null ? source! : new BehaviorContextProxy(source, cancellationToken);
            }

            return new BehaviorContextProxy(StateMachine, _context, @event);
        }

        /// <summary>Creates a message-event behavior context with a new message view over the same saga consume context.</summary>
        /// <typeparam name="T">The message contract carried by the new event context.</typeparam>
        /// <param name="event">The event represented by the new context.</param>
        /// <param name="data">The message exposed by the new context.</param>
        /// <returns>A message behavior context sharing the selected saga and underlying saga consume context.</returns>
        public IBehaviorContext<TInstance, T> CreateProxy<T>(IEvent<T> @event, T data)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(@event, "event");
            ArgumentNullException.ThrowIfNull(data);
            if (_operationCancellationToken is { } cancellationToken)
            {
                IBehaviorContext<TInstance, T> source = _operationSourceContext!.CreateProxy(@event, data);
                return source is null ? source! : new BehaviorContextProxy<T>(source, cancellationToken);
            }

            return new BehaviorContextProxy<T>(StateMachine, _context, new MessageConsumeContext<T>(_context, data), @event);
        }
    }
}
