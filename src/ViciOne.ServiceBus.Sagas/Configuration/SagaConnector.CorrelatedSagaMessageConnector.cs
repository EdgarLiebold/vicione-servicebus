using System;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Groups saga-message connectors and specifications for a closed saga/message pair.</summary>
/// <typeparam name="TSaga">The saga state handled by the nested connectors and specifications.</typeparam>
/// <typeparam name="TMessage">The message contract handled by the nested connectors and specifications.</typeparam>
public partial class SagaConnector<TSaga, TMessage>
    where TSaga : class, ISaga
    where TMessage : class
{
    /// <summary>
    /// Assigns a message correlation identifier and dispatches through the correlated saga repository filter.
    /// </summary>
    public class CorrelatedSagaMessageConnector :
        SagaMessageConnector
    {
        readonly Func<ConsumeContext<TMessage>, Guid> _correlationIdSelector;
        readonly ISagaPolicy<TSaga, TMessage> _policy;

        /// <summary>Associates the saga consume filter, repository policy and message-identifier selector.</summary>
        /// <param name="consumeFilter">The filter appended to the saga consume pipeline.</param>
        /// <param name="policy">The policy governing existing and missing saga instances.</param>
        /// <param name="correlationIdSelector">The selector assigning the message's saga correlation identifier.</param>
        public CorrelatedSagaMessageConnector(IFilter<SagaConsumeContext<TSaga, TMessage>> consumeFilter, ISagaPolicy<TSaga, TMessage> policy,
            Func<ConsumeContext<TMessage>, Guid> correlationIdSelector)
            : base(consumeFilter)
        {
            _policy = policy;
            _correlationIdSelector = correlationIdSelector;
        }

        /// <summary>Appends identifier selection followed by correlated saga repository dispatch.</summary>
        /// <param name="configurator">The message pipeline receiving both filters.</param>
        /// <param name="repository">The repository locating the selected saga instance.</param>
        /// <param name="sagaPipe">The pipeline invoked with the located or created saga context.</param>
        protected override void ConfigureMessagePipe(IPipeConfigurator<ConsumeContext<TMessage>> configurator, ISagaRepository<TSaga> repository,
            IPipe<SagaConsumeContext<TSaga, TMessage>> sagaPipe)
        {
            configurator.UseFilter(new CorrelationIdMessageFilter<TMessage>(_correlationIdSelector));
            configurator.UseFilter(new CorrelatedSagaFilter<TSaga, TMessage>(repository, _policy, sagaPipe));
        }
    }
}
