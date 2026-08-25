namespace ViciOne.ServiceBus.Testing.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading.Tasks;
    using Saga;


    public abstract class BaseSagaTestHarness<TSaga>
        where TSaga : class, ISaga
    {
        protected BaseSagaTestHarness(IQuerySagaRepository<TSaga> querySagaRepository, ILoadSagaRepository<TSaga> loadSagaRepository, TimeSpan testTimeout,
            TimeProvider timeProvider)
        {
            QuerySagaRepository = querySagaRepository;
            LoadSagaRepository = loadSagaRepository;

            TestTimeout = testTimeout;
            TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        }

        protected TimeSpan TestTimeout { get; }
        protected TimeProvider TimeProvider { get; }

        protected IQuerySagaRepository<TSaga> QuerySagaRepository { get; }
        protected ILoadSagaRepository<TSaga> LoadSagaRepository { get; }

        /// <summary>
        /// Waits until a saga exists with the specified correlationId
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public async Task<Guid?> Exists(Guid correlationId, TimeSpan? timeout = default)
        {
            if (LoadSagaRepository == null)
                throw new InvalidOperationException("The repository does not support Load operations");

            return await PollAsync(
                async () => (await LoadSagaRepository.Load(correlationId).ConfigureAwait(false))?.CorrelationId,
                sagaId => sagaId.HasValue,
                default(Guid?),
                timeout).ConfigureAwait(false);
        }

        /// <summary>
        /// Waits until at least one saga exists matching the specified filter
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public async Task<IList<Guid>> Match(Expression<Func<TSaga, bool>> filter, TimeSpan? timeout = default)
        {
            if (QuerySagaRepository == null)
                throw new InvalidOperationException("The repository does not support Query operations");

            var query = new SagaQuery<TSaga>(filter);

            return await PollAsync(
                async () => (IList<Guid>)(await QuerySagaRepository.Find(query).ConfigureAwait(false)).ToList(),
                sagas => sagas.Count > 0,
                new List<Guid>(),
                timeout).ConfigureAwait(false);
        }

        /// <summary>
        /// Waits until the saga matching the specified correlationId does NOT exist
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public async Task<Guid?> NotExists(Guid correlationId, TimeSpan? timeout = default)
        {
            if (LoadSagaRepository == null)
                throw new InvalidOperationException("The repository does not support Load operations");

            TSaga saga = await PollAsync(
                () => LoadSagaRepository.Load(correlationId),
                instance => instance == null,
                default(TSaga),
                timeout).ConfigureAwait(false);

            return saga?.CorrelationId;
        }

        protected async Task<TResult> PollAsync<TResult>(Func<Task<TResult>> probe, Func<TResult, bool> completed, TResult timeoutResult,
            TimeSpan? timeout = default)
        {
            var effectiveTimeout = timeout ?? TestTimeout;
            if (effectiveTimeout <= TimeSpan.Zero)
                return timeoutResult;

            var startedAt = TimeProvider.GetTimestamp();
            while (TimeProvider.GetElapsedTime(startedAt) < effectiveTimeout)
            {
                TResult result = await probe().ConfigureAwait(false);
                if (completed(result))
                    return result;

                await Task.Delay(TimeSpan.FromMilliseconds(10), TimeProvider).ConfigureAwait(false);
            }

            return timeoutResult;
        }
    }
}
