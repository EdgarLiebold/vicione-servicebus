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

    /// <summary>Generates a bounded subscription name from an entity name and optional namespace scope.</summary>
    /// <param name="entityName">The destination queue or topic name.</param>
    /// <param name="hostScope">The optional namespace-relative scope.</param>
    /// <returns>A deterministic subscription name no longer than 50 characters.</returns>
    string GenerateSubscriptionName(string entityName, string? hostScope = default);
}
