using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.AmazonSqs.Configuration;

namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>
/// Provides an amazon sqs message consume topology implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
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

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="messageTopology">The message topology value.</param>
    /// <param name="publishTopology">The publish topology value.</param>
    public AmazonSqsMessageConsumeTopology(IMessageTopology<TMessage> messageTopology, IAmazonSqsPublishTopology publishTopology)
    {
        _messageTopology = messageTopology;
        _publishTopology = publishTopology;
        _messagePublishTopology = _publishTopology.GetMessageTopology<TMessage>();

        _specifications = new List<IAmazonSqsConsumeTopologySpecification>();
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
    /// <param name="configure">The configuration callback.</param>
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

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override IEnumerable<ValidationResult> Validate()
    {
        return base.Validate().Concat(_specifications.SelectMany(x => x.Validate()));
    }
}
