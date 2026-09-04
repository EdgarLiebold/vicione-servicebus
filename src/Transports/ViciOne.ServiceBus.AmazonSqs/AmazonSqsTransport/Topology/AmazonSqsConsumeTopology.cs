using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.AmazonSqs.Configuration;

namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>
/// Provides an amazon sqs consume topology implementation.
/// </summary>
public class AmazonSqsConsumeTopology :
    ConsumeTopology,
    IAmazonSqsConsumeTopologyConfigurator
{
    readonly IMessageTopology _messageTopology;
    readonly IAmazonSqsPublishTopology _publishTopology;
    readonly IList<IAmazonSqsConsumeTopologySpecification> _specifications;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="messageTopology">The message topology value.</param>
    /// <param name="publishTopology">The publish topology value.</param>
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

    /// <summary>
    /// Adds specification to the configuration.
    /// </summary>
    /// <param name="specification">The specification value.</param>
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

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(IReceiveEndpointBrokerTopologyBuilder builder)
    {
        foreach (var specification in _specifications)
            specification.Apply(builder);

        ForEach<IAmazonSqsMessageConsumeTopologyConfigurator>(x => x.Apply(builder));
    }

    /// <summary>
    /// Performs the bind operation.
    /// </summary>
    /// <param name="topicName">The topic name value.</param>
    /// <param name="configure">The configuration callback.</param>
    public void Bind(string topicName, Action<IAmazonSqsTopicSubscriptionConfigurator>? configure = null)
    {
        var specification = new ConsumerConsumeTopologySpecification(_publishTopology, topicName);

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

    /// <summary>
    /// Creates message topology.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    protected override IMessageConsumeTopologyConfigurator CreateMessageTopology<T>()
    {
        var messageTopology = new AmazonSqsMessageConsumeTopology<T>(_messageTopology.GetMessageTopology<T>(), _publishTopology);

        OnMessageTopologyCreated(messageTopology);

        return messageTopology;
    }
}
