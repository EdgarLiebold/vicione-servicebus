using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.AmazonSqs.Configuration;

namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>Builds Amazon SNS topic subscriptions for an Amazon SQS receive queue.</summary>
public class AmazonSqsConsumeTopology :
    ConsumeTopology,
    IAmazonSqsConsumeTopologyConfigurator
{
    readonly IMessageTopology _messageTopology;
    readonly IAmazonSqsPublishTopology _publishTopology;
    readonly IList<IAmazonSqsConsumeTopologySpecification> _specifications;

    /// <summary>Initializes Amazon SQS consume topology.</summary>
    /// <param name="messageTopology">The message-topology convention source.</param>
    /// <param name="publishTopology">The Amazon SNS publish topology used to resolve topics.</param>
    public AmazonSqsConsumeTopology(IMessageTopology messageTopology, IAmazonSqsPublishTopology publishTopology)
        : base(72)
    {
        _messageTopology = messageTopology;
        _publishTopology = publishTopology;

        _specifications = new List<IAmazonSqsConsumeTopologySpecification>();
    }

    IAmazonSqsMessageConsumeTopology<T> IAmazonSqsConsumeTopology.GetMessageTopology<T>()
    {
        return (IAmazonSqsMessageConsumeTopologyConfigurator<T>)base.GetMessageTopology<T>();
    }

    /// <summary>Adds a queue-subscription specification.</summary>
    /// <param name="specification">The specification to apply and validate.</param>
    public void AddSpecification(IAmazonSqsConsumeTopologySpecification specification)
    {
        if (specification == null)
            throw new ArgumentNullException(nameof(specification));

        _specifications.Add(specification);
    }

    IAmazonSqsMessageConsumeTopologyConfigurator<T> IAmazonSqsConsumeTopologyConfigurator.GetMessageTopology<T>()
    {
        return (IAmazonSqsMessageConsumeTopologyConfigurator<T>)base.GetMessageTopology<T>();
    }

    /// <summary>Applies explicit and message-specific subscriptions to a receive-endpoint builder.</summary>
    /// <param name="builder">The receive-endpoint broker-topology builder.</param>
    public void Apply(IReceiveEndpointBrokerTopologyBuilder builder)
    {
        foreach (var specification in _specifications)
            specification.Apply(builder);

        ForEach<IAmazonSqsMessageConsumeTopologyConfigurator>(x => x.Apply(builder));
    }

    /// <summary>Subscribes the receive queue to a named Amazon SNS topic.</summary>
    /// <param name="topicName">The source topic name.</param>
    /// <param name="configure">An optional callback that configures the subscription.</param>
    public void Bind(string topicName, Action<IAmazonSqsTopicSubscriptionConfigurator>? configure = null)
    {
        var specification = new ConsumerConsumeTopologySpecification(_publishTopology, topicName);

        configure?.Invoke(specification);

        _specifications.Add(specification);
    }

    /// <summary>Validates base consume topology and all explicit subscription specifications.</summary>
    /// <returns>All detected validation failures.</returns>
    public override IEnumerable<ValidationResult> Validate()
    {
        return base.Validate().Concat(_specifications.SelectMany(x => x.Validate()));
    }

    /// <summary>Creates Amazon SQS consume topology for a message type and notifies topology observers.</summary>
    /// <typeparam name="T">The consumed message type.</typeparam>
    /// <returns>The message consume-topology configurator.</returns>
    protected override IMessageConsumeTopologyConfigurator CreateMessageTopology<T>()
    {
        var messageTopology = new AmazonSqsMessageConsumeTopology<T>(_messageTopology.GetMessageTopology<T>(), _publishTopology);

        OnMessageTopologyCreated(messageTopology);

        return messageTopology;
    }
}
