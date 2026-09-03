#nullable enable
namespace ViciOne.ServiceBus.Configuration
{
    using System;
    using System.Collections.Generic;
    using Middleware;


    /// <summary>
    /// Adds exactly one first-class consumer-concurrency gate to a consumed message pipeline.
    /// </summary>
    internal sealed class ConsumerConcurrencyPipeSpecification<TMessage> :
        IPipeSpecification<ConsumeContext<TMessage>>
        where TMessage : class
    {
        readonly IConsumerConcurrencyGate<TMessage> _gate;
        readonly ConsumerConcurrencyPolicy _policy;

        public ConsumerConcurrencyPipeSpecification(
            IConsumerConcurrencyGate<TMessage> gate,
            ConsumerConcurrencyPolicy policy)
        {
            _gate = gate ?? throw new ArgumentNullException(nameof(gate));
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        }

        public void Apply(IPipeBuilder<ConsumeContext<TMessage>> builder)
        {
            builder.AddFilter(new ConsumerConcurrencyFilter<TMessage>(_gate, _policy));
        }

        public IEnumerable<ValidationResult> Validate()
        {
            yield break;
        }
    }
}
