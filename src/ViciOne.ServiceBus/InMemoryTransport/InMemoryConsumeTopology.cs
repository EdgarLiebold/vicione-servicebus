using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.InMemoryTransport.Configuration;
using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.InMemoryTransport;

/// <summary>
/// Provides an in memory consume topology implementation.
/// </summary>
public class InMemoryConsumeTopology :
    ConsumeTopology,
    IInMemoryConsumeTopologyConfigurator
{
    readonly IMessageTopology _messageTopology;
    readonly IInMemoryPublishTopologyConfigurator _publishTopology;
    readonly List<IInMemoryConsumeTopologySpecification> _specifications;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="messageTopology">The message topology value.</param>
    /// <param name="publishTopology">The publish topology value.</param>
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

    /// <summary>
    /// Adds specification to the configuration.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    public void AddSpecification(IInMemoryConsumeTopologySpecification specification)
    {
        if (specification == null)
            throw new ArgumentNullException(nameof(specification));

        _specifications.Add(specification);
    }

    /// <summary>
    /// Performs the bind operation.
    /// </summary>
    /// <param name="exchangeName">The exchange name value.</param>
    /// <param name="exchangeType">The exchange type value.</param>
    /// <param name="routingKey">The routing key value.</param>
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

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(IMessageFabricConsumeTopologyBuilder builder)
    {
        foreach (var specification in _specifications)
            specification.Apply(builder);

        ForEach<IInMemoryMessageConsumeTopologyConfigurator>(x => x.Apply(builder));
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
        var topology = new InMemoryMessageConsumeTopology<T>(_messageTopology.GetMessageTopology<T>(), _publishTopology);

        OnMessageTopologyCreated(topology);

        return topology;
    }
}
