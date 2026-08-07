// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    /// <summary>
    /// The configuration of a bus
    /// </summary>
    public interface IBusConfiguration :
        IEndpointConfiguration,
        IBusObserverConnector,
        IEndpointConfigurationObserverConnector
    {
        IHostConfiguration HostConfiguration { get; }

        IEndpointConfiguration BusEndpointConfiguration { get; }

        IBusObserver BusObservers { get; }
    }
}
