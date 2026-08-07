// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.ActiveMqTransport.Configuration
{
    using ViciOne.ServiceBus.Configuration;


    public interface IActiveMqBusConfiguration :
        IBusConfiguration
    {
        new IActiveMqHostConfiguration HostConfiguration { get; }

        new IActiveMqEndpointConfiguration BusEndpointConfiguration { get; }

        new IActiveMqTopologyConfiguration Topology { get; }

        /// <summary>
        /// Create an endpoint configuration on the bus, which can later be turned into a receive endpoint
        /// </summary>
        /// <returns></returns>
        IActiveMqEndpointConfiguration CreateEndpointConfiguration(bool isBusEndpoint = false);
    }
}
