using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.InMemoryTransport.Configuration;

/// <summary>Declares an exchange and binds it to a receive endpoint's exchange.</summary>
internal sealed class ExchangeBindingConsumeTopologySpecification :
    IInMemoryConsumeTopologySpecification
{
    readonly string _exchange;
    readonly ExchangeType _exchangeType;
    readonly string? _routingKey;

    /// <summary>Creates a validated consume-topology binding.</summary>
    /// <param name="exchange">The source exchange name.</param>
    /// <param name="exchangeType">The source exchange routing behavior.</param>
    /// <param name="routingKey">The optional direct or topic routing key.</param>
    public ExchangeBindingConsumeTopologySpecification(string exchange, ExchangeType exchangeType, string? routingKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(exchange);
        if (!Enum.IsDefined(exchangeType))
            throw new ArgumentOutOfRangeException(nameof(exchangeType), exchangeType, "The exchange type is not supported.");
        if (exchangeType == ExchangeType.FanOut && routingKey is not null)
            throw new ArgumentException("A fan-out exchange does not accept a routing key.", nameof(routingKey));

        _exchange = exchange;
        _exchangeType = exchangeType;
        _routingKey = routingKey;
    }

    /// <summary>Returns no failures because construction validates every binding value.</summary>
    /// <returns>An empty validation sequence.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }

    /// <summary>Declares the source exchange and binds it to the builder's endpoint exchange.</summary>
    /// <param name="builder">The consume topology builder to update.</param>
    public void Apply(IMessageFabricConsumeTopologyBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ExchangeDeclare(_exchange, _exchangeType);
        builder.ExchangeBind(_exchange, builder.Exchange, _routingKey);
    }
}
