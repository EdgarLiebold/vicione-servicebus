using System;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Provides extension methods for active mq configure endpoint callback.
/// </summary>
public static class ActiveMqConfigureEndpointCallbackExtensions
{
    /// <summary>
    /// Add an ActiveMQ specific configure callback to the endpoint.
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="callback"></param>
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

    /// <summary>
    /// Add an ActiveMQ specific configure callback for configured endpoints
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="callback"></param>
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
