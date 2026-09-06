using System;
using ViciOne.ServiceBus.AzureServiceBus;
using ViciOne.ServiceBus.AzureServiceBus.Configuration;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Configures Azure Service Bus session identifier conventions for sent messages.</summary>
public static class ServiceBusSessionIdConventionExtensions
{
    /// <summary>Replaces the session identifier formatter on a message-specific send topology.</summary>
    /// <typeparam name="T">The sent message contract.</typeparam>
    /// <param name="configurator">The message-specific send topology.</param>
    /// <param name="formatter">The formatter that derives session identifiers.</param>
    public static void UseSessionIdFormatter<T>(this IMessageSendTopologyConfigurator<T> configurator, IMessageSessionIdFormatter<T> formatter)
        where T : class
    {
        configurator.UpdateConvention<ISessionIdMessageSendTopologyConvention<T>>(
            update =>
            {
                update.SetFormatter(formatter);

                return update;
            });
    }

    /// <summary>Replaces the session identifier formatter for a message contract.</summary>
    /// <typeparam name="T">The sent message contract.</typeparam>
    /// <param name="configurator">The bus-wide send topology.</param>
    /// <param name="formatter">The formatter that derives session identifiers.</param>
    public static void UseSessionIdFormatter<T>(this ISendTopologyConfigurator configurator, IMessageSessionIdFormatter<T> formatter)
        where T : class
    {
        configurator.GetMessageTopology<T>().UseSessionIdFormatter(formatter);
    }

    /// <summary>Derives the session identifier for a message contract with a delegate.</summary>
    /// <typeparam name="T">The sent message contract.</typeparam>
    /// <param name="configurator">The bus-wide send topology.</param>
    /// <param name="formatter">The delegate whose result is assigned to the session identifier.</param>
    public static void UseSessionIdFormatter<T>(this ISendTopologyConfigurator configurator, Func<SendContext<T>, string> formatter)
        where T : class
    {
        configurator.GetMessageTopology<T>().UseSessionIdFormatter(new DelegateSessionIdFormatter<T>(formatter));
    }

    /// <summary>Derives the session identifier on a message-specific send topology with a delegate.</summary>
    /// <typeparam name="T">The sent message contract.</typeparam>
    /// <param name="configurator">The message-specific send topology.</param>
    /// <param name="formatter">The delegate whose result is assigned to the session identifier.</param>
    public static void UseSessionIdFormatter<T>(this IMessageSendTopologyConfigurator<T> configurator, Func<SendContext<T>, string> formatter)
        where T : class
    {
        configurator.UseSessionIdFormatter(new DelegateSessionIdFormatter<T>(formatter));
    }
}
