using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.AzureServiceBus.Configuration;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>
/// Provides a service bus message consume topology implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class ServiceBusMessageConsumeTopology<TMessage> :
    MessageConsumeTopology<TMessage>,
    IServiceBusMessageConsumeTopologyConfigurator<TMessage>,
    IServiceBusMessageConsumeTopologyConfigurator
    where TMessage : class
{
    readonly IMessageTopology<TMessage> _messageTopology;
    readonly IServiceBusMessagePublishTopology<TMessage> _publishTopology;
    readonly IList<IServiceBusConsumeTopologySpecification> _specifications;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="messageTopology">The message topology value.</param>
    /// <param name="publishTopology">The publish topology value.</param>
    public ServiceBusMessageConsumeTopology(IMessageTopology<TMessage> messageTopology, IServiceBusMessagePublishTopology<TMessage> publishTopology)
    {
        _messageTopology = messageTopology;
        _publishTopology = publishTopology;

        _specifications = new List<IServiceBusConsumeTopologySpecification>();
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(IReceiveEndpointBrokerTopologyBuilder builder)
    {
        foreach (var specification in _specifications)
            specification.Apply(builder);
    }

    /// <summary>
    /// Performs the subscribe operation.
    /// </summary>
    /// <param name="subscriptionName">The subscription name value.</param>
    /// <param name="configure">The configuration callback.</param>
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

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override IEnumerable<ValidationResult> Validate()
    {
        return base.Validate().Concat(_specifications.SelectMany(x => x.Validate()));
    }
}
