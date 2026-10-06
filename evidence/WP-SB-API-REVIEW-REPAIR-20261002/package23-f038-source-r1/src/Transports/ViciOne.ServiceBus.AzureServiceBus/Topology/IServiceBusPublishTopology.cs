using ViciOne.ServiceBus.AzureServiceBus.Topology;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Exposes Azure Service Bus publish topics for a bus.</summary>
public interface IServiceBusPublishTopology :
    IPublishTopology
{
    /// <summary>Gets the publish topology for a message contract.</summary>
    /// <typeparam name="T">The published message contract.</typeparam>
    /// <returns>The message-specific publish topology.</returns>
    new IServiceBusMessagePublishTopology<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>Builds the broker entities required by the configured publish topology.</summary>
    /// <returns>The topics and subscriptions to declare.</returns>
    BrokerTopology GetPublishBrokerTopology();

    /// <summary>Shortens a subscription name to Azure Service Bus's 50-character limit.</summary>
    /// <param name="name">The candidate subscription name.</param>
    /// <returns>The original name when valid, otherwise a deterministic shortened name.</returns>
    string FormatSubscriptionName(string name);

    /// <summary>Generates an automatic subscription name from the complete physical destination and namespace identity.</summary>
    /// <param name="entityName">The complete destination path, including any configured base path.</param>
    /// <param name="hostScope">The optional namespace authority discriminator, without credentials or query options.</param>
    /// <returns>A deterministic 45-character name derived from the case-insensitive destination identity.</returns>
    /// <remarks>Existing automatic subscriptions require an explicit resource cutover. Explicit subscription names and <see cref="FormatSubscriptionName" /> are unaffected.</remarks>
    string GenerateSubscriptionName(string entityName, string? hostScope = default);
}
