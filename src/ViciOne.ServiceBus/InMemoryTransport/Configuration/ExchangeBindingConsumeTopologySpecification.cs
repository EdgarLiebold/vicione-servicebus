using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports.Fabric;

#nullable enable
namespace ViciOne.ServiceBus.InMemoryTransport.Configuration;

/// <summary>
/// Used to bind an exchange to the consuming queue's exchange
/// </summary>
public class ExchangeBindingConsumeTopologySpecification :
    IInMemoryConsumeTopologySpecification
{
    readonly string _exchange;
    readonly ExchangeType _exchangeType;
    readonly string? _routingKey;

    public ExchangeBindingConsumeTopologySpecification(string exchange, ExchangeType exchangeType, string? routingKey)
    {
        _exchange = exchange;
        _exchangeType = exchangeType;
        _routingKey = routingKey;
    }

    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }

    public void Apply(IMessageFabricConsumeTopologyBuilder builder)
    {
        builder.ExchangeDeclare(_exchange, _exchangeType);
        builder.ExchangeBind(_exchange, builder.Exchange, _routingKey);
    }
}
