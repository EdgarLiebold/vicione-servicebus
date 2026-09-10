using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Testing.Internal;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Records saga creation, consumption, and repository outcomes for a test harness.</summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
internal sealed class TestSagaRepositoryDecorator<TSaga> :
    ISagaRepository<TSaga>
    where TSaga : class, ISaga
{
    readonly SagaList<TSaga> _created;
    readonly ConsumedMessageList _consumed;
    readonly ISagaRepository<TSaga> _sagaRepository;
    readonly SagaList<TSaga> _sagas;

    /// <summary>Creates an observation decorator over a saga repository.</summary>
    /// <param name="sagaRepository">The repository to decorate.</param>
    /// <param name="consumed">The list that records delivery outcomes.</param>
    /// <param name="created">The list that records created saga instances.</param>
    /// <param name="sagas">The list that records every observed saga instance.</param>
    public TestSagaRepositoryDecorator(ISagaRepository<TSaga> sagaRepository, ConsumedMessageList consumed, SagaList<TSaga> created,
        SagaList<TSaga> sagas)
    {
        _sagaRepository = sagaRepository ?? throw new ArgumentNullException(nameof(sagaRepository));
        _consumed = consumed ?? throw new ArgumentNullException(nameof(consumed));
        _created = created ?? throw new ArgumentNullException(nameof(created));
        _sagas = sagas ?? throw new ArgumentNullException(nameof(sagas));
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _sagaRepository.Probe(context);
    }

    Task ISagaRepository<TSaga>.SendAsync<TMessage>(ConsumeContext<TMessage> context, ISagaPolicy<TSaga, TMessage> policy,
        IPipe<SagaConsumeContext<TSaga, TMessage>> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(next);
        var preInserted = new PreInsertedSagaTracker();
        var interceptPipe = new InterceptPipe<TMessage>(_sagas, _consumed, _created, preInserted, next);
        var interceptPolicy = new InterceptPolicy<TMessage>(_created, preInserted, policy);

        return _sagaRepository.SendAsync(context, interceptPolicy, interceptPipe);
    }

    Task ISagaRepository<TSaga>.SendQueryAsync<TMessage>(ConsumeContext<TMessage> context, ISagaQuery<TSaga> query,
        ISagaPolicy<TSaga, TMessage> policy, IPipe<SagaConsumeContext<TSaga, TMessage>> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(next);
        var preInserted = new PreInsertedSagaTracker();
        var interceptPipe = new InterceptPipe<TMessage>(_sagas, _consumed, _created, preInserted, next);
        var interceptPolicy = new InterceptPolicy<TMessage>(_created, preInserted, policy);

        return _sagaRepository.SendQueryAsync(context, query, interceptPolicy, interceptPipe);
    }


    sealed class InterceptPipe<TMessage> :
        IPipe<SagaConsumeContext<TSaga, TMessage>>
        where TMessage : class
    {
        readonly IPipe<SagaConsumeContext<TSaga, TMessage>> _pipe;
        readonly PreInsertedSagaTracker _preInserted;
        readonly ConsumedMessageList _consumed;
        readonly SagaList<TSaga> _created;
        readonly SagaList<TSaga> _sagas;

        public InterceptPipe(SagaList<TSaga> sagas, ConsumedMessageList consumed, SagaList<TSaga> created,
            PreInsertedSagaTracker preInserted, IPipe<SagaConsumeContext<TSaga, TMessage>> pipe)
        {
            _sagas = sagas ?? throw new ArgumentNullException(nameof(sagas));
            _consumed = consumed ?? throw new ArgumentNullException(nameof(consumed));
            _created = created ?? throw new ArgumentNullException(nameof(created));
            _preInserted = preInserted ?? throw new ArgumentNullException(nameof(preInserted));
            _pipe = pipe ?? throw new ArgumentNullException(nameof(pipe));
        }

        void IProbeSite.Probe(ProbeContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            _pipe.Probe(context);
        }

        public async Task SendAsync(SagaConsumeContext<TSaga, TMessage> context)
        {
            ArgumentNullException.ThrowIfNull(context);
            // InsertOnInitial reaches this pipe through the Existing branch after persistence succeeds.
            // Record only the resulting consume context so Created contains persisted saga instances.
            if (_preInserted.Value)
                _created.Add(context);

            _sagas.Add(context);

            try
            {
                await _pipe.SendAsync(context).ConfigureAwait(false);

                _consumed.Add(context);
            }
            catch (Exception ex)
            {
                _consumed.Add(context, ex);
                throw;
            }
        }
    }


    sealed class InterceptPolicy<TMessage> :
        ISagaPolicy<TSaga, TMessage>
        where TMessage : class
    {
        readonly SagaList<TSaga> _created;
        readonly ISagaPolicy<TSaga, TMessage> _policy;
        readonly PreInsertedSagaTracker _preInserted;

        public InterceptPolicy(SagaList<TSaga> created, PreInsertedSagaTracker preInserted, ISagaPolicy<TSaga, TMessage> policy)
        {
            _created = created ?? throw new ArgumentNullException(nameof(created));
            _preInserted = preInserted ?? throw new ArgumentNullException(nameof(preInserted));
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        }

        public bool IsReadOnly => _policy.IsReadOnly;

        public bool PreInsertInstance(ConsumeContext<TMessage> context, [NotNullWhen(true)] out TSaga? instance)
        {
            ArgumentNullException.ThrowIfNull(context);
            _preInserted.Value = _policy.PreInsertInstance(context, out instance);

            return _preInserted.Value;
        }

        public Task ExistingAsync(SagaConsumeContext<TSaga, TMessage> context, IPipe<SagaConsumeContext<TSaga, TMessage>> next)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(next);
            return _policy.ExistingAsync(context, next);
        }

        public Task MissingAsync(ConsumeContext<TMessage> context, IPipe<SagaConsumeContext<TSaga, TMessage>> next)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(next);
            var interceptPipe = new InterceptPolicyPipe(_created, next);

            return _policy.MissingAsync(context, interceptPipe);
        }


        sealed class InterceptPolicyPipe :
            IPipe<SagaConsumeContext<TSaga, TMessage>>
        {
            readonly SagaList<TSaga> _created;
            readonly IPipe<SagaConsumeContext<TSaga, TMessage>> _pipe;

            public InterceptPolicyPipe(SagaList<TSaga> created, IPipe<SagaConsumeContext<TSaga, TMessage>> pipe)
            {
                _created = created ?? throw new ArgumentNullException(nameof(created));
                _pipe = pipe ?? throw new ArgumentNullException(nameof(pipe));
            }

            void IProbeSite.Probe(ProbeContext context)
            {
                ArgumentNullException.ThrowIfNull(context);
                _pipe.Probe(context);
            }

            public Task SendAsync(SagaConsumeContext<TSaga, TMessage> context)
            {
                ArgumentNullException.ThrowIfNull(context);
                _created.Add(context);

                return _pipe.SendAsync(context);
            }
        }
    }


    sealed class PreInsertedSagaTracker
    {
        public bool Value { get; set; }
    }
}
