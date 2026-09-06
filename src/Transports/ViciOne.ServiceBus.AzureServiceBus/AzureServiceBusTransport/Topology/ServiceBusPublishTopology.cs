using System;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>Creates Azure Service Bus publish topics and their implemented-message relationships.</summary>
public class ServiceBusPublishTopology :
    PublishTopology,
    IServiceBusPublishTopologyConfigurator
{
    readonly IMessageTopology _messageTopology;

    /// <summary>Creates publish topology linked to provider-neutral message metadata.</summary>
    /// <param name="messageTopology">The message topology used to derive entity names and implemented contracts.</param>
    public ServiceBusPublishTopology(IMessageTopology messageTopology)
    {
        _messageTopology = messageTopology;
    }

    IServiceBusMessagePublishTopology<T> IServiceBusPublishTopology.GetMessageTopology<T>()
    {
        return GetMessageTopology<T>() as IServiceBusMessagePublishTopologyConfigurator<T>
            ?? throw new InvalidOperationException($"The publish topology for {typeof(T).FullName} is not an Azure Service Bus topology.");
    }

    /// <summary>Fits a subscription name within the Azure Service Bus limit using a stable hash suffix when shortening is required.</summary>
    /// <param name="subscriptionName">The subscription name.</param>
    /// <returns>The original name when it fits; otherwise, a deterministic 50-character name.</returns>
    public string FormatSubscriptionName(string subscriptionName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionName);

        return EntityNameShortener.Shorten(subscriptionName, 50);
    }

    /// <summary>Creates a subscription name from an entity and optional host scope.</summary>
    /// <param name="entityName">The entity name.</param>
    /// <param name="hostScope">An optional scope appended to the entity name.</param>
    /// <returns>A deterministic name within the Azure Service Bus subscription limit.</returns>
    public string GenerateSubscriptionName(string entityName, string? hostScope = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);

        return FormatSubscriptionName(string.IsNullOrWhiteSpace(hostScope) ? entityName : $"{entityName}-{hostScope}");
    }

    IServiceBusMessagePublishTopologyConfigurator IServiceBusPublishTopologyConfigurator.GetMessageTopology(Type messageType)
    {
        return GetMessageTopology(messageType) as IServiceBusMessagePublishTopologyConfigurator
            ?? throw new InvalidOperationException($"The publish topology for {messageType.FullName} is not an Azure Service Bus topology.");
    }

    /// <summary>Builds broker topology for every configured publish message type.</summary>
    /// <returns>The topics and relationships to declare.</returns>
    public BrokerTopology GetPublishBrokerTopology()
    {
        var builder = new PublishEndpointBrokerTopologyBuilder(this);

        ForEachMessageType<IServiceBusMessagePublishTopology>(x =>
        {
            x.Apply(builder);

            builder.Topic = null;
        });

        return builder.BuildBrokerTopology();
    }

    IServiceBusMessagePublishTopologyConfigurator<T> IServiceBusPublishTopologyConfigurator.GetMessageTopology<T>()
    {
        return GetMessageTopology<T>() as IServiceBusMessagePublishTopologyConfigurator<T>
            ?? throw new InvalidOperationException($"The publish topology for {typeof(T).FullName} is not an Azure Service Bus topology.");
    }

    /// <summary>Creates publish topology for a message contract and connects its implemented contracts.</summary>
    /// <typeparam name="T">The published message contract.</typeparam>
    /// <returns>The message-specific Azure publish topology.</returns>
    protected override IMessagePublishTopologyConfigurator CreateMessageTopology<T>()
    {
        var messageTopology = new ServiceBusMessagePublishTopology<T>(this, _messageTopology.GetMessageTopology<T>());

        var connector = new ImplementedMessageTypeConnector<T>(this, messageTopology);

        ImplementedMessageTypeCache<T>.EnumerateImplementedTypes(connector);

        OnMessageTopologyCreated(messageTopology);

        return messageTopology;
    }


    class ImplementedMessageTypeConnector<TMessage> :
        IImplementedMessageType
        where TMessage : class
    {
        readonly ServiceBusMessagePublishTopology<TMessage> _messagePublishTopologyConfigurator;
        readonly IServiceBusPublishTopologyConfigurator _publishTopology;

        public ImplementedMessageTypeConnector(IServiceBusPublishTopologyConfigurator publishTopology,
            ServiceBusMessagePublishTopology<TMessage> messagePublishTopologyConfigurator)
        {
            _publishTopology = publishTopology;
            _messagePublishTopologyConfigurator = messagePublishTopologyConfigurator;
        }

        public void ImplementsMessageType<T>(bool direct)
            where T : class
        {
            IServiceBusMessagePublishTopologyConfigurator<T> messageTopology = _publishTopology.GetMessageTopology<T>();

            _messagePublishTopologyConfigurator.AddImplementedMessageConfigurator(messageTopology, direct);
        }
    }
}
