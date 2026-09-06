using System;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Registers Amazon SQS-specific callbacks for dependency-injection endpoint configuration.</summary>
public static class AmazonSqsConfigureEndpointCallbackExtensions
{
    /// <summary>Adds a callback that runs when a registered endpoint uses Amazon SQS.</summary>
    /// <param name="configurator">The endpoint registration configurator.</param>
    /// <param name="callback">The Amazon SQS endpoint callback.</param>
    public static void AddAmazonSqsConfigureEndpointCallback(this IEndpointRegistrationConfigurator configurator,
        Action<IRegistrationContext, IAmazonSqsReceiveEndpointConfigurator> callback)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        configurator.AddConfigureEndpointCallback((context, cfg) =>
        {
            if (cfg is IAmazonSqsReceiveEndpointConfigurator sb)
                callback(context, sb);
        });
    }

    /// <summary>Adds a callback that runs for every configured Amazon SQS endpoint.</summary>
    /// <param name="configurator">The bus registration configurator.</param>
    /// <param name="callback">The Amazon SQS endpoints callback.</param>
    public static void AddAmazonSqsConfigureEndpointsCallback(this IBusRegistrationConfigurator configurator, AmazonSqsConfigureEndpointsCallback callback)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        configurator.AddConfigureEndpointsCallback((context, name, cfg) =>
        {
            if (cfg is IAmazonSqsReceiveEndpointConfigurator sb)
                callback(context, name, sb);
        });
    }
}
