using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.AzureServiceBus.Configuration;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>Collects Azure Service Bus subscriptions applied to receive endpoints.</summary>
public class ServiceBusConsumeTopology :
    ConsumeTopology,
    IServiceBusConsumeTopologyConfigurator
{
    readonly IMessageTopology _messageTopology;
    readonly IServiceBusPublishTopology _publishTopology;
    readonly IList<IServiceBusConsumeTopologySpecification> _specifications;

    /// <summary>Creates an empty consume topology linked to message and publish topology.</summary>
    /// <param name="messageTopology">The provider-neutral message topology.</param>
    /// <param name="publishTopology">The Azure publish topology used to resolve message topics.</param>
    public ServiceBusConsumeTopology(IMessageTopology messageTopology, IServiceBusPublishTopology publishTopology)
        : base(260)
    {
        _messageTopology = messageTopology;
        _publishTopology = publishTopology;
        _specifications = new List<IServiceBusConsumeTopologySpecification>();
    }

    IServiceBusMessageConsumeTopology<T> IServiceBusConsumeTopology.GetMessageTopology<T>()
    {
        return (IServiceBusMessageConsumeTopologyConfigurator<T>)GetMessageTopology<T>();
    }

    IServiceBusMessageConsumeTopologyConfigurator<T> IServiceBusConsumeTopologyConfigurator.GetMessageTopology<T>()
    {
        return (IServiceBusMessageConsumeTopologyConfigurator<T>)GetMessageTopology<T>();
    }

    /// <summary>Adds a subscription specification to the consume topology.</summary>
    /// <param name="specification">The specification to apply when an endpoint is built.</param>
    public void AddSpecification(IServiceBusConsumeTopologySpecification specification)
    {
        if (specification == null)
            throw new ArgumentNullException(nameof(specification));

        _specifications.Add(specification);
    }

    /// <summary>Adds a subscription to a named topic.</summary>
    /// <param name="topicName">The namespace-relative topic name.</param>
    /// <param name="subscriptionName">The subscription name.</param>
    /// <param name="callback">Optionally configures the subscription.</param>
    public void Subscribe(string topicName, string subscriptionName, Action<IServiceBusSubscriptionConfigurator>? callback = null)
    {
        if (string.IsNullOrWhiteSpace(topicName))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(topicName));

        if (string.IsNullOrWhiteSpace(subscriptionName))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(subscriptionName));

        subscriptionName = _publishTopology.FormatSubscriptionName(subscriptionName);

        var createTopicOptions = Defaults.GetCreateTopicOptions(topicName);

        var subscriptionConfigurator = new ServiceBusSubscriptionConfigurator(subscriptionName, createTopicOptions.Name);

        callback?.Invoke(subscriptionConfigurator);

        var specification = new SubscriptionConsumeTopologySpecification(createTopicOptions, subscriptionConfigurator.GetCreateSubscriptionOptions(),
            subscriptionConfigurator.Rule,
            subscriptionConfigurator.Filter);

        _specifications.Add(specification);
    }

    /// <summary>Applies explicit and message-specific subscriptions to a receive-endpoint builder.</summary>
    /// <param name="builder">The topology builder receiving the subscriptions.</param>
    public void Apply(IReceiveEndpointBrokerTopologyBuilder builder)
    {
        foreach (var specification in _specifications)
            specification.Apply(builder);

        ForEach<IServiceBusMessageConsumeTopologyConfigurator>(x => x.Apply(builder));
    }

    /// <summary>Validates the bus-wide consume topology and every explicit subscription.</summary>
    /// <returns>All Azure Service Bus consume-topology validation failures.</returns>
    public override IEnumerable<ValidationResult> Validate()
    {
        return base.Validate().Concat(_specifications.SelectMany(x => x.Validate()));
    }

    /// <summary>Creates consume topology for a message contract.</summary>
    /// <typeparam name="T">The consumed message contract.</typeparam>
    /// <returns>The message-specific Azure consume topology.</returns>
    protected override IMessageConsumeTopologyConfigurator CreateMessageTopology<T>()
    {
        var messageTopology = new ServiceBusMessageConsumeTopology<T>(_messageTopology.GetMessageTopology<T>(), _publishTopology.GetMessageTopology<T>());

        OnMessageTopologyCreated(messageTopology);

        return messageTopology;
    }
}
