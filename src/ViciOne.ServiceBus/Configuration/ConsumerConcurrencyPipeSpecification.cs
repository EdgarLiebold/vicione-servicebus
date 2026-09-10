using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Adds exactly one first-class consumer-concurrency gate to a consumed message pipeline.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
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
