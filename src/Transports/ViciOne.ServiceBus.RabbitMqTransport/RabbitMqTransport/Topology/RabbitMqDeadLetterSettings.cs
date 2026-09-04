using System.Collections.Generic;
using ViciOne.ServiceBus.RabbitMqTransport.Configuration;

namespace ViciOne.ServiceBus.RabbitMqTransport.Topology;

public class RabbitMqDeadLetterSettings :
    QueueBindingConfigurator,
    DeadLetterSettings
{
    public RabbitMqDeadLetterSettings(ReceiveSettings source, string name)
        : base(name, source.ExchangeType, source.Durable, source.AutoDelete)
    {
        QueueName = name;

        foreach (KeyValuePair<string, object?> argument in source.ExchangeArguments)
            SetExchangeArgument(argument.Key, argument.Value);

        foreach (KeyValuePair<string, object?> argument in source.QueueArguments)
            SetQueueArgument(argument.Key, argument.Value);
    }

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
