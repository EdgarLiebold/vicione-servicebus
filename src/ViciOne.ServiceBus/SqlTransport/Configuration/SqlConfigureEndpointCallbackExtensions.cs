using System;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Provides extension methods for sql configure endpoint callback.</summary>
public static class SqlConfigureEndpointCallbackExtensions
{
    /// <summary>Add a SQL specific configure callback to the endpoint.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    public static void AddSqlConfigureEndpointCallback(this IEndpointRegistrationConfigurator configurator,
        Action<IRegistrationContext, ISqlReceiveEndpointConfigurator> callback)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        configurator.AddConfigureEndpointCallback((context, cfg) =>
        {
            if (cfg is ISqlReceiveEndpointConfigurator sb)
                callback(context, sb);
        });
    }

    /// <summary>Add a SQL specific configure callback for configured endpoints.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    public static void AddSqlConfigureEndpointsCallback(this IBusRegistrationConfigurator configurator, SqlConfigureEndpointsCallback callback)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        configurator.AddConfigureEndpointsCallback((context, name, cfg) =>
        {
            if (cfg is ISqlReceiveEndpointConfigurator sb)
                callback(context, name, sb);
        });
    }
}
