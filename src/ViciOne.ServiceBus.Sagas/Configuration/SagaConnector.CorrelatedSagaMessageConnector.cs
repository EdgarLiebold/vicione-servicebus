using System;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Connects a correlated saga message to its saga repository and message pipeline.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
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

        /// <summary>Initializes a new instance.</summary>
        /// <param name="consumeFilter">The consume filter.</param>
        /// <param name="policy">The policy.</param>
        /// <param name="correlationIdSelector">The correlation id selector.</param>
        public CorrelatedSagaMessageConnector(IFilter<SagaConsumeContext<TSaga, TMessage>> consumeFilter, ISagaPolicy<TSaga, TMessage> policy,
            Func<ConsumeContext<TMessage>, Guid> correlationIdSelector)
            : base(consumeFilter)
        {
            _policy = policy;
            _correlationIdSelector = correlationIdSelector;
        }

        /// <summary>Configures message pipe.</summary>
        /// <param name="configurator">The configurator to update.</param>
        /// <param name="repository">The repository.</param>
        /// <param name="sagaPipe">The saga pipe.</param>
        protected override void ConfigureMessagePipe(IPipeConfigurator<ConsumeContext<TMessage>> configurator, ISagaRepository<TSaga> repository,
            IPipe<SagaConsumeContext<TSaga, TMessage>> sagaPipe)
        {
            configurator.UseFilter(new CorrelationIdMessageFilter<TMessage>(_correlationIdSelector));
            configurator.UseFilter(new CorrelatedSagaFilter<TSaga, TMessage>(repository, _policy, sagaPipe));
        }
    }
}
