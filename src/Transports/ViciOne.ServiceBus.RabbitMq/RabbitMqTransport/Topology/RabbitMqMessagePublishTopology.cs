using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using RabbitMQ.Client;
using ViciOne.ServiceBus.RabbitMq.Configuration;
using ViciOne.ServiceBus.Topology;

#nullable enable
namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>
/// Provides a rabbit mq message publish topology implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class RabbitMqMessagePublishTopology<TMessage> :
    MessagePublishTopology<TMessage>,
    IRabbitMqMessagePublishTopologyConfigurator<TMessage>
    where TMessage : class
{
    readonly RabbitMqExchangeConfigurator _exchange;
    readonly List<IRabbitMqMessagePublishTopology> _implementedMessageTypes;
    readonly IRabbitMqPublishTopology _publishTopology;
    readonly List<IRabbitMqPublishTopologySpecification> _specifications;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="publishTopology">The publish topology value.</param>
    /// <param name="messageTopology">The message topology value.</param>
    /// <param name="exchangeTypeSelector">The exchange type selector value.</param>
    public RabbitMqMessagePublishTopology(IRabbitMqPublishTopology publishTopology, IMessageTopology<TMessage> messageTopology,
        IMessageExchangeTypeSelector<TMessage> exchangeTypeSelector)
        : base(publishTopology)
    {
        _publishTopology = publishTopology;
        ExchangeTypeSelector = exchangeTypeSelector;

        var exchangeName = messageTopology.EntityName;
        var exchangeType = exchangeTypeSelector.GetExchangeType(exchangeName);

        var temporary = MessageTypeCache<TMessage>.IsTemporaryMessageType;

        var durable = !temporary;
        var autoDelete = temporary;

        _exchange = new RabbitMqExchangeConfigurator(exchangeName, exchangeType, durable, autoDelete);

        _implementedMessageTypes = new List<IRabbitMqMessagePublishTopology>();
        _specifications = new List<IRabbitMqPublishTopologySpecification>();
    }

    IMessageExchangeTypeSelector<TMessage> ExchangeTypeSelector { get; }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(IPublishEndpointBrokerTopologyBuilder builder)
    {
        if (Exclude)
            return;

        var exchangeHandle = builder.ExchangeDeclare(_exchange.ExchangeName, _exchange.ExchangeType, _exchange.Durable, _exchange.AutoDelete,
            _exchange.ExchangeArguments);

        if (builder.Exchange is { } parentExchange)
        {
            var routingKey = parentExchange.Exchange.ExchangeType == ExchangeType.Topic
                ? "#"
                : "";

            builder.ExchangeBind(parentExchange, exchangeHandle, routingKey, new Dictionary<string, object?>());
        }
        else
            builder.Exchange = exchangeHandle;

        for (var i = 0; i < _specifications.Count; i++)
            _specifications[i].Apply(builder);

        foreach (var configurator in _implementedMessageTypes)
            configurator.Apply(builder);
    }

    /// <summary>
    /// Attempts to get publish address.
    /// </summary>
    /// <param name="baseAddress">The base address value.</param>
    /// <param name="publishAddress">The publish address value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public override bool TryGetPublishAddress(Uri baseAddress, [NotNullWhen(true)] out Uri? publishAddress)
    {
        publishAddress = _exchange.GetEndpointAddress(baseAddress);
        return true;
    }

    /// <summary>
    /// Gets send settings.
    /// </summary>
    /// <param name="hostAddress">The host address value.</param>
    /// <returns>The result of the operation.</returns>
    public SendSettings GetSendSettings(Uri hostAddress)
    {
        return new RabbitMqSendSettings(_exchange.GetEndpointAddress(hostAddress));
    }

    /// <summary>
    /// Gets broker topology.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public BrokerTopology GetBrokerTopology()
    {
        var builder = new PublishEndpointBrokerTopologyBuilder(_publishTopology.BrokerTopologyOptions);

        Apply(builder);

        return builder.BuildBrokerTopology();
    }

    /// <summary>
    /// Performs the apply broker topology operation.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void ApplyBrokerTopology(IPublishEndpointBrokerTopologyBuilder builder)
    {
        Apply(builder);
    }

    /// <summary>
    /// Gets the exchange value.
    /// </summary>
    public Exchange Exchange => _exchange;

    bool IRabbitMqExchangeConfigurator.Durable
    {
        set => _exchange.Durable = value;
    }

    bool IRabbitMqExchangeConfigurator.AutoDelete
    {
        set => _exchange.AutoDelete = value;
    }

    string IRabbitMqExchangeConfigurator.ExchangeType
    {
        set => _exchange.ExchangeType = value;
    }

    void IRabbitMqExchangeConfigurator.SetExchangeArgument(string key, object? value)
    {
        _exchange.SetExchangeArgument(key, value);
    }

    void IRabbitMqExchangeConfigurator.SetExchangeArgument(string key, TimeSpan value)
    {
        _exchange.SetExchangeArgument(key, value);
    }

    /// <summary>
    /// Gets or sets the alternate exchange value.
    /// </summary>
    public string AlternateExchange
    {
        set => _exchange.SetExchangeArgument(RabbitMQ.Client.Headers.AlternateExchange, value);
    }

    /// <summary>
    /// Performs the bind queue operation.
    /// </summary>
    /// <param name="exchangeName">The exchange name value.</param>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="configure">The configuration callback.</param>
    public void BindQueue(string exchangeName, string? queueName, Action<IRabbitMqQueueBindingConfigurator>? configure)
    {
        if (string.IsNullOrWhiteSpace(exchangeName))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(exchangeName));

        var exchangeType = ExchangeTypeSelector.DefaultExchangeType;

        var specification = new ExchangeToQueueBindingPublishTopologySpecification(exchangeName, exchangeType, queueName);

        configure?.Invoke(specification);

        _specifications.Add(specification);
    }

    /// <summary>
    /// Performs the bind alternate exchange queue operation.
    /// </summary>
    /// <param name="exchangeName">The exchange name value.</param>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="configure">The configuration callback.</param>
    public void BindAlternateExchangeQueue(string exchangeName, string? queueName, Action<IRabbitMqQueueBindingConfigurator>? configure)
    {
        BindQueue(exchangeName, queueName, configure);

        AlternateExchange = exchangeName;
    }

    /// <summary>
    /// Adds implemented message configurator to the configuration.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="direct">The direct value.</param>
    public void AddImplementedMessageConfigurator<T>(IRabbitMqMessagePublishTopologyConfigurator<T> configurator, bool direct)
        where T : class
    {
        var adapter = new ImplementedTypeAdapter<T>(configurator, direct);

        _implementedMessageTypes.Add(adapter);
    }


    class ImplementedTypeAdapter<T> :
        IRabbitMqMessagePublishTopology
        where T : class
    {
        readonly IRabbitMqMessagePublishTopologyConfigurator<T> _configurator;
        readonly bool _direct;

        public ImplementedTypeAdapter(IRabbitMqMessagePublishTopologyConfigurator<T> configurator, bool direct)
        {
            _configurator = configurator;
            _direct = direct;
        }

        public void Apply(IPublishEndpointBrokerTopologyBuilder builder)
        {
            if (_direct)
            {
                var implementedBuilder = builder.CreateImplementedBuilder();

                _configurator.Apply(implementedBuilder);
            }
        }
    }
}
