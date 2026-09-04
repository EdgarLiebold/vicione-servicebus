using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.SqlTransport.Configuration;

#nullable enable
namespace ViciOne.ServiceBus.SqlTransport.Topology;

/// <summary>
/// Provides a sql consume topology implementation.
/// </summary>
public class SqlConsumeTopology :
    ConsumeTopology,
    ISqlConsumeTopologyConfigurator
{
    readonly ISqlPublishTopology _publishTopology;
    readonly List<ISqlConsumeTopologySpecification> _specifications;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="publishTopology">The publish topology value.</param>
    public SqlConsumeTopology(ISqlPublishTopology publishTopology)
        : base(255)
    {
        _publishTopology = publishTopology;

        _specifications = new List<ISqlConsumeTopologySpecification>();
    }

    ISqlMessageConsumeTopology<T> ISqlConsumeTopology.GetMessageTopology<T>()
    {
        return (ISqlMessageConsumeTopology<T>)base.GetMessageTopology<T>();
    }

    /// <summary>
    /// Adds specification to the configuration.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    public void AddSpecification(ISqlConsumeTopologySpecification specification)
    {
        if (specification == null)
            throw new ArgumentNullException(nameof(specification));

        _specifications.Add(specification);
    }

    ISqlMessageConsumeTopologyConfigurator<T> ISqlConsumeTopologyConfigurator.GetMessageTopology<T>()
    {
        return (ISqlMessageConsumeTopologyConfigurator<T>)base.GetMessageTopology<T>();
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(IReceiveEndpointBrokerTopologyBuilder builder)
    {
        foreach (var specification in _specifications)
            specification.Apply(builder);

        ForEach<IDbMessageConsumeTopologyConfigurator>(x => x.Apply(builder));
    }

    /// <summary>
    /// Performs the subscribe operation.
    /// </summary>
    /// <param name="topicName">The topic name value.</param>
    /// <param name="configure">The configuration callback.</param>
    public void Subscribe(string topicName, Action<ISqlTopicSubscriptionConfigurator>? configure = null)
    {
        if (string.IsNullOrWhiteSpace(topicName))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(topicName));

        var specification = new QueueSubscriptionConsumeTopologySpecification(topicName);

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
        var messageTopology = new SqlMessageConsumeTopology<T>(_publishTopology.GetMessageTopology<T>());

        OnMessageTopologyCreated(messageTopology);

        return messageTopology;
    }
}
