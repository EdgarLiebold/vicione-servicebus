using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides extension methods for service instance configuration.</summary>
public static class ServiceInstanceConfigurationExtensions
{
    /// <summary>Configure a service instance for use with the job service.</summary>
    /// <typeparam name="TEndpointConfigurator">The endpoint configurator type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public static void ServiceInstance<TEndpointConfigurator>(this IBusFactoryConfigurator<TEndpointConfigurator> configurator,
        Action<IServiceInstanceConfigurator<TEndpointConfigurator>> configure)
        where TEndpointConfigurator : IReceiveEndpointConfigurator
    {
        ServiceInstance(configurator, new ServiceInstanceOptions(), configure);
    }

    /// <summary>Configure a service instance for use with the job service.</summary>
    /// <typeparam name="TEndpointConfigurator">The endpoint configurator type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public static void ServiceInstance<TEndpointConfigurator>(this IBusFactoryConfigurator<TEndpointConfigurator> configurator,
        ServiceInstanceOptions options, Action<IServiceInstanceConfigurator<TEndpointConfigurator>> configure)
        where TEndpointConfigurator : IReceiveEndpointConfigurator
    {
        var definition = new InstanceEndpointDefinition();

        configurator.ReceiveEndpoint(definition, options.EndpointNameFormatter, endpointConfigurator =>
        {
            var instanceConfigurator = new ServiceInstanceConfigurator<TEndpointConfigurator>(configurator, options, endpointConfigurator);

            configure?.Invoke(instanceConfigurator);
        });
    }
}
