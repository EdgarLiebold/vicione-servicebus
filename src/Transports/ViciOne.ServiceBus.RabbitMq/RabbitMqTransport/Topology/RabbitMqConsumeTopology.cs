using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.RabbitMq.Configuration;

namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>Collects RabbitMQ receive bindings and per-message consume topology.</summary>
public class RabbitMqConsumeTopology :
    ConsumeTopology,
    IRabbitMqConsumeTopologyConfigurator
{
    readonly IMessageTopology _messageTopology;
    readonly IRabbitMqPublishTopology _publishTopology;
    readonly List<IRabbitMqConsumeTopologySpecification> _specifications;

    /// <summary>Creates a RabbitMQ consume topology with fanout exchanges by default.</summary>
    /// <param name="messageTopology">The message metadata used to derive entity names.</param>
    /// <param name="publishTopology">The publish topology used for consumed message contracts.</param>
    public RabbitMqConsumeTopology(IMessageTopology messageTopology, IRabbitMqPublishTopology publishTopology)
        : base(255)
    {
        _messageTopology = messageTopology;
        _publishTopology = publishTopology;
        ExchangeTypeSelector = new FanoutExchangeTypeSelector();

        _specifications = new List<IRabbitMqConsumeTopologySpecification>();
    }

    /// <summary>Gets the selector used to determine exchange types for consumed message contracts.</summary>
    public IExchangeTypeSelector ExchangeTypeSelector { get; }

    IRabbitMqMessageConsumeTopology<T> IRabbitMqConsumeTopology.GetMessageTopology<T>()
    {
        return base.GetMessageTopology<T>() as IRabbitMqMessageConsumeTopologyConfigurator<T>
            ?? throw new InvalidOperationException($"The message topology for '{typeof(T)}' is not a RabbitMQ consume topology.");
    }

    /// <summary>Adds a receive-topology specification.</summary>
    /// <param name="specification">The specification to apply when the endpoint topology is built.</param>
    public void AddSpecification(IRabbitMqConsumeTopologySpecification specification)
    {
        if (specification == null)
            throw new ArgumentNullException(nameof(specification));

        _specifications.Add(specification);
    }

    IRabbitMqMessageConsumeTopologyConfigurator<T> IRabbitMqConsumeTopologyConfigurator.GetMessageTopology<T>()
    {
        return base.GetMessageTopology<T>() as IRabbitMqMessageConsumeTopologyConfigurator<T>
            ?? throw new InvalidOperationException($"The message topology for '{typeof(T)}' is not a RabbitMQ consume topology.");
    }

    /// <summary>Applies explicit bindings followed by every configured message topology.</summary>
    /// <param name="builder">The receive-endpoint topology builder.</param>
    public void Apply(IReceiveEndpointBrokerTopologyBuilder builder)
    {
        foreach (var specification in _specifications)
            specification.Apply(builder);

        ForEach<IRabbitMqMessageConsumeTopologyConfigurator>(x => x.Apply(builder));
    }

    /// <summary>Adds a binding from a named source exchange to the receive endpoint exchange.</summary>
    /// <param name="exchangeName">The exchange name.</param>
    /// <param name="configure">An optional callback that customizes the source exchange and binding.</param>
    public void Bind(string exchangeName, Action<IRabbitMqExchangeToExchangeBindingConfigurator>? configure = null)
    {
        if (string.IsNullOrWhiteSpace(exchangeName))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(exchangeName));

        var exchangeType = ExchangeTypeSelector.DefaultExchangeType;

        var specification = new ExchangeBindingConsumeTopologySpecification(exchangeName, exchangeType);

        configure?.Invoke(specification);

        _specifications.Add(specification);
    }

    /// <summary>Adds a binding from a named exchange to a named queue.</summary>
    /// <param name="exchangeName">The exchange name.</param>
    /// <param name="queueName">The queue name.</param>
    /// <param name="configure">An optional callback that customizes the exchange, queue, and binding.</param>
    public void BindQueue(string exchangeName, string queueName, Action<IRabbitMqQueueBindingConfigurator>? configure = null)
    {
        if (string.IsNullOrWhiteSpace(exchangeName))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(exchangeName));

        var exchangeType = ExchangeTypeSelector.DefaultExchangeType;

        var specification = new ExchangeToQueueBindingConsumeTopologySpecification(exchangeName, exchangeType, queueName);

        configure?.Invoke(specification);

        _specifications.Add(specification);
    }

    /// <summary>Validates the message topologies and all explicit binding specifications.</summary>
    /// <returns>Every validation failure found in the consume topology.</returns>
    public override IEnumerable<ValidationResult> Validate()
    {
        return base.Validate().Concat(_specifications.SelectMany(x => x.Validate()));
    }

    /// <summary>Creates the RabbitMQ consume topology for a message contract.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <returns>The message-specific consume topology.</returns>
    protected override IMessageConsumeTopologyConfigurator CreateMessageTopology<T>()
    {
        var exchangeTypeSelector = new MessageExchangeTypeSelector<T>(ExchangeTypeSelector);

        var messageTopology = new RabbitMqMessageConsumeTopology<T>(_messageTopology.GetMessageTopology<T>(), exchangeTypeSelector,
            _publishTopology.GetMessageTopology<T>());

        OnMessageTopologyCreated(messageTopology);

        return messageTopology;
    }
}
