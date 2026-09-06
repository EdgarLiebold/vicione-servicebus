using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.AzureServiceBus.Configuration;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>Collects Azure Service Bus subscriptions used to consume a message contract.</summary>
/// <typeparam name="TMessage">The consumed message contract.</typeparam>
public class ServiceBusMessageConsumeTopology<TMessage> :
    MessageConsumeTopology<TMessage>,
    IServiceBusMessageConsumeTopologyConfigurator<TMessage>,
    IServiceBusMessageConsumeTopologyConfigurator
    where TMessage : class
{
    readonly IMessageTopology<TMessage> _messageTopology;
    readonly IServiceBusMessagePublishTopology<TMessage> _publishTopology;
    readonly IList<IServiceBusConsumeTopologySpecification> _specifications;

    /// <summary>Creates an empty consume topology for a message contract.</summary>
    /// <param name="messageTopology">The provider-neutral topology for the message contract.</param>
    /// <param name="publishTopology">The Azure publish topology used to resolve the message topic.</param>
    public ServiceBusMessageConsumeTopology(IMessageTopology<TMessage> messageTopology, IServiceBusMessagePublishTopology<TMessage> publishTopology)
    {
        _messageTopology = messageTopology;
        _publishTopology = publishTopology;

        _specifications = new List<IServiceBusConsumeTopologySpecification>();
    }

    /// <summary>Applies all message-specific subscription specifications to a receive-endpoint builder.</summary>
    /// <param name="builder">The topology builder receiving the subscriptions.</param>
    public void Apply(IReceiveEndpointBrokerTopologyBuilder builder)
    {
        foreach (var specification in _specifications)
            specification.Apply(builder);
    }

    /// <summary>Adds a subscription to this message contract's publish topic.</summary>
    /// <param name="subscriptionName">The subscription name.</param>
    /// <param name="configure">Optionally configures the subscription.</param>
    public void Subscribe(string subscriptionName, Action<IServiceBusSubscriptionConfigurator>? configure = null)
    {
        if (string.IsNullOrWhiteSpace(subscriptionName))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(subscriptionName));

        if (!IsBindableMessageType)
        {
            _specifications.Add(new InvalidServiceBusConsumeTopologySpecification(TypeCache<TMessage>.ShortName, "Is not a bindable message type"));
            return;
        }

        var createTopicOptions = _publishTopology.CreateTopicOptions;

        var subscriptionConfigurator = _publishTopology.GetSubscriptionConfigurator(subscriptionName);

        configure?.Invoke(subscriptionConfigurator);

        var specification = new SubscriptionConsumeTopologySpecification(createTopicOptions, subscriptionConfigurator.GetCreateSubscriptionOptions(),
            subscriptionConfigurator.Rule,
            subscriptionConfigurator.Filter);

        _specifications.Add(specification);
    }

    /// <summary>Validates the message topology and every configured subscription.</summary>
    /// <returns>All validation failures for this consumed message contract.</returns>
    public override IEnumerable<ValidationResult> Validate()
    {
        return base.Validate().Concat(_specifications.SelectMany(x => x.Validate()));
    }
}
