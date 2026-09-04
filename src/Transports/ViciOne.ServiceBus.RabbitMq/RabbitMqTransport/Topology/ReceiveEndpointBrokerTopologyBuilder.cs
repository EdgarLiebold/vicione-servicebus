using System;

namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>
/// Provides a receive endpoint broker topology builder implementation.
/// </summary>
public class ReceiveEndpointBrokerTopologyBuilder :
    BrokerTopologyBuilder,
    IReceiveEndpointBrokerTopologyBuilder
{
    QueueHandle? _queue;
    ExchangeHandle? _exchange;

    /// <summary>
    /// Gets or sets the queue value.
    /// </summary>
    public QueueHandle Queue
    {
        get => _queue ?? throw new InvalidOperationException("The receive queue has not been declared.");
        set => _queue = value;
    }

    /// <summary>
    /// Gets or sets the exchange value.
    /// </summary>
    public ExchangeHandle Exchange
    {
        get => _exchange ?? throw new InvalidOperationException("The receive exchange has not been declared.");
        set => _exchange = value;
    }

    /// <summary>
    /// Gets or sets the bound exchange value.
    /// </summary>
    public ExchangeHandle? BoundExchange { get; set; }
}
