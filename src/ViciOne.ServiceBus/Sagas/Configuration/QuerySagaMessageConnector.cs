using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

public partial class SagaConnector<TSaga, TMessage>
    where TSaga : class, ISaga
    where TMessage : class
{
    /// <summary>Connects query saga message to the service bus pipeline.</summary>
    public class QuerySagaMessageConnector :
        SagaMessageConnector
    {
        readonly ISagaPolicy<TSaga, TMessage> _policy;
        readonly ISagaQueryFactory<TSaga, TMessage> _queryFactory;

        /// <summary>Initializes a new instance.</summary>
        /// <param name="consumeFilter">The consume filter.</param>
        /// <param name="policy">The policy.</param>
        /// <param name="queryFactory">The query factory.</param>
        public QuerySagaMessageConnector(IFilter<SagaConsumeContext<TSaga, TMessage>> consumeFilter, ISagaPolicy<TSaga, TMessage> policy,
            ISagaQueryFactory<TSaga, TMessage> queryFactory)
            : base(consumeFilter)
        {
            _policy = policy;
            _queryFactory = queryFactory;
        }

        /// <summary>Configures message pipe.</summary>
        /// <param name="configurator">The configurator to update.</param>
        /// <param name="repository">The repository.</param>
        /// <param name="sagaPipe">The saga pipe.</param>
        protected override void ConfigureMessagePipe(IPipeConfigurator<ConsumeContext<TMessage>> configurator, ISagaRepository<TSaga> repository,
            IPipe<SagaConsumeContext<TSaga, TMessage>> sagaPipe)
        {
            configurator.UseFilter(new QuerySagaFilter<TSaga, TMessage>(repository, _policy, _queryFactory, sagaPipe));
        }
    }
}
