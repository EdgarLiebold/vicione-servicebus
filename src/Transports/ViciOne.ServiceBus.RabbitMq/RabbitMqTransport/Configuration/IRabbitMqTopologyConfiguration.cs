using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>Combines RabbitMQ publish, send, and consume topology configuration.</summary>
public interface IRabbitMqTopologyConfiguration :
    ITopologyConfiguration
{
    /// <summary>Gets publish topology across message contracts.</summary>
    new IRabbitMqPublishTopologyConfigurator Publish { get; }

    /// <summary>Gets send topology and generated fault-queue conventions.</summary>
    new IRabbitMqSendTopologyConfigurator Send { get; }

    /// <summary>Gets consume topology for the endpoint under construction.</summary>
    new IRabbitMqConsumeTopologyConfigurator Consume { get; }
}
