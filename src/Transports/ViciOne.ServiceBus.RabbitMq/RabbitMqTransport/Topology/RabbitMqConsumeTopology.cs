using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.RabbitMq.Configuration;

namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>
/// Provides a rabbit mq consume topology implementation.
/// </summary>
public class RabbitMqConsumeTopology :
    ConsumeTopology,
    IRabbitMqConsumeTopologyConfigurator
{
    readonly IMessageTopology _messageTopology;
    readonly IRabbitMqPublishTopology _publishTopology;
    readonly List<IRabbitMqConsumeTopologySpecification> _specifications;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="messageTopology">The message topology value.</param>
    /// <param name="publishTopology">The publish topology value.</param>
    public RabbitMqConsumeTopology(IMessageTopology messageTopology, IRabbitMqPublishTopology publishTopology)
        : base(255)
    {
        _messageTopology = messageTopology;
        _publishTopology = publishTopology;
        ExchangeTypeSelector = new FanoutExchangeTypeSelector();

        _specifications = new List<IRabbitMqConsumeTopologySpecification>();
    }

    /// <summary>
    /// Gets the exchange type selector value.
    /// </summary>
    public IExchangeTypeSelector ExchangeTypeSelector { get; }

    IRabbitMqMessageConsumeTopology<T> IRabbitMqConsumeTopology.GetMessageTopology<T>()
    {
        return base.GetMessageTopology<T>() as IRabbitMqMessageConsumeTopologyConfigurator<T>
            ?? throw new InvalidOperationException($"The message topology for '{typeof(T)}' is not a RabbitMQ consume topology.");
    }

    /// <summary>
    /// Adds specification to the configuration.
    /// </summary>
    /// <param name="specification">The specification value.</param>
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

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(IReceiveEndpointBrokerTopologyBuilder builder)
    {
        foreach (var specification in _specifications)
            specification.Apply(builder);

        ForEach<IRabbitMqMessageConsumeTopologyConfigurator>(x => x.Apply(builder));
    }

    /// <summary>
    /// Performs the bind operation.
    /// </summary>
    /// <param name="exchangeName">The exchange name value.</param>
    /// <param name="configure">The configuration callback.</param>
    public void Bind(string exchangeName, Action<IRabbitMqExchangeToExchangeBindingConfigurator>? configure = null)
    {
        if (string.IsNullOrWhiteSpace(exchangeName))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(exchangeName));

        var exchangeType = ExchangeTypeSelector.DefaultExchangeType;

        var specification = new ExchangeBindingConsumeTopologySpecification(exchangeName, exchangeType);

        configure?.Invoke(specification);

        _specifications.Add(specification);
    }

    /// <summary>
    /// Performs the bind queue operation.
    /// </summary>
    /// <param name="exchangeName">The exchange name value.</param>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="configure">The configuration callback.</param>
    public void BindQueue(string exchangeName, string queueName, Action<IRabbitMqQueueBindingConfigurator>? configure = null)
    {
        if (string.IsNullOrWhiteSpace(exchangeName))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(exchangeName));

        var exchangeType = ExchangeTypeSelector.DefaultExchangeType;

        var specification = new ExchangeToQueueBindingConsumeTopologySpecification(exchangeName, exchangeType, queueName);

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
        var exchangeTypeSelector = new MessageExchangeTypeSelector<T>(ExchangeTypeSelector);

        var messageTopology = new RabbitMqMessageConsumeTopology<T>(_messageTopology.GetMessageTopology<T>(), exchangeTypeSelector,
            _publishTopology.GetMessageTopology<T>());

        OnMessageTopologyCreated(messageTopology);

        return messageTopology;
    }
}
