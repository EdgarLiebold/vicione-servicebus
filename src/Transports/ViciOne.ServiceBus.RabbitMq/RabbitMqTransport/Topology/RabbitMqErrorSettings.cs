using System.Collections.Generic;
using ViciOne.ServiceBus.RabbitMq.Configuration;

namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>
/// Provides a rabbit mq error settings implementation.
/// </summary>
public class RabbitMqErrorSettings :
    QueueBindingConfigurator,
    ErrorSettings
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="source">The source value.</param>
    /// <param name="name">The name value.</param>
    public RabbitMqErrorSettings(ReceiveSettings source, string name)
        : base(name, source.ExchangeType, source.Durable, source.AutoDelete)
    {
        QueueName = name;

        foreach (KeyValuePair<string, object?> argument in source.ExchangeArguments)
            SetExchangeArgument(argument.Key, argument.Value);

        foreach (KeyValuePair<string, object?> argument in source.QueueArguments)
            SetQueueArgument(argument.Key, argument.Value);
    }

    /// <summary>
    /// Gets broker topology.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public BrokerTopology GetBrokerTopology()
    {
        var builder = new PublishEndpointBrokerTopologyBuilder();

        var exchange = builder.ExchangeDeclare(ExchangeName, ExchangeType, Durable, AutoDelete, ExchangeArguments);
        builder.Exchange = exchange;

        var queue = builder.QueueDeclare(QueueName, Durable, !QueueExpiration.HasValue && AutoDelete, false, QueueArguments);

        builder.QueueBind(exchange, queue, RoutingKey, BindingArguments);

        return builder.BuildBrokerTopology();
    }
}
