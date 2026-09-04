using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.RabbitMq.Topology;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>
/// Used to bind an exchange to the consuming queue's exchange
/// </summary>
public class ExchangeToExchangeBindingConsumeTopologySpecification :
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
    public ExchangeToExchangeBindingConsumeTopologySpecification(string exchangeName, string exchangeType, bool durable = true, bool autoDelete = false)
        : base(exchangeName, exchangeType, durable, autoDelete)
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
        if (builder.BoundExchange == null)
            throw new ArgumentException("The builder should have an already bound exchange", nameof(builder));

        // save this, since it must be restored on exit
        var boundExchange = builder.BoundExchange;

        var exchangeHandle = builder.ExchangeDeclare(ExchangeName, ExchangeType, Durable, AutoDelete, ExchangeArguments);

        builder.ExchangeBind(exchangeHandle, boundExchange, RoutingKey, BindingArguments);

        builder.BoundExchange = exchangeHandle;

        foreach (var specification in _specifications)
            specification.Apply(builder);

        builder.BoundExchange = boundExchange;
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
