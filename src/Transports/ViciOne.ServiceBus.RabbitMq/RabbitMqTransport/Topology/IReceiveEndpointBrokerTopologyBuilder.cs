namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>Builds the queue, endpoint exchange, and optional message exchange bindings for one receive endpoint.</summary>
public interface IReceiveEndpointBrokerTopologyBuilder :
    IBrokerTopologyBuilder
{
    /// <summary>Gets the consuming queue handle.</summary>
    QueueHandle Queue { get; }

    /// <summary>Gets the receive endpoint exchange handle that is bound directly to the queue.</summary>
    ExchangeHandle Exchange { get; }

    /// <summary>Gets or sets the message exchange currently bound to the receive endpoint exchange.</summary>
    ExchangeHandle? BoundExchange { get; set; }
}
