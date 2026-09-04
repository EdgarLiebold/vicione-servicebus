using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.ActiveMq.Configuration;

#nullable enable
namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>
/// Provides an active mq consume topology implementation.
/// </summary>
public class ActiveMqConsumeTopology :
    ConsumeTopology,
    IActiveMqConsumeTopologyConfigurator
{
    readonly IActiveMqPublishTopology _publishTopology;
    readonly IList<IActiveMqConsumeTopologySpecification> _specifications;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="publishTopology">The publish topology value.</param>
    /// <param name="consumeTopology">The consume topology value.</param>
    public ActiveMqConsumeTopology(IActiveMqPublishTopology publishTopology, IActiveMqConsumeTopology? consumeTopology = default)
    {
        _publishTopology = publishTopology;

        if (consumeTopology?.ConsumerEndpointQueueNameFormatter != null)
            ConsumerEndpointQueueNameFormatter = consumeTopology.ConsumerEndpointQueueNameFormatter;

        if (consumeTopology?.TemporaryQueueNameFormatter != null)
            TemporaryQueueNameFormatter = consumeTopology.TemporaryQueueNameFormatter;

        _specifications = new List<IActiveMqConsumeTopologySpecification>();
    }

    /// <summary>
    /// Gets or sets the consumer endpoint queue name formatter value.
    /// </summary>
    public IActiveMqConsumerEndpointQueueNameFormatter? ConsumerEndpointQueueNameFormatter { get; set; }
    /// <summary>
    /// Gets or sets the temporary queue name formatter value.
    /// </summary>
    public IActiveMqTemporaryQueueNameFormatter? TemporaryQueueNameFormatter { get; set; }

    IActiveMqMessageConsumeTopology<T> IActiveMqConsumeTopology.GetMessageTopology<T>()
    {
        return (IActiveMqMessageConsumeTopologyConfigurator<T>)base.GetMessageTopology<T>();
    }

    /// <summary>
    /// Adds specification to the configuration.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    public void AddSpecification(IActiveMqConsumeTopologySpecification specification)
    {
        if (specification == null)
            throw new ArgumentNullException(nameof(specification));

        _specifications.Add(specification);
    }

    IActiveMqMessageConsumeTopologyConfigurator<T> IActiveMqConsumeTopologyConfigurator.GetMessageTopology<T>()
    {
        return (IActiveMqMessageConsumeTopologyConfigurator<T>)base.GetMessageTopology<T>();
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(IReceiveEndpointBrokerTopologyBuilder builder)
    {
        foreach (var specification in _specifications)
            specification.Apply(builder);

        ForEach<IActiveMqMessageConsumeTopologyConfigurator>(x => x.Apply(builder));
    }

    /// <summary>
    /// Performs the bind operation.
    /// </summary>
    /// <param name="topicName">The topic name value.</param>
    /// <param name="configure">The configuration callback.</param>
    public void Bind(string topicName, Action<IActiveMqTopicBindingConfigurator>? configure = null)
    {
        IActiveMqTopicBindingConfigurator specification =
            string.IsNullOrEmpty(_publishTopology.VirtualTopicPrefix) || topicName.StartsWith(_publishTopology.VirtualTopicPrefix)
                ? new ConsumerConsumeTopologySpecification(topicName, ConsumerEndpointQueueNameFormatter)
                : new ConsumerConsumeTopicTopologySpecification(topicName);

        configure?.Invoke(specification);

        _specifications.Add((IActiveMqConsumeTopologySpecification)specification);
    }

    /// <summary>
    /// Creates temporary queue name.
    /// </summary>
    /// <param name="tag">The tag value.</param>
    /// <returns>The result of the operation.</returns>
    public override string CreateTemporaryQueueName(string tag)
    {
        var queueName = new string(base.CreateTemporaryQueueName(tag).Where(c => c != '.').ToArray());

        if (TemporaryQueueNameFormatter != null)
            queueName = TemporaryQueueNameFormatter.Format(queueName);

        return queueName;
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
        var messageTopology = new ActiveMqMessageConsumeTopology<T>(_publishTopology.GetMessageTopology<T>(), ConsumerEndpointQueueNameFormatter);

        OnMessageTopologyCreated(messageTopology);

        return messageTopology;
    }
}
