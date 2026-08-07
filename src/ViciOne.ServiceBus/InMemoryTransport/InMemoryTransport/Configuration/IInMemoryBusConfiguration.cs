// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.InMemoryTransport.Configuration
{
    using ViciOne.ServiceBus.Configuration;


    public interface IInMemoryBusConfiguration :
        IBusConfiguration,
        IInMemoryEndpointConfiguration
    {
        new IInMemoryHostConfiguration HostConfiguration { get; }

        new IInMemoryEndpointConfiguration BusEndpointConfiguration { get; }

        /// <summary>
        /// Create an endpoint configuration on the bus, which can later be turned into a receive endpoint
        /// </summary>
        /// <returns></returns>
        IInMemoryEndpointConfiguration CreateEndpointConfiguration(bool isBusEndpoint = false);
    }
}
