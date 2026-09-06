using System;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Registers Azure Service Bus-specific endpoint configuration callbacks.</summary>
public static class ServiceBusConfigureEndpointCallbackExtensions
{
    /// <summary>Adds a callback that runs only when the configured endpoint uses Azure Service Bus.</summary>
    /// <param name="configurator">The endpoint registration to update.</param>
    /// <param name="callback">The callback that receives the registration context and provider-specific configurator.</param>
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

    /// <summary>Adds a callback for every named endpoint that uses Azure Service Bus.</summary>
    /// <param name="configurator">The bus registration to update.</param>
    /// <param name="callback">The callback that receives the registration context, queue name, and provider-specific configurator.</param>
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
