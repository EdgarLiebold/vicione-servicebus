using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.SqlTransport.Configuration;

#nullable enable
namespace ViciOne.ServiceBus.SqlTransport.Topology;

/// <summary>
/// Provides a sql message consume topology implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class SqlMessageConsumeTopology<TMessage> :
    MessageConsumeTopology<TMessage>,
    ISqlMessageConsumeTopologyConfigurator<TMessage>,
    IDbMessageConsumeTopologyConfigurator
    where TMessage : class
{
    readonly ISqlMessagePublishTopology<TMessage> _publishTopology;
    readonly List<ISqlConsumeTopologySpecification> _specifications;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="publishTopology">The publish topology value.</param>
    public SqlMessageConsumeTopology(ISqlMessagePublishTopology<TMessage> publishTopology)
    {
        _publishTopology = publishTopology;

        _specifications = new List<ISqlConsumeTopologySpecification>();
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

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override IEnumerable<ValidationResult> Validate()
    {
        return base.Validate().Concat(_specifications.SelectMany(x => x.Validate()));
    }
}
