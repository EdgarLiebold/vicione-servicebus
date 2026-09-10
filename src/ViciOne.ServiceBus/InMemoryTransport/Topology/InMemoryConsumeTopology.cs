using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.InMemoryTransport.Configuration;
using ViciOne.ServiceBus.Providers.Transports;
using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.InMemoryTransport.Topology;

/// <summary>Collects exchange bindings for an in-memory receive endpoint.</summary>
internal sealed class InMemoryConsumeTopology :
    ConsumeTopology,
    IInMemoryConsumeTopologyConfigurator
{
    readonly IMessageTopology _messageTopology;
    readonly IInMemoryPublishTopologyConfigurator _publishTopology;
    readonly List<IInMemoryConsumeTopologySpecification> _specifications;

    /// <summary>Creates consume topology over shared message and publish topology.</summary>
    /// <param name="messageTopology">The message entity-name topology.</param>
    /// <param name="publishTopology">The publish topology used to infer exchange behavior.</param>
    public InMemoryConsumeTopology(IMessageTopology messageTopology, IInMemoryPublishTopologyConfigurator publishTopology)
    {
        _messageTopology = messageTopology ?? throw new ArgumentNullException(nameof(messageTopology));
        _publishTopology = publishTopology ?? throw new ArgumentNullException(nameof(publishTopology));
        _specifications = new List<IInMemoryConsumeTopologySpecification>();
    }

    IInMemoryMessageConsumeTopology<T> IInMemoryConsumeTopology.GetMessageTopology<T>()
    {
        IMessageConsumeTopologyConfigurator<T> configurator = base.GetMessageTopology<T>();

        return configurator as IInMemoryMessageConsumeTopology<T>
            ?? throw new InvalidOperationException($"The consume topology for {TypeCache<T>.ShortName} is not an in-memory topology.");
    }

    /// <summary>Adds a consume-topology specification.</summary>
    /// <param name="specification">The specification to add.</param>
    public void AddSpecification(IInMemoryConsumeTopologySpecification specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        _specifications.Add(specification);
    }

    /// <summary>Adds a named source exchange binding.</summary>
    /// <param name="exchangeName">The non-empty source exchange name.</param>
    /// <param name="exchangeType">The source exchange routing behavior.</param>
    /// <param name="routingKey">The optional direct or topic routing key.</param>
    public void Bind(
        string exchangeName,
        InMemoryExchangeType exchangeType = InMemoryExchangeType.FanOut,
        string? routingKey = default)
    {
        var specification = new ExchangeBindingConsumeTopologySpecification(exchangeName, exchangeType, routingKey);

        _specifications.Add(specification);
    }

    IInMemoryMessageConsumeTopologyConfigurator<T> IInMemoryConsumeTopologyConfigurator.GetMessageTopology<T>()
    {
        return GetMessageTopology<T>() as IInMemoryMessageConsumeTopologyConfigurator<T>
            ?? throw new InvalidOperationException($"The consume topology for {TypeCache<T>.ShortName} is not configurable.");
    }

    /// <summary>Applies endpoint-level and message-level bindings to a topology builder.</summary>
    /// <param name="builder">The consume topology builder to update.</param>
    public void Apply(IMessageFabricConsumeTopologyBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        foreach (var specification in _specifications)
            specification.Apply(builder);

        ForEach<IInMemoryMessageConsumeTopologyConfigurator>(x => x.Apply(builder));
    }

    /// <summary>Validates inherited topology and every endpoint-level binding.</summary>
    /// <returns>All consume-topology validation failures.</returns>
    public override IEnumerable<ValidationResult> Validate()
    {
        return base.Validate().Concat(_specifications.SelectMany(x => x.Validate()));
    }

    /// <summary>Creates consume topology for a message contract.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <returns>The message-specific consume topology.</returns>
    protected override IMessageConsumeTopologyConfigurator CreateMessageTopology<T>()
    {
        var topology = new InMemoryMessageConsumeTopology<T>(_messageTopology.GetMessageTopology<T>(), _publishTopology);

        OnMessageTopologyCreated(topology);

        return topology;
    }
}
