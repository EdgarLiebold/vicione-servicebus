using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.SqlTransport.Configuration;

namespace ViciOne.ServiceBus.SqlTransport.Topology;

/// <summary>Defines the topology for sql consume.</summary>
public class SqlConsumeTopology :
    ConsumeTopology,
    ISqlConsumeTopologyConfigurator
{
    readonly ISqlPublishTopology _publishTopology;
    readonly List<ISqlConsumeTopologySpecification> _specifications;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="publishTopology">The publish topology.</param>
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

    /// <summary>Adds specification to the configuration.</summary>
    /// <param name="specification">The specification.</param>
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

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(IReceiveEndpointBrokerTopologyBuilder builder)
    {
        foreach (var specification in _specifications)
            specification.Apply(builder);

        ForEach<IDbMessageConsumeTopologyConfigurator>(x => x.Apply(builder));
    }

    /// <summary>Subscribes to the configured event source.</summary>
    /// <param name="topicName">The topic name.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public void Subscribe(string topicName, Action<ISqlTopicSubscriptionConfigurator>? configure = null)
    {
        if (string.IsNullOrWhiteSpace(topicName))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(topicName));

        var specification = new QueueSubscriptionConsumeTopologySpecification(topicName);

        configure?.Invoke(specification);

        _specifications.Add(specification);
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public override IEnumerable<ValidationResult> Validate()
    {
        return base.Validate().Concat(_specifications.SelectMany(x => x.Validate()));
    }

    /// <summary>Creates message topology.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The created message topology.</returns>
    protected override IMessageConsumeTopologyConfigurator CreateMessageTopology<T>()
    {
        var messageTopology = new SqlMessageConsumeTopology<T>(_publishTopology.GetMessageTopology<T>());

        OnMessageTopologyCreated(messageTopology);

        return messageTopology;
    }
}
