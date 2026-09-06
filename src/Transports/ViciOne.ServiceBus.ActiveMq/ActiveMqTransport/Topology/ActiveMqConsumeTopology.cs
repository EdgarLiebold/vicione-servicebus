using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.ActiveMq.Configuration;

namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>Collects ActiveMQ receive bindings and per-message consume topology.</summary>
public class ActiveMqConsumeTopology :
    ConsumeTopology,
    IActiveMqConsumeTopologyConfigurator
{
    readonly IActiveMqPublishTopology _publishTopology;
    readonly IList<IActiveMqConsumeTopologySpecification> _specifications;

    /// <summary>Creates consume topology linked to a publish topology and optional parent settings.</summary>
    /// <param name="publishTopology">The publish topology that resolves message topics.</param>
    /// <param name="consumeTopology">An optional parent whose naming formatters are copied.</param>
    public ActiveMqConsumeTopology(IActiveMqPublishTopology publishTopology, IActiveMqConsumeTopology? consumeTopology = default)
    {
        _publishTopology = publishTopology;

        if (consumeTopology?.ConsumerEndpointQueueNameFormatter != null)
            ConsumerEndpointQueueNameFormatter = consumeTopology.ConsumerEndpointQueueNameFormatter;

        if (consumeTopology?.TemporaryQueueNameFormatter != null)
            TemporaryQueueNameFormatter = consumeTopology.TemporaryQueueNameFormatter;

        _specifications = new List<IActiveMqConsumeTopologySpecification>();
    }

    /// <summary>Gets or sets the formatter for virtual-topic consumer queues or subscriptions.</summary>
    public IActiveMqConsumerEndpointQueueNameFormatter? ConsumerEndpointQueueNameFormatter { get; set; }
    /// <summary>Gets or sets the formatter applied to generated temporary queue names.</summary>
    public IActiveMqTemporaryQueueNameFormatter? TemporaryQueueNameFormatter { get; set; }

    IActiveMqMessageConsumeTopology<T> IActiveMqConsumeTopology.GetMessageTopology<T>()
    {
        return (IActiveMqMessageConsumeTopologyConfigurator<T>)base.GetMessageTopology<T>();
    }

    /// <summary>Adds a consume-topology specification.</summary>
    /// <param name="specification">The specification to add.</param>
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

    /// <summary>Applies explicit and per-message consume specifications to a receive topology.</summary>
    /// <param name="builder">The receive-topology builder.</param>
    public void Apply(IReceiveEndpointBrokerTopologyBuilder builder)
    {
        foreach (var specification in _specifications)
            specification.Apply(builder);

        ForEach<IActiveMqMessageConsumeTopologyConfigurator>(x => x.Apply(builder));
    }

    /// <summary>Binds a topic through virtual-topic or direct-topic consumer semantics.</summary>
    /// <param name="topicName">The topic name.</param>
    /// <param name="configure">An optional callback that configures the topic binding.</param>
    public void Bind(string topicName, Action<IActiveMqTopicBindingConfigurator>? configure = null)
    {
        IActiveMqTopicBindingConfigurator specification =
            string.IsNullOrEmpty(_publishTopology.VirtualTopicPrefix) || topicName.StartsWith(_publishTopology.VirtualTopicPrefix)
                ? new ConsumerConsumeTopologySpecification(topicName, ConsumerEndpointQueueNameFormatter)
                : new ConsumerConsumeTopicTopologySpecification(topicName);

        configure?.Invoke(specification);

        _specifications.Add((IActiveMqConsumeTopologySpecification)specification);
    }

    /// <summary>Creates an ActiveMQ-compatible temporary queue name without periods.</summary>
    /// <param name="tag">The descriptive name tag.</param>
    /// <returns>The optionally custom-formatted temporary queue name.</returns>
    public override string CreateTemporaryQueueName(string tag)
    {
        var queueName = new string(base.CreateTemporaryQueueName(tag).Where(c => c != '.').ToArray());

        if (TemporaryQueueNameFormatter != null)
            queueName = TemporaryQueueNameFormatter.Format(queueName);

        return queueName;
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public override IEnumerable<ValidationResult> Validate()
    {
        return base.Validate().Concat(_specifications.SelectMany(x => x.Validate()));
    }

    /// <summary>Creates consume topology for a message type.</summary>
    /// <typeparam name="T">The consumed message type.</typeparam>
    /// <returns>The new ActiveMQ message consume topology.</returns>
    protected override IMessageConsumeTopologyConfigurator CreateMessageTopology<T>()
    {
        var messageTopology = new ActiveMqMessageConsumeTopology<T>(_publishTopology.GetMessageTopology<T>(), ConsumerEndpointQueueNameFormatter);

        OnMessageTopologyCreated(messageTopology);

        return messageTopology;
    }
}
