using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.SqlTransport.Configuration;

namespace ViciOne.ServiceBus.SqlTransport.Topology;

/// <summary>Defines the topology for sql message consume.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class SqlMessageConsumeTopology<TMessage> :
    MessageConsumeTopology<TMessage>,
    ISqlMessageConsumeTopologyConfigurator<TMessage>,
    IDbMessageConsumeTopologyConfigurator
    where TMessage : class
{
    readonly ISqlMessagePublishTopology<TMessage> _publishTopology;
    readonly List<ISqlConsumeTopologySpecification> _specifications;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="publishTopology">The publish topology.</param>
    public SqlMessageConsumeTopology(ISqlMessagePublishTopology<TMessage> publishTopology)
    {
        _publishTopology = publishTopology;

        _specifications = new List<ISqlConsumeTopologySpecification>();
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(IReceiveEndpointBrokerTopologyBuilder builder)
    {
        foreach (var specification in _specifications)
            specification.Apply(builder);
    }

    /// <summary>Subscribes to the configured event source.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    public void Subscribe(Action<ISqlTopicSubscriptionConfigurator>? configure = null)
    {
        if (!IsBindableMessageType)
        {
            _specifications.Add(new InvalidSqlConsumeTopologySpecification(TypeCache<TMessage>.ShortName, "Is not a consumable message type"));
            return;
        }

        var specification = new QueueSubscriptionConsumeTopologySpecification(_publishTopology.Topic);

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
