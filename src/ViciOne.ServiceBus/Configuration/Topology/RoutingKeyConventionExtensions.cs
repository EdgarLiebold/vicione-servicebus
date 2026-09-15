using System;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures routing-key selection for message send topology.</summary>
public static class RoutingKeyConventionExtensions
{
    /// <summary>Uses a message-specific routing-key formatter.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="configurator">The message send topology to configure.</param>
    /// <param name="formatter">The routing-key formatter.</param>
    public static void UseRoutingKeyFormatter<T>(this IMessageSendTopologyConfigurator<T> configurator, IMessageRoutingKeyFormatter<T> formatter)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(formatter);

        configurator.UpdateConvention<IRoutingKeyMessageSendTopologyConvention<T>>(update =>
        {
            update.SetFormatter(formatter);

            return update;
        });
    }

    /// <summary>Uses a message-specific routing-key formatter for one contract in a send topology.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="configurator">The send topology to configure.</param>
    /// <param name="formatter">The routing-key formatter.</param>
    public static void UseRoutingKeyFormatter<T>(this ISendTopologyConfigurator configurator, IMessageRoutingKeyFormatter<T> formatter)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(formatter);

        configurator.GetMessageTopology<T>().UseRoutingKeyFormatter(formatter);
    }

    /// <summary>Uses a delegate to select a routing key for one contract in a send topology.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="configurator">The send topology to configure.</param>
    /// <param name="formatter">The delegate that returns a non-null routing key.</param>
    public static void UseRoutingKeyFormatter<T>(this ISendTopologyConfigurator configurator, Func<SendContext<T>, string> formatter)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(formatter);

        configurator.GetMessageTopology<T>().UseRoutingKeyFormatter(new DelegateRoutingKeyFormatter<T>(formatter));
    }

    /// <summary>Uses a delegate to select a routing key for a message send topology.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="configurator">The message send topology to configure.</param>
    /// <param name="formatter">The delegate that returns a non-null routing key.</param>
    public static void UseRoutingKeyFormatter<T>(this IMessageSendTopologyConfigurator<T> configurator, Func<SendContext<T>, string> formatter)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(formatter);

        configurator.UseRoutingKeyFormatter(new DelegateRoutingKeyFormatter<T>(formatter));
    }
}
