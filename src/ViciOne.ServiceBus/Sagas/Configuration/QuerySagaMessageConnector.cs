using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a saga connector implementation.
/// </summary>
public partial class SagaConnector<TSaga, TMessage>
    where TSaga : class, ISaga
    where TMessage : class
{
    /// <summary>
    /// Provides a query saga message connector implementation.
    /// </summary>
    public class QuerySagaMessageConnector :
        SagaMessageConnector
    {
        readonly ISagaPolicy<TSaga, TMessage> _policy;
        readonly ISagaQueryFactory<TSaga, TMessage> _queryFactory;

        /// <summary>
        /// Initializes a new instance of the containing type.
        /// </summary>
        /// <param name="consumeFilter">The consume filter value.</param>
        /// <param name="policy">The policy value.</param>
        /// <param name="queryFactory">The query factory value.</param>
        public QuerySagaMessageConnector(IFilter<SagaConsumeContext<TSaga, TMessage>> consumeFilter, ISagaPolicy<TSaga, TMessage> policy,
            ISagaQueryFactory<TSaga, TMessage> queryFactory)
            : base(consumeFilter)
        {
            _policy = policy;
            _queryFactory = queryFactory;
        }

        /// <summary>
        /// Configures message pipe.
        /// </summary>
        /// <param name="configurator">The configurator value.</param>
        /// <param name="repository">The repository value.</param>
        /// <param name="sagaPipe">The saga pipe value.</param>
        protected override void ConfigureMessagePipe(IPipeConfigurator<ConsumeContext<TMessage>> configurator, ISagaRepository<TSaga> repository,
            IPipe<SagaConsumeContext<TSaga, TMessage>> sagaPipe)
        {
            configurator.UseFilter(new QuerySagaFilter<TSaga, TMessage>(repository, _policy, _queryFactory, sagaPipe));
        }
    }
}
