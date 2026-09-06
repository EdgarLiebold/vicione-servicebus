using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Initializers;

namespace ViciOne.ServiceBus.Sagas;

public partial class ViciOneServiceBusStateMachine<TInstance>
    where TInstance : class, SagaStateMachineInstance
{
    /// <summary>Forwards behavior context operations to an underlying context.</summary>
    public class BehaviorContextProxy :
        ConsumeContextProxy,
        BehaviorContext<TInstance>
    {
        readonly SagaConsumeContext<TInstance> _context;
        readonly Event _event;

        /// <summary>Initializes a new instance.</summary>
        /// <param name="machine">The machine.</param>
        /// <param name="context">The context associated with the operation.</param>
        /// <param name="event">The event.</param>
        public BehaviorContextProxy(StateMachine<TInstance> machine, SagaConsumeContext<TInstance> context, Event @event)
            : base(context)
        {
            StateMachine = machine;
            _context = context;
            _event = @event;
        }

        /// <summary>Gets the state machine.</summary>
        public StateMachine<TInstance> StateMachine { get; }

        /// <summary>Gets the correlation id.</summary>
        public override Guid? CorrelationId => Saga.CorrelationId;

        /// <summary>Gets the saga.</summary>
        public TInstance Saga => _context.Saga;

        /// <summary>Sets completed.</summary>
        /// <param name="cancellationToken">The token used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task SetCompletedAsync(CancellationToken cancellationToken = default)
        {
            return _context.SetCompletedAsync(cancellationToken: cancellationToken);
        }

        /// <summary>Gets a value indicating whether completed.</summary>
        public bool IsCompleted => _context.IsCompleted;

        /// <summary>Raises the configured event.</summary>
        /// <param name="event">The event.</param>
        /// <param name="cancellationToken">The token used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task RaiseAsync(Event @event, CancellationToken cancellationToken = default)
        {
            return StateMachine.RaiseEventAsync(CreateProxy(@event), cancellationToken: cancellationToken);
        }

        /// <summary>Raises the configured event.</summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="event">The event.</param>
        /// <param name="data">The data.</param>
        /// <param name="cancellationToken">The token used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task RaiseAsync<T>(Event<T> @event, T data, CancellationToken cancellationToken = default)
            where T : class
        {
            return StateMachine.RaiseEventAsync(CreateProxy(@event, data), cancellationToken: cancellationToken);
        }

        Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> BehaviorContext<TInstance>.InitAsync<T>(object values, CancellationToken cancellationToken)
        {
            return MessageInitializerCache<T>.InitializeMessageAsync(this, values, cancellationToken: cancellationToken);
        }

        Event BehaviorContext<TInstance>.Event => _event;

        /// <summary>Gets the instance.</summary>
        public TInstance Instance => _context.Saga;

        /// <summary>Creates proxy.</summary>
        /// <param name="event">The event.</param>
        /// <returns>The created proxy.</returns>
        public BehaviorContext<TInstance> CreateProxy(Event @event)
        {
            return new BehaviorContextProxy(StateMachine, _context, @event);
        }

        /// <summary>Creates proxy.</summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="event">The event.</param>
        /// <param name="data">The data.</param>
        /// <returns>The created proxy.</returns>
        public BehaviorContext<TInstance, T> CreateProxy<T>(Event<T> @event, T data)
            where T : class
        {
            return new BehaviorContextProxy<T>(StateMachine, _context, new MessageConsumeContext<T>(_context, data), @event);
        }
    }


    /// <summary>Forwards behavior context operations to an underlying context.</summary>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    public class BehaviorContextProxy<TMessage> :
        ConsumeContextProxy<TMessage>,
        BehaviorContext<TInstance, TMessage>
        where TMessage : class
    {
        readonly SagaConsumeContext<TInstance> _context;
        readonly Event<TMessage> _event;

        /// <summary>Initializes a new instance.</summary>
        /// <param name="machine">The machine.</param>
        /// <param name="context">The context associated with the operation.</param>
        /// <param name="consumeContext">The consume context.</param>
        /// <param name="event">The event.</param>
        public BehaviorContextProxy(StateMachine<TInstance> machine, SagaConsumeContext<TInstance> context, ConsumeContext<TMessage> consumeContext,
            Event<TMessage> @event)
            : base(consumeContext)
        {
            StateMachine = machine;
            _context = context;
            _event = @event;
        }

        /// <summary>Gets the state machine.</summary>
        public StateMachine<TInstance> StateMachine { get; }

        /// <summary>Gets the correlation id.</summary>
        public override Guid? CorrelationId => Saga.CorrelationId;

        /// <summary>Gets the saga.</summary>
        public TInstance Saga => _context.Saga;

        /// <summary>Sets completed.</summary>
        /// <param name="cancellationToken">The token used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task SetCompletedAsync(CancellationToken cancellationToken = default)
        {
            return _context.SetCompletedAsync(cancellationToken: cancellationToken);
        }

        /// <summary>Gets a value indicating whether completed.</summary>
        public bool IsCompleted => _context.IsCompleted;

        /// <summary>Raises the configured event.</summary>
        /// <param name="event">The event.</param>
        /// <param name="cancellationToken">The token used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task RaiseAsync(Event @event, CancellationToken cancellationToken = default)
        {
            return StateMachine.RaiseEventAsync(CreateProxy(@event), cancellationToken: cancellationToken);
        }

        /// <summary>Raises the configured event.</summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="event">The event.</param>
        /// <param name="data">The data.</param>
        /// <param name="cancellationToken">The token used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task RaiseAsync<T>(Event<T> @event, T data, CancellationToken cancellationToken = default)
            where T : class
        {
            return StateMachine.RaiseEventAsync(CreateProxy(@event, data), cancellationToken: cancellationToken);
        }

        Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> BehaviorContext<TInstance, TMessage>.InitAsync<T>(object values, CancellationToken cancellationToken)
        {
            return MessageInitializerCache<T>.InitializeMessageAsync(this, values, cancellationToken: cancellationToken);
        }

        Task<global::ViciOne.ServiceBus.Advanced.Initializers.InitializedMessage<T>> BehaviorContext<TInstance>.InitAsync<T>(object values, CancellationToken cancellationToken)
        {
            return MessageInitializerCache<T>.InitializeMessageAsync(this, values, cancellationToken: cancellationToken);
        }

        /// <summary>Gets the data.</summary>
        public TMessage Data => Message;
        Event BehaviorContext<TInstance>.Event => _event;
        Event<TMessage> BehaviorContext<TInstance, TMessage>.Event => _event;

        /// <summary>Gets the instance.</summary>
        public TInstance Instance => _context.Saga;

        /// <summary>Creates proxy.</summary>
        /// <param name="event">The event.</param>
        /// <returns>The created proxy.</returns>
        public BehaviorContext<TInstance> CreateProxy(Event @event)
        {
            return new BehaviorContextProxy(StateMachine, _context, @event);
        }

        /// <summary>Creates proxy.</summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="event">The event.</param>
        /// <param name="data">The data.</param>
        /// <returns>The created proxy.</returns>
        public BehaviorContext<TInstance, T> CreateProxy<T>(Event<T> @event, T data)
            where T : class
        {
            return new BehaviorContextProxy<T>(StateMachine, _context, new MessageConsumeContext<T>(_context, data), @event);
        }
    }
}
