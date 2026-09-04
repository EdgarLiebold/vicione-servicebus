using RabbitMQ.Client;
using ViciOne.ServiceBus.RabbitMq.Configuration;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Defines the contract for rabbit mq endpoint resolver.
/// </summary>
public interface IRabbitMqEndpointResolver :
    IEndpointResolver
{
    /// <summary>
    /// Returns the last host selected by the selector
    /// </summary>
    ClusterNode LastHost { get; }
}
