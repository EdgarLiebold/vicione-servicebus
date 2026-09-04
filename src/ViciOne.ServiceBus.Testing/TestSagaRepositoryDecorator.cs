using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Testing.Implementations;

namespace ViciOne.ServiceBus.Testing;

public class TestSagaRepositoryDecorator<TSaga> :
    ISagaRepository<TSaga>
    where TSaga : class, ISaga
{
    readonly SagaList<TSaga> _created;
    readonly ReceivedMessageList _received;
    readonly ISagaRepository<TSaga> _sagaRepository;
    readonly SagaList<TSaga> _sagas;

    public TestSagaRepositoryDecorator(ISagaRepository<TSaga> sagaRepository, ReceivedMessageList received, SagaList<TSaga> created,
        SagaList<TSaga> sagas)
    {
        _sagaRepository = sagaRepository;
        _received = received;
        _created = created;
        _sagas = sagas;
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        _sagaRepository.Probe(context);
    }

    Task ISagaRepository<TSaga>.Send<T>(ConsumeContext<T> context, ISagaPolicy<TSaga, T> policy, IPipe<SagaConsumeContext<TSaga, T>> next)
    {
        var preInserted = new PreInsertedSagaTracker();
        var interceptPipe = new InterceptPipe<T>(_sagas, _received, _created, preInserted, next);
        var interceptPolicy = new InterceptPolicy<T>(_created, preInserted, policy);

        return _sagaRepository.Send(context, interceptPolicy, interceptPipe);
    }

    Task ISagaRepository<TSaga>.SendQuery<T>(ConsumeContext<T> context, ISagaQuery<TSaga> query, ISagaPolicy<TSaga, T> policy,
        IPipe<SagaConsumeContext<TSaga, T>> next)
    {
        var preInserted = new PreInsertedSagaTracker();
        var interceptPipe = new InterceptPipe<T>(_sagas, _received, _created, preInserted, next);
        var interceptPolicy = new InterceptPolicy<T>(_created, preInserted, policy);

        return _sagaRepository.SendQuery(context, query, interceptPolicy, interceptPipe);
    }


    class InterceptPipe<TMessage> :
        IPipe<SagaConsumeContext<TSaga, TMessage>>
        where TMessage : class
    {
        readonly IPipe<SagaConsumeContext<TSaga, TMessage>> _pipe;
        readonly PreInsertedSagaTracker _preInserted;
        readonly ReceivedMessageList _received;
        readonly SagaList<TSaga> _created;
        readonly SagaList<TSaga> _sagas;

        public InterceptPipe(SagaList<TSaga> sagas, ReceivedMessageList received, SagaList<TSaga> created,
            PreInsertedSagaTracker preInserted, IPipe<SagaConsumeContext<TSaga, TMessage>> pipe)
        {
            _sagas = sagas;
            _received = received;
            _created = created;
            _preInserted = preInserted;
            _pipe = pipe;
        }

        void IProbeSite.Probe(ProbeContext context)
        {
            _pipe.Probe(context);
        }

        public async Task Send(SagaConsumeContext<TSaga, TMessage> context)
        {
            // InsertOnInitial creates and inserts the saga before the policy's Existing branch
            // invokes this pipe. The older Missing branch is therefore never reached. Record the
            // instance here, after insertion produced a real consume context, so Created remains
            // truthful for both creation paths and never reports a failed pre-insert attempt.
            if (_preInserted.Value)
                _created.Add(context);

            _sagas.Add(context);

            try
            {
                await _pipe.Send(context).ConfigureAwait(false);

                _received.Add(context);
            }
            catch (Exception ex)
            {
                _received.Add(context, ex);
                throw;
            }
        }
    }


    class InterceptPolicy<TMessage> :
        ISagaPolicy<TSaga, TMessage>
        where TMessage : class
    {
        readonly SagaList<TSaga> _created;
        readonly ISagaPolicy<TSaga, TMessage> _policy;
        readonly PreInsertedSagaTracker _preInserted;

        public InterceptPolicy(SagaList<TSaga> created, PreInsertedSagaTracker preInserted, ISagaPolicy<TSaga, TMessage> policy)
        {
            _created = created;
            _preInserted = preInserted;
            _policy = policy;
        }

        public bool IsReadOnly => _policy.IsReadOnly;

        public bool PreInsertInstance(ConsumeContext<TMessage> context, out TSaga instance)
        {
            _preInserted.Value = _policy.PreInsertInstance(context, out instance);

            return _preInserted.Value;
        }

        public Task Existing(SagaConsumeContext<TSaga, TMessage> context, IPipe<SagaConsumeContext<TSaga, TMessage>> next)
        {
            return _policy.Existing(context, next);
        }

        public Task Missing(ConsumeContext<TMessage> context, IPipe<SagaConsumeContext<TSaga, TMessage>> next)
        {
            var interceptPipe = new InterceptPolicyPipe(_created, next);

            return _policy.Missing(context, interceptPipe);
        }


        class InterceptPolicyPipe :
            IPipe<SagaConsumeContext<TSaga, TMessage>>
        {
            readonly SagaList<TSaga> _created;
            readonly IPipe<SagaConsumeContext<TSaga, TMessage>> _pipe;

            public InterceptPolicyPipe(SagaList<TSaga> created, IPipe<SagaConsumeContext<TSaga, TMessage>> pipe)
            {
                _created = created;
                _pipe = pipe;
            }

            void IProbeSite.Probe(ProbeContext context)
            {
                _pipe.Probe(context);
            }

            public Task Send(SagaConsumeContext<TSaga, TMessage> context)
            {
                _created.Add(context);

                return _pipe.Send(context);
            }
        }
    }


    sealed class PreInsertedSagaTracker
    {
        public bool Value { get; set; }
    }
}
