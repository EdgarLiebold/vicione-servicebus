// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    public interface IEndpointConfigurationObserver
    {
        /// <summary>
        /// Called when an endpoint is configured
        /// </summary>
        /// <typeparam name="T">The receive endpoint configurator type</typeparam>
        /// <param name="configurator"></param>
        void EndpointConfigured<T>(T configurator)
            where T : IReceiveEndpointConfigurator;
    }
}
