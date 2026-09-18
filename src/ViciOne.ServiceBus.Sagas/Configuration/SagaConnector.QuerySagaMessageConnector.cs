using System;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

public partial class SagaConnector<TSaga, TMessage>
    where TSaga : class, ISaga
    where TMessage : class
{
    /// <summary>Dispatches messages to saga instances selected by a query factory.</summary>
    public class QuerySagaMessageConnector :
        SagaMessageConnector
    {
        readonly ISagaPolicy<TSaga, TMessage> _policy;
        readonly ISagaQueryFactory<TSaga, TMessage> _queryFactory;

        /// <summary>Associates the saga consume filter, repository policy and query factory.</summary>
        /// <param name="consumeFilter">The filter appended to the saga consume pipeline.</param>
        /// <param name="policy">The policy governing existing and missing saga instances.</param>
        /// <param name="queryFactory">The factory creating a saga query from each message context.</param>
        public QuerySagaMessageConnector(IFilter<SagaConsumeContext<TSaga, TMessage>> consumeFilter, ISagaPolicy<TSaga, TMessage> policy,
            ISagaQueryFactory<TSaga, TMessage> queryFactory)
            : base(consumeFilter)
        {
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
            _queryFactory = queryFactory ?? throw new ArgumentNullException(nameof(queryFactory));
        }

        /// <summary>Appends saga repository dispatch using the configured query factory.</summary>
        /// <param name="configurator">The message pipeline receiving the query filter.</param>
        /// <param name="repository">The repository locating matching saga instances.</param>
        /// <param name="sagaPipe">The pipeline invoked with each matched saga context.</param>
        protected override void ConfigureMessagePipe(IPipeConfigurator<ConsumeContext<TMessage>> configurator, ISagaRepository<TSaga> repository,
            IPipe<SagaConsumeContext<TSaga, TMessage>> sagaPipe)
        {
            configurator.UseFilter(new QuerySagaFilter<TSaga, TMessage>(repository, _policy, _queryFactory, sagaPipe));
        }
    }
}
