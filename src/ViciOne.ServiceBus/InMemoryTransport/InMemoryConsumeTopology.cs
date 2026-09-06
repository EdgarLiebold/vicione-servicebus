using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.InMemoryTransport.Configuration;
using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.InMemoryTransport;

/// <summary>Defines the topology for in memory consume.</summary>
public class InMemoryConsumeTopology :
    ConsumeTopology,
    IInMemoryConsumeTopologyConfigurator
{
    readonly IMessageTopology _messageTopology;
    readonly IInMemoryPublishTopologyConfigurator _publishTopology;
    readonly List<IInMemoryConsumeTopologySpecification> _specifications;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="messageTopology">The message topology.</param>
    /// <param name="publishTopology">The publish topology.</param>
    public InMemoryConsumeTopology(IMessageTopology messageTopology, IInMemoryPublishTopologyConfigurator publishTopology)
    {
        _messageTopology = messageTopology;
        _publishTopology = publishTopology;
        _specifications = new List<IInMemoryConsumeTopologySpecification>();
    }

    IInMemoryMessageConsumeTopology<T> IInMemoryConsumeTopology.GetMessageTopology<T>()
    {
        IMessageConsumeTopologyConfigurator<T> configurator = base.GetMessageTopology<T>();

        return configurator as IInMemoryMessageConsumeTopology<T>
            ?? throw new InvalidOperationException($"The consume topology for {TypeCache<T>.ShortName} is not an in-memory topology.");
    }

    /// <summary>Adds specification to the configuration.</summary>
    /// <param name="specification">The specification.</param>
    public void AddSpecification(IInMemoryConsumeTopologySpecification specification)
    {
        if (specification == null)
            throw new ArgumentNullException(nameof(specification));

        _specifications.Add(specification);
    }

    /// <summary>Binds the configured entities.</summary>
    /// <param name="exchangeName">The exchange name.</param>
    /// <param name="exchangeType">The runtime exchange type used by the operation.</param>
    /// <param name="routingKey">The routing key.</param>
    public void Bind(string exchangeName, ExchangeType exchangeType = ExchangeType.FanOut, string? routingKey = default)
    {
        var specification = new ExchangeBindingConsumeTopologySpecification(exchangeName, exchangeType, routingKey);

        _specifications.Add(specification);
    }

    IInMemoryMessageConsumeTopologyConfigurator<T> IInMemoryConsumeTopologyConfigurator.GetMessageTopology<T>()
    {
        return GetMessageTopology<T>() as IInMemoryMessageConsumeTopologyConfigurator<T>
            ?? throw new InvalidOperationException($"The consume topology for {TypeCache<T>.ShortName} is not configurable.");
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(IMessageFabricConsumeTopologyBuilder builder)
    {
        foreach (var specification in _specifications)
            specification.Apply(builder);

        ForEach<IInMemoryMessageConsumeTopologyConfigurator>(x => x.Apply(builder));
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
        var topology = new InMemoryMessageConsumeTopology<T>(_messageTopology.GetMessageTopology<T>(), _publishTopology);

        OnMessageTopologyCreated(topology);

        return topology;
    }
}
