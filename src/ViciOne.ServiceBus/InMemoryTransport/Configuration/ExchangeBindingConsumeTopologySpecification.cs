using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.InMemoryTransport.Configuration;

/// <summary>Used to bind an exchange to the consuming queue's exchange.</summary>
public class ExchangeBindingConsumeTopologySpecification :
    IInMemoryConsumeTopologySpecification
{
    readonly string _exchange;
    readonly ExchangeType _exchangeType;
    readonly string? _routingKey;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="exchange">The exchange.</param>
    /// <param name="exchangeType">The runtime exchange type used by the operation.</param>
    /// <param name="routingKey">The routing key.</param>
    public ExchangeBindingConsumeTopologySpecification(string exchange, ExchangeType exchangeType, string? routingKey)
    {
        _exchange = exchange;
        _exchangeType = exchangeType;
        _routingKey = routingKey;
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(IMessageFabricConsumeTopologyBuilder builder)
    {
        builder.ExchangeDeclare(_exchange, _exchangeType);
        builder.ExchangeBind(_exchange, builder.Exchange, _routingKey);
    }
}
