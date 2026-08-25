namespace ViciOne.ServiceBus.Testing.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading.Tasks;
    using Configuration;


    public class RegistrationSagaStateMachineTestHarness<TStateMachine, TInstance> :
        BaseSagaTestHarness<TInstance>,
        ISagaStateMachineTestHarness<TStateMachine, TInstance>
        where TInstance : class, SagaStateMachineInstance
        where TStateMachine : SagaStateMachine<TInstance>
    {
        public RegistrationSagaStateMachineTestHarness(ISagaRepositoryDecoratorRegistration<TInstance> registration,
            IQuerySagaRepository<TInstance> querySagaRepository, ILoadSagaRepository<TInstance> loadSagaRepository, TStateMachine stateMachine)
            : base(querySagaRepository, loadSagaRepository, registration.TestTimeout, registration.TimeProvider)
        {
            StateMachine = stateMachine;
            Consumed = registration.Consumed;
            Created = registration.Created;
            Sagas = registration.Sagas;
        }

        public IReceivedMessageList Consumed { get; }

        public ISagaList<TInstance> Sagas { get; }

        public ISagaList<TInstance> Created { get; }

        public TStateMachine StateMachine { get; }

        /// <summary>
        /// Waits until a saga exists with the specified correlationId in the specified state
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="stateSelector"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public Task<Guid?> Exists(Guid correlationId, Func<TStateMachine, State> stateSelector, TimeSpan? timeout = default)
        {
            var state = stateSelector(StateMachine);

            return Exists(correlationId, state, timeout);
        }

        /// <summary>
        /// Waits until a saga exists with the specified correlationId in the specified state
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="state">The expected state</param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public async Task<Guid?> Exists(Guid correlationId, State state, TimeSpan? timeout = default)
        {
            if (QuerySagaRepository == null)
                throw new InvalidOperationException("The repository does not support Query operations");

            ISagaQuery<TInstance> query = StateMachine.CreateSagaQuery(x => x.CorrelationId == correlationId, state);

            return await PollAsync(
                async () => (Guid?)(await QuerySagaRepository.Find(query).ConfigureAwait(false)).FirstOrDefault(),
                sagaId => sagaId.HasValue && sagaId.Value != Guid.Empty,
                default(Guid?),
                timeout).ConfigureAwait(false);
        }

        /// <summary>
        /// Waits until a saga exists with the specified correlationId in the specified state
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="stateSelector"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public Task<IList<Guid>> Exists(Expression<Func<TInstance, bool>> expression, Func<TStateMachine, State> stateSelector, TimeSpan? timeout = default)
        {
            var state = stateSelector(StateMachine);

            return Exists(expression, state, timeout);
        }

        /// <summary>
        /// Waits until a saga exists with the specified correlationId in the specified state
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="state">The expected state</param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public async Task<IList<Guid>> Exists(Expression<Func<TInstance, bool>> expression, State state, TimeSpan? timeout = default)
        {
            if (QuerySagaRepository == null)
                throw new InvalidOperationException("The repository does not support Query operations");

            ISagaQuery<TInstance> query = StateMachine.CreateSagaQuery(expression, state);

            return await PollAsync(
                async () => (IList<Guid>)(await QuerySagaRepository.Find(query).ConfigureAwait(false)).ToList(),
                sagas => sagas.Count > 0,
                default(IList<Guid>),
                timeout).ConfigureAwait(false);
        }
    }
}
