using System;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Adds ActiveMQ-specific callbacks to endpoint registration.</summary>
public static class ActiveMqConfigureEndpointCallbackExtensions
{
    /// <summary>Adds a callback that runs when this registration is configured as an ActiveMQ endpoint.</summary>
    /// <param name="configurator">The endpoint registration configurator.</param>
    /// <param name="callback">The ActiveMQ-specific endpoint callback.</param>
    public static void AddActiveMqConfigureEndpointCallback(this IEndpointRegistrationConfigurator configurator,
        Action<IRegistrationContext, IActiveMqReceiveEndpointConfigurator> callback)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        configurator.AddConfigureEndpointCallback((context, cfg) =>
        {
            if (cfg is IActiveMqReceiveEndpointConfigurator sb)
                callback(context, sb);
        });
    }

    /// <summary>Adds a callback that runs for every configured ActiveMQ endpoint.</summary>
    /// <param name="configurator">The bus registration configurator.</param>
    /// <param name="callback">The ActiveMQ-specific configured-endpoint callback.</param>
    public static void AddActiveMqConfigureEndpointsCallback(this IBusRegistrationConfigurator configurator, ActiveMqConfigureEndpointsCallback callback)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        configurator.AddConfigureEndpointsCallback((context, name, cfg) =>
        {
            if (cfg is IActiveMqReceiveEndpointConfigurator sb)
            {
                callback(context, name
                    ?? throw new InvalidOperationException("A configured ActiveMQ endpoint must have a name."), sb);
            }
        });
    }
}
