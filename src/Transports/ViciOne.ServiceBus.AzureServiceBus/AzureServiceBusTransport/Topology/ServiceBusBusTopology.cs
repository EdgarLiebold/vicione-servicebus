using ViciOne.ServiceBus.AzureServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>Exposes Azure Service Bus send and publish topology through a bus topology.</summary>
public class ServiceBusBusTopology :
    BusTopology,
    IServiceBusBusTopology
{
    readonly IServiceBusTopologyConfiguration _configuration;
    readonly IServiceBusHostConfiguration _hostConfiguration;

    /// <summary>Creates a bus topology over the host and provider topology configuration.</summary>
    /// <param name="hostConfiguration">The Azure Service Bus host configuration.</param>
    /// <param name="configuration">The send, publish, and consume topology configuration.</param>
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
