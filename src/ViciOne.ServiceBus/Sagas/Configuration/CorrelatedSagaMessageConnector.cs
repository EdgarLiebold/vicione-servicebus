using System;
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
    /// Connects a message that has an exact CorrelationId to the saga instance
    /// to the saga repository.
    /// </summary>
    public class CorrelatedSagaMessageConnector :
        SagaMessageConnector
    {
        readonly Func<ConsumeContext<TMessage>, Guid> _correlationIdSelector;
        readonly ISagaPolicy<TSaga, TMessage> _policy;

        /// <summary>
        /// Initializes a new instance of the containing type.
        /// </summary>
        /// <param name="consumeFilter">The consume filter value.</param>
        /// <param name="policy">The policy value.</param>
        /// <param name="correlationIdSelector">The correlation id selector value.</param>
        public CorrelatedSagaMessageConnector(IFilter<SagaConsumeContext<TSaga, TMessage>> consumeFilter, ISagaPolicy<TSaga, TMessage> policy,
            Func<ConsumeContext<TMessage>, Guid> correlationIdSelector)
            : base(consumeFilter)
        {
            _policy = policy;
            _correlationIdSelector = correlationIdSelector;
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
            configurator.UseFilter(new CorrelationIdMessageFilter<TMessage>(_correlationIdSelector));
            configurator.UseFilter(new CorrelatedSagaFilter<TSaga, TMessage>(repository, _policy, sagaPipe));
        }
    }
}
