using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Initializers;

namespace ViciOne.ServiceBus;

public partial class ViciOneServiceBusStateMachine<TInstance>
    where TInstance : class, SagaStateMachineInstance
{
    public class BehaviorContextProxy :
        ConsumeContextProxy,
        BehaviorContext<TInstance>
    {
        readonly SagaConsumeContext<TInstance> _context;
        readonly Event _event;

        public BehaviorContextProxy(StateMachine<TInstance> machine, SagaConsumeContext<TInstance> context, Event @event)
            : base(context)
        {
            StateMachine = machine;
            _context = context;
            _event = @event;
        }

        public StateMachine<TInstance> StateMachine { get; }

        public override Guid? CorrelationId => Saga.CorrelationId;

        public TInstance Saga => _context.Saga;

        public Task SetCompletedAsync(CancellationToken cancellationToken = default)
        {
            return _context.SetCompletedAsync(cancellationToken: cancellationToken);
        }

        public bool IsCompleted => _context.IsCompleted;

        public Task RaiseAsync(Event @event, CancellationToken cancellationToken = default)
        {
            return StateMachine.RaiseEventAsync(CreateProxy(@event), cancellationToken: cancellationToken);
        }

        public Task RaiseAsync<T>(Event<T> @event, T data, CancellationToken cancellationToken = default)
            where T : class
        {
            return StateMachine.RaiseEventAsync(CreateProxy(@event, data), cancellationToken: cancellationToken);
        }

        Task<SendTuple<T>> BehaviorContext<TInstance>.InitAsync<T>(object values, CancellationToken cancellationToken)
        {
            return MessageInitializerCache<T>.InitializeMessageAsync(this, values, cancellationToken: cancellationToken);
        }

        Event BehaviorContext<TInstance>.Event => _event;

        public TInstance Instance => _context.Saga;

        public BehaviorContext<TInstance> CreateProxy(Event @event)
        {
            return new BehaviorContextProxy(StateMachine, _context, @event);
        }

        public BehaviorContext<TInstance, T> CreateProxy<T>(Event<T> @event, T data)
            where T : class
        {
            return new BehaviorContextProxy<T>(StateMachine, _context, new MessageConsumeContext<T>(_context, data), @event);
        }
    }


    public class BehaviorContextProxy<TMessage> :
        ConsumeContextProxy<TMessage>,
        BehaviorContext<TInstance, TMessage>
        where TMessage : class
    {
        readonly SagaConsumeContext<TInstance> _context;
        readonly Event<TMessage> _event;

        public BehaviorContextProxy(StateMachine<TInstance> machine, SagaConsumeContext<TInstance> context, ConsumeContext<TMessage> consumeContext,
            Event<TMessage> @event)
            : base(consumeContext)
        {
            StateMachine = machine;
            _context = context;
            _event = @event;
        }

        public StateMachine<TInstance> StateMachine { get; }

        public override Guid? CorrelationId => Saga.CorrelationId;

        public TInstance Saga => _context.Saga;

        public Task SetCompletedAsync(CancellationToken cancellationToken = default)
        {
            return _context.SetCompletedAsync(cancellationToken: cancellationToken);
        }

        public bool IsCompleted => _context.IsCompleted;

        public Task RaiseAsync(Event @event, CancellationToken cancellationToken = default)
        {
            return StateMachine.RaiseEventAsync(CreateProxy(@event), cancellationToken: cancellationToken);
        }

        public Task RaiseAsync<T>(Event<T> @event, T data, CancellationToken cancellationToken = default)
            where T : class
        {
            return StateMachine.RaiseEventAsync(CreateProxy(@event, data), cancellationToken: cancellationToken);
        }

        Task<SendTuple<T>> BehaviorContext<TInstance, TMessage>.InitAsync<T>(object values, CancellationToken cancellationToken)
        {
            return MessageInitializerCache<T>.InitializeMessageAsync(this, values, cancellationToken: cancellationToken);
        }

        Task<SendTuple<T>> BehaviorContext<TInstance>.InitAsync<T>(object values, CancellationToken cancellationToken)
        {
            return MessageInitializerCache<T>.InitializeMessageAsync(this, values, cancellationToken: cancellationToken);
        }

        public TMessage Data => Message;
        Event BehaviorContext<TInstance>.Event => _event;
        Event<TMessage> BehaviorContext<TInstance, TMessage>.Event => _event;

        public TInstance Instance => _context.Saga;

        public BehaviorContext<TInstance> CreateProxy(Event @event)
        {
            return new BehaviorContextProxy(StateMachine, _context, @event);
        }

        public BehaviorContext<TInstance, T> CreateProxy<T>(Event<T> @event, T data)
            where T : class
        {
            return new BehaviorContextProxy<T>(StateMachine, _context, new MessageConsumeContext<T>(_context, data), @event);
        }
    }
}
