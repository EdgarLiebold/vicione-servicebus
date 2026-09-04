using RabbitMQ.Client;
using ViciOne.ServiceBus.RabbitMqTransport.Configuration;

namespace ViciOne.ServiceBus.RabbitMqTransport;

public interface IRabbitMqEndpointResolver :
    IEndpointResolver
{
    /// <summary>
    /// Returns the last host selected by the selector
    /// </summary>
    ClusterNode LastHost { get; }
}
