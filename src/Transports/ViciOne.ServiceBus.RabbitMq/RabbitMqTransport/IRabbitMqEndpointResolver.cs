using RabbitMQ.Client;
using ViciOne.ServiceBus.RabbitMq.Configuration;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Resolves RabbitMQ cluster endpoints and exposes the last selected node.</summary>
public interface IRabbitMqEndpointResolver :
    IEndpointResolver
{
    /// <summary>Returns the last host selected by the selector.</summary>
    ClusterNode LastHost { get; }
}
