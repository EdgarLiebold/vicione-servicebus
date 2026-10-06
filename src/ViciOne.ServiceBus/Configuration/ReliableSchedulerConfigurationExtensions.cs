using System;
using ViciOne.ServiceBus.Providers.Persistence;


namespace ViciOne.ServiceBus.Configuration;

/// <summary>Selects explicit scheduling adapters for a reliable-messaging block.</summary>
public static class ReliableSchedulerConfigurationExtensions
{
    /// <summary>Selects transport-native scheduling instead of the reliable store's default DueAt scheduler.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <returns>The same configurator after selecting the transport scheduler adapter.</returns>
    public static IReliableMessagingConfigurator UseTransportScheduler(
        this IReliableMessagingConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        if (configurator is not IReliableMessagingProviderConfigurator provider)
        {
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                    "Scheduling",
                    "unknown",
                    "The reliable-messaging configurator does not expose its scheduler adapter contract.",
                    "Choose the adapter inside UseReliableMessaging"));
        }

        provider.UseTransportSchedulerAdapter();
        return configurator;
    }
}
