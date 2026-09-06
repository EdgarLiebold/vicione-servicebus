using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.ActiveMq.Configuration;

namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>Collects ActiveMQ consume bindings for one message type.</summary>
/// <typeparam name="TMessage">The consumed message type.</typeparam>
public class ActiveMqMessageConsumeTopology<TMessage> :
    MessageConsumeTopology<TMessage>,
    IActiveMqMessageConsumeTopologyConfigurator<TMessage>,
    IActiveMqMessageConsumeTopologyConfigurator
    where TMessage : class
{
    readonly IActiveMqConsumerEndpointQueueNameFormatter? _consumerEndpointQueueNameFormatter;
    readonly IActiveMqMessagePublishTopology<TMessage> _publishTopology;
    readonly IList<IActiveMqConsumeTopologySpecification> _specifications;

    /// <summary>Creates consume topology linked to a message's publish topic.</summary>
    /// <param name="publishTopology">The publish topology that identifies the message topic.</param>
    /// <param name="consumerEndpointQueueNameFormatter">An optional formatter for virtual-topic consumer names.</param>
    public ActiveMqMessageConsumeTopology(IActiveMqMessagePublishTopology<TMessage> publishTopology,
        IActiveMqConsumerEndpointQueueNameFormatter? consumerEndpointQueueNameFormatter)
    {
        _publishTopology = publishTopology;

        _specifications = new List<IActiveMqConsumeTopologySpecification>();

        _consumerEndpointQueueNameFormatter = consumerEndpointQueueNameFormatter;
    }

    /// <summary>Applies all message-specific consume specifications to a receive topology.</summary>
    /// <param name="builder">The receive-topology builder.</param>
    public void Apply(IReceiveEndpointBrokerTopologyBuilder builder)
    {
        foreach (var specification in _specifications)
            specification.Apply(builder);
    }

    /// <summary>Binds the message's publish topic through ActiveMQ virtual-topic semantics.</summary>
    /// <param name="configure">An optional callback that configures the topic binding.</param>
    public void Bind(Action<IActiveMqTopicBindingConfigurator>? configure = null)
    {
        if (!IsBindableMessageType)
        {
            _specifications.Add(new InvalidActiveMqConsumeTopologySpecification(TypeCache<TMessage>.ShortName, "Is not a bindable message type"));
            return;
        }

        var specification = new ConsumerConsumeTopologySpecification(_publishTopology.Topic, _consumerEndpointQueueNameFormatter);

        configure?.Invoke(specification);

        _specifications.Add(specification);
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public override IEnumerable<ValidationResult> Validate()
    {
        return base.Validate().Concat(_specifications.SelectMany(x => x.Validate()));
    }
}
