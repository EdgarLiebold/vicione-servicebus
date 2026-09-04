using ViciOne.ServiceBus.AzureServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>
/// Provides a service bus bus topology implementation.
/// </summary>
public class ServiceBusBusTopology :
    BusTopology,
    IServiceBusBusTopology
{
    readonly IServiceBusTopologyConfiguration _configuration;
    readonly IServiceBusHostConfiguration _hostConfiguration;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="configuration">The configuration callback.</param>
    public ServiceBusBusTopology(IServiceBusHostConfiguration hostConfiguration, IServiceBusTopologyConfiguration configuration)
        : base(hostConfiguration, configuration)
    {
        _hostConfiguration = hostConfiguration;
        _configuration = configuration;
    }

    IServiceBusPublishTopology IServiceBusBusTopology.PublishTopology => _configuration.Publish;
    IServiceBusSendTopology IServiceBusBusTopology.SendTopology => _configuration.Send;

    IServiceBusMessagePublishTopology<T> IServiceBusBusTopology.Publish<T>()
    {
        return _configuration.Publish.GetMessageTopology<T>();
    }

    IServiceBusMessageSendTopology<T> IServiceBusBusTopology.Send<T>()
    {
        return _configuration.Send.GetMessageTopology<T>();
    }
}
