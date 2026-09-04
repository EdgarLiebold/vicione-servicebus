using System;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Provides extension methods for service bus configure endpoint callback.
/// </summary>
public static class ServiceBusConfigureEndpointCallbackExtensions
{
    /// <summary>
    /// Add an Azure Service Bus specific configure callback to the endpoint.
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="callback"></param>
    public static void AddServiceBusConfigureEndpointCallback(this IEndpointRegistrationConfigurator configurator,
        Action<IRegistrationContext, IServiceBusReceiveEndpointConfigurator> callback)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        configurator.AddConfigureEndpointCallback((context, cfg) =>
        {
            if (cfg is IServiceBusReceiveEndpointConfigurator sb)
                callback(context, sb);
        });
    }

    /// <summary>
    /// Add an Azure Service Bus specific configure callback for configured endpoints
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="callback"></param>
    public static void AddServiceBusConfigureEndpointsCallback(this IBusRegistrationConfigurator configurator, ServiceBusConfigureEndpointsCallback callback)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        configurator.AddConfigureEndpointsCallback((context, name, cfg) =>
        {
            if (cfg is IServiceBusReceiveEndpointConfigurator sb)
            {
                callback(context, name
                    ?? throw new InvalidOperationException("A configured Azure Service Bus endpoint must have a name."), sb);
            }
        });
    }
}
