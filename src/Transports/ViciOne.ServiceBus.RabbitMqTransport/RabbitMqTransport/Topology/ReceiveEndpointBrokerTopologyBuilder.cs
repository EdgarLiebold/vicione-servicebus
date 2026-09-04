using System;

namespace ViciOne.ServiceBus.RabbitMqTransport.Topology;

public class ReceiveEndpointBrokerTopologyBuilder :
    BrokerTopologyBuilder,
    IReceiveEndpointBrokerTopologyBuilder
{
    QueueHandle? _queue;
    ExchangeHandle? _exchange;

    public QueueHandle Queue
    {
        get => _queue ?? throw new InvalidOperationException("The receive queue has not been declared.");
        set => _queue = value;
    }

    public ExchangeHandle Exchange
    {
        get => _exchange ?? throw new InvalidOperationException("The receive exchange has not been declared.");
        set => _exchange = value;
    }

    public ExchangeHandle? BoundExchange { get; set; }
}
