using System.Collections.Generic;
using ViciOne.ServiceBus.RabbitMq.Configuration;

namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>Builds the RabbitMQ exchange and queue used to hold dead-lettered messages.</summary>
public class RabbitMqDeadLetterSettings :
    QueueBindingConfigurator,
    DeadLetterSettings
{
    /// <summary>Creates dead-letter settings derived from a receive endpoint.</summary>
    /// <param name="source">The source receive settings whose durability and declaration arguments are copied.</param>
    /// <param name="name">The dead-letter exchange and queue name.</param>
    public RabbitMqDeadLetterSettings(ReceiveSettings source, string name)
        : base(name, source.ExchangeType, source.Durable, source.AutoDelete)
    {
        QueueName = name;

        foreach (KeyValuePair<string, object?> argument in source.ExchangeArguments)
            SetExchangeArgument(argument.Key, argument.Value);

        foreach (KeyValuePair<string, object?> argument in source.QueueArguments)
            SetQueueArgument(argument.Key, argument.Value);
    }

    /// <summary>Builds the exchange, queue, and binding required by the dead-letter transport.</summary>
    /// <returns>The dead-letter broker topology.</returns>
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
