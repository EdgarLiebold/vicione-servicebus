using System;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Registers callbacks that run only for RabbitMQ receive endpoints.</summary>
public static class RabbitMqConfigureEndpointCallbackExtensions
{
    /// <summary>Adds a RabbitMQ-specific callback to one endpoint registration.</summary>
    /// <param name="configurator">The endpoint registration configurator.</param>
    /// <param name="callback">The callback invoked when the registered endpoint uses RabbitMQ.</param>
    public static void AddRabbitMqConfigureEndpointCallback(this IEndpointRegistrationConfigurator configurator,
        Action<IRegistrationContext, IRabbitMqReceiveEndpointConfigurator> callback)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        configurator.AddConfigureEndpointCallback((context, cfg) =>
        {
            if (cfg is IRabbitMqReceiveEndpointConfigurator rmq)
                callback(context, rmq);
        });
    }

    /// <summary>Adds a RabbitMQ-specific callback for every configured receive endpoint.</summary>
    /// <param name="configurator">The bus registration configurator.</param>
    /// <param name="callback">The callback invoked for each RabbitMQ receive endpoint.</param>
    public static void AddRabbitMqConfigureEndpointsCallback(this IBusRegistrationConfigurator configurator, RabbitMqConfigureEndpointsCallback callback)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        configurator.AddConfigureEndpointsCallback((context, name, cfg) =>
        {
            if (cfg is IRabbitMqReceiveEndpointConfigurator rmq)
                callback(context, name, rmq);
        });
    }
}
