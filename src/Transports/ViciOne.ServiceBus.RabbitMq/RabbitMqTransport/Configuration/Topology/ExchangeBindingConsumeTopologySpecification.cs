using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.RabbitMq.Topology;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>
/// Used to bind an exchange to the consuming queue's exchange
/// </summary>
public class ExchangeBindingConsumeTopologySpecification :
    RabbitMqExchangeBindingConfigurator,
    IRabbitMqExchangeToExchangeBindingConfigurator,
    IRabbitMqConsumeTopologySpecification
{
    readonly List<IRabbitMqConsumeTopologySpecification> _specifications;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="exchangeName">The exchange name value.</param>
    /// <param name="exchangeType">The exchange type value.</param>
    /// <param name="durable">The durable value.</param>
    /// <param name="autoDelete">The auto delete value.</param>
    public ExchangeBindingConsumeTopologySpecification(string exchangeName, string exchangeType, bool durable = true, bool autoDelete = false)
        : base(exchangeName, exchangeType, durable, autoDelete)
    {
        _specifications = new List<IRabbitMqConsumeTopologySpecification>();
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="exchange">The exchange value.</param>
    public ExchangeBindingConsumeTopologySpecification(Exchange exchange)
        : base(exchange)
    {
        _specifications = new List<IRabbitMqConsumeTopologySpecification>();
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(IReceiveEndpointBrokerTopologyBuilder builder)
    {
        var exchangeHandle = builder.ExchangeDeclare(ExchangeName, ExchangeType, Durable, AutoDelete, ExchangeArguments);

        builder.ExchangeBind(exchangeHandle, builder.Exchange, RoutingKey, BindingArguments);

        builder.BoundExchange = exchangeHandle;

        foreach (var specification in _specifications)
            specification.Apply(builder);
    }

    /// <summary>
    /// Performs the bind operation.
    /// </summary>
    /// <param name="exchangeName">The exchange name value.</param>
    /// <param name="configure">The configuration callback.</param>
    public void Bind(string exchangeName, Action<IRabbitMqExchangeToExchangeBindingConfigurator>? configure)
    {
        if (string.IsNullOrWhiteSpace(exchangeName))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(exchangeName));

        var specification =
            new ExchangeToExchangeBindingConsumeTopologySpecification(exchangeName, ExchangeType, Durable, AutoDelete) { RoutingKey = RoutingKey };

        configure?.Invoke(specification);

        _specifications.Add(specification);
    }
}
