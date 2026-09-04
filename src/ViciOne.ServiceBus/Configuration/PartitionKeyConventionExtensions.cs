using System;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides extension methods for partition key convention.
/// </summary>
public static class PartitionKeyConventionExtensions
{
    /// <summary>
    /// Configures partition key formatter for the current pipeline.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="formatter">The formatter value.</param>
    public static void UsePartitionKeyFormatter<T>(this IMessageSendTopologyConfigurator<T> configurator, IMessagePartitionKeyFormatter<T> formatter)
        where T : class
    {
        configurator.UpdateConvention<IPartitionKeyMessageSendTopologyConvention<T>>(update =>
        {
            update.SetFormatter(formatter);

            return update;
        });
    }

    /// <summary>
    /// Use the partition key formatter for the specified message type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="configurator"></param>
    /// <param name="formatter"></param>
    public static void UsePartitionKeyFormatter<T>(this ISendTopologyConfigurator configurator, IMessagePartitionKeyFormatter<T> formatter)
        where T : class
    {
        configurator.GetMessageTopology<T>().UsePartitionKeyFormatter(formatter);
    }

    /// <summary>
    /// Use the delegate to format the partition key, using Empty if the string is null upon return
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="configurator"></param>
    /// <param name="formatter"></param>
    public static void UsePartitionKeyFormatter<T>(this ISendTopologyConfigurator configurator, Func<SendContext<T>, string> formatter)
        where T : class
    {
        configurator.GetMessageTopology<T>().UsePartitionKeyFormatter(new DelegatePartitionKeyFormatter<T>(formatter));
    }

    /// <summary>
    /// Use the delegate to format the partition key, using Empty if the string is null upon return
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="configurator"></param>
    /// <param name="formatter"></param>
    public static void UsePartitionKeyFormatter<T>(this IMessageSendTopologyConfigurator<T> configurator, Func<SendContext<T>, string> formatter)
        where T : class
    {
        configurator.UsePartitionKeyFormatter(new DelegatePartitionKeyFormatter<T>(formatter));
    }
}
