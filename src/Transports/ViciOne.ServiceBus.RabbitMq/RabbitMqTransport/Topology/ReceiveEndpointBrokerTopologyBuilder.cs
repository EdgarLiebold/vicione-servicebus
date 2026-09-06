using System;

namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>Builds a receive endpoint's queue, endpoint exchange, and message exchange bindings.</summary>
public class ReceiveEndpointBrokerTopologyBuilder :
    BrokerTopologyBuilder,
    IReceiveEndpointBrokerTopologyBuilder
{
    QueueHandle? _queue;
    ExchangeHandle? _exchange;

    /// <summary>Gets or sets the receive queue handle.</summary>
    public QueueHandle Queue
    {
        get => _queue ?? throw new InvalidOperationException("The receive queue has not been declared.");
        set => _queue = value;
    }

    /// <summary>Gets or sets the receive endpoint exchange handle.</summary>
    public ExchangeHandle Exchange
    {
        get => _exchange ?? throw new InvalidOperationException("The receive exchange has not been declared.");
        set => _exchange = value;
    }

    /// <summary>Gets or sets the message exchange currently bound to the receive endpoint exchange.</summary>
    public ExchangeHandle? BoundExchange { get; set; }
}
