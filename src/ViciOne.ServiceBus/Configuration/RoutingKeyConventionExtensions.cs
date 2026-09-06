using System;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides extension methods for routing key convention.</summary>
public static class RoutingKeyConventionExtensions
{
    /// <summary>Configures routing key formatter for the current pipeline.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="formatter">The formatter.</param>
    public static void UseRoutingKeyFormatter<T>(this IMessageSendTopologyConfigurator<T> configurator, IMessageRoutingKeyFormatter<T> formatter)
        where T : class
    {
        configurator.UpdateConvention<IRoutingKeyMessageSendTopologyConvention<T>>(update =>
        {
            update.SetFormatter(formatter);

            return update;
        });
    }

    /// <summary>Use the routing key formatter for the specified message type.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="formatter">The formatter.</param>
    public static void UseRoutingKeyFormatter<T>(this ISendTopologyConfigurator configurator, IMessageRoutingKeyFormatter<T> formatter)
        where T : class
    {
        configurator.GetMessageTopology<T>().UseRoutingKeyFormatter(formatter);
    }

    /// <summary>Use the delegate to format the routing key.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="formatter">The formatter.</param>
    public static void UseRoutingKeyFormatter<T>(this ISendTopologyConfigurator configurator, Func<SendContext<T>, string> formatter)
        where T : class
    {
        configurator.GetMessageTopology<T>().UseRoutingKeyFormatter(new DelegateRoutingKeyFormatter<T>(formatter));
    }

    /// <summary>Use the delegate to format the routing key.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="formatter">The formatter.</param>
    public static void UseRoutingKeyFormatter<T>(this IMessageSendTopologyConfigurator<T> configurator, Func<SendContext<T>, string> formatter)
        where T : class
    {
        configurator.UseRoutingKeyFormatter(new DelegateRoutingKeyFormatter<T>(formatter));
    }
}
