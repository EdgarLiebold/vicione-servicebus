// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using EventHubIntegration;
    using Microsoft.Extensions.DependencyInjection;


    public static class AzureFunctionsEventHubConfigurationExtensions
    {
        /// <summary>
        /// Add Event Hub for ViciOne.ServiceBus on Azure Functions, which sits on top of Azure Service Bus, and configures
        /// <see cref="IEventReceiver" /> for use by functions to handle messages.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddViciOneServiceBusEventHub(this IServiceCollection services)
        {
            services.AddSingleton<IEventReceiver, EventReceiver>();

            return services;
        }
    }
}
