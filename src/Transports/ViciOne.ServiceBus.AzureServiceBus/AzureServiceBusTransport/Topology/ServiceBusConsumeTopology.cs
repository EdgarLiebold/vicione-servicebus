using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.AzureServiceBus.Configuration;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>
/// Provides a service bus consume topology implementation.
/// </summary>
public class ServiceBusConsumeTopology :
    ConsumeTopology,
    IServiceBusConsumeTopologyConfigurator
{
    readonly IMessageTopology _messageTopology;
    readonly IServiceBusPublishTopology _publishTopology;
    readonly IList<IServiceBusConsumeTopologySpecification> _specifications;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="messageTopology">The message topology value.</param>
    /// <param name="publishTopology">The publish topology value.</param>
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

    /// <summary>
    /// Adds specification to the configuration.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    public void AddSpecification(IServiceBusConsumeTopologySpecification specification)
    {
        if (specification == null)
            throw new ArgumentNullException(nameof(specification));

        _specifications.Add(specification);
    }

    /// <summary>
    /// Performs the subscribe operation.
    /// </summary>
    /// <param name="topicName">The topic name value.</param>
    /// <param name="subscriptionName">The subscription name value.</param>
    /// <param name="callback">The callback value.</param>
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

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(IReceiveEndpointBrokerTopologyBuilder builder)
    {
        foreach (var specification in _specifications)
            specification.Apply(builder);

        ForEach<IServiceBusMessageConsumeTopologyConfigurator>(x => x.Apply(builder));
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override IEnumerable<ValidationResult> Validate()
    {
        return base.Validate().Concat(_specifications.SelectMany(x => x.Validate()));
    }

    /// <summary>
    /// Creates message topology.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    protected override IMessageConsumeTopologyConfigurator CreateMessageTopology<T>()
    {
        var messageTopology = new ServiceBusMessageConsumeTopology<T>(_messageTopology.GetMessageTopology<T>(), _publishTopology.GetMessageTopology<T>());

        OnMessageTopologyCreated(messageTopology);

        return messageTopology;
    }
}
