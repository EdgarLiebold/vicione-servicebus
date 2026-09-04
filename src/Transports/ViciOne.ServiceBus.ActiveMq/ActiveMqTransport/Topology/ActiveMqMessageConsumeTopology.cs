using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.ActiveMq.Configuration;

namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>
/// Provides an active mq message consume topology implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class ActiveMqMessageConsumeTopology<TMessage> :
    MessageConsumeTopology<TMessage>,
    IActiveMqMessageConsumeTopologyConfigurator<TMessage>,
    IActiveMqMessageConsumeTopologyConfigurator
    where TMessage : class
{
    readonly IActiveMqConsumerEndpointQueueNameFormatter? _consumerEndpointQueueNameFormatter;
    readonly IActiveMqMessagePublishTopology<TMessage> _publishTopology;
    readonly IList<IActiveMqConsumeTopologySpecification> _specifications;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="publishTopology">The publish topology value.</param>
    /// <param name="consumerEndpointQueueNameFormatter">The consumer endpoint queue name formatter value.</param>
    public ActiveMqMessageConsumeTopology(IActiveMqMessagePublishTopology<TMessage> publishTopology,
        IActiveMqConsumerEndpointQueueNameFormatter? consumerEndpointQueueNameFormatter)
    {
        _publishTopology = publishTopology;

        _specifications = new List<IActiveMqConsumeTopologySpecification>();

        _consumerEndpointQueueNameFormatter = consumerEndpointQueueNameFormatter;
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
    /// Performs the bind operation.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
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

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override IEnumerable<ValidationResult> Validate()
    {
        return base.Validate().Concat(_specifications.SelectMany(x => x.Validate()));
    }
}
