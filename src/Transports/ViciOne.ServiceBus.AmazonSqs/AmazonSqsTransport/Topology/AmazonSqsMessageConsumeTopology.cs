using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.AmazonSqs.Configuration;

namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>Configures the Amazon SNS topic subscription used to consume a message type from an Amazon SQS queue.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class AmazonSqsMessageConsumeTopology<TMessage> :
    MessageConsumeTopology<TMessage>,
    IAmazonSqsMessageConsumeTopologyConfigurator<TMessage>,
    IAmazonSqsMessageConsumeTopologyConfigurator
    where TMessage : class
{
    readonly IAmazonSqsMessagePublishTopology<TMessage> _messagePublishTopology;
    readonly IMessageTopology<TMessage> _messageTopology;
    readonly IAmazonSqsPublishTopology _publishTopology;
    readonly IList<IAmazonSqsConsumeTopologySpecification> _specifications;

    /// <summary>Initializes consume topology for a message type.</summary>
    /// <param name="messageTopology">The message topology used to determine whether the type is bindable.</param>
    /// <param name="publishTopology">The Amazon SNS publish topology used to resolve the source topic.</param>
    public AmazonSqsMessageConsumeTopology(IMessageTopology<TMessage> messageTopology, IAmazonSqsPublishTopology publishTopology)
    {
        _messageTopology = messageTopology;
        _publishTopology = publishTopology;
        _messagePublishTopology = _publishTopology.GetMessageTopology<TMessage>();

        _specifications = new List<IAmazonSqsConsumeTopologySpecification>();
    }

    /// <summary>Applies all message-specific subscription specifications to a receive-endpoint builder.</summary>
    /// <param name="builder">The receive-endpoint broker-topology builder.</param>
    public void Apply(IReceiveEndpointBrokerTopologyBuilder builder)
    {
        foreach (var specification in _specifications)
            specification.Apply(builder);
    }

    /// <summary>Subscribes the receive queue to the Amazon SNS topic for this message type.</summary>
    /// <param name="configure">An optional callback that configures the topic subscription.</param>
    public void Subscribe(Action<IAmazonSqsTopicSubscriptionConfigurator>? configure = null)
    {
        if (!IsBindableMessageType)
        {
            _specifications.Add(new InvalidAmazonSqsConsumeTopologySpecification(TypeCache<TMessage>.ShortName, "Is not a bindable message type"));
            return;
        }

        var specification = new ConsumerConsumeTopologySpecification(_publishTopology, _messagePublishTopology.Topic);

        configure?.Invoke(specification);

        _specifications.Add(specification);
    }

    /// <summary>Validates the message topology and its subscription specifications.</summary>
    /// <returns>All detected validation failures.</returns>
    public override IEnumerable<ValidationResult> Validate()
    {
        return base.Validate().Concat(_specifications.SelectMany(x => x.Validate()));
    }
}
