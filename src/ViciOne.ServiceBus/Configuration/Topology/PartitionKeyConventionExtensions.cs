using System;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures partition-key selection for message send topology.</summary>
public static class PartitionKeyConventionExtensions
{
    /// <summary>Uses a message-specific partition-key formatter.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="configurator">The message send topology to configure.</param>
    /// <param name="formatter">The partition-key formatter.</param>
    public static void UsePartitionKeyFormatter<T>(this IMessageSendTopologyConfigurator<T> configurator, IMessagePartitionKeyFormatter<T> formatter)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(formatter);

        configurator.UpdateConvention<IPartitionKeyMessageSendTopologyConvention<T>>(update =>
        {
            update.SetFormatter(formatter);

            return update;
        });
    }

    /// <summary>Uses a message-specific partition-key formatter for one contract in a send topology.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="configurator">The send topology to configure.</param>
    /// <param name="formatter">The partition-key formatter.</param>
    public static void UsePartitionKeyFormatter<T>(this ISendTopologyConfigurator configurator, IMessagePartitionKeyFormatter<T> formatter)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(formatter);

        configurator.GetMessageTopology<T>().UsePartitionKeyFormatter(formatter);
    }

    /// <summary>Uses a delegate to select a partition key for one contract in a send topology.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="configurator">The send topology to configure.</param>
    /// <param name="formatter">The delegate that returns a non-null partition key.</param>
    public static void UsePartitionKeyFormatter<T>(this ISendTopologyConfigurator configurator, Func<SendContext<T>, string> formatter)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(formatter);

        configurator.GetMessageTopology<T>().UsePartitionKeyFormatter(new DelegatePartitionKeyFormatter<T>(formatter));
    }

    /// <summary>Uses a delegate to select a partition key for a message send topology.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="configurator">The message send topology to configure.</param>
    /// <param name="formatter">The delegate that returns a non-null partition key.</param>
    public static void UsePartitionKeyFormatter<T>(this IMessageSendTopologyConfigurator<T> configurator, Func<SendContext<T>, string> formatter)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(formatter);

        configurator.UsePartitionKeyFormatter(new DelegatePartitionKeyFormatter<T>(formatter));
    }
}
