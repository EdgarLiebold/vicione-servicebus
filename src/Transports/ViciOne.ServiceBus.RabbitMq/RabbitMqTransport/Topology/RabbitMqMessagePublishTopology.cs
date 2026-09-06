using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using RabbitMQ.Client;
using ViciOne.ServiceBus.RabbitMq.Configuration;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>Builds the RabbitMQ publish exchange and related bindings for one message contract.</summary>
/// <typeparam name="TMessage">The published message contract type.</typeparam>
public class RabbitMqMessagePublishTopology<TMessage> :
    MessagePublishTopology<TMessage>,
    IRabbitMqMessagePublishTopologyConfigurator<TMessage>
    where TMessage : class
{
    readonly RabbitMqExchangeConfigurator _exchange;
    readonly List<IRabbitMqMessagePublishTopology> _implementedMessageTypes;
    readonly IRabbitMqPublishTopology _publishTopology;
    readonly List<IRabbitMqPublishTopologySpecification> _specifications;

    /// <summary>Creates publish topology for a message contract.</summary>
    /// <param name="publishTopology">The parent RabbitMQ publish topology.</param>
    /// <param name="messageTopology">The message metadata that supplies the exchange name.</param>
    /// <param name="exchangeTypeSelector">The selector that determines the exchange type.</param>
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

    /// <summary>Declares the message exchange, its hierarchy binding, and configured subordinate topology.</summary>
    /// <param name="builder">The publish-endpoint topology builder.</param>
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

    /// <summary>Builds the publish address for this message exchange.</summary>
    /// <param name="baseAddress">The RabbitMQ host address.</param>
    /// <param name="publishAddress">Receives the configured exchange address.</param>
    /// <returns>Always <see langword="true"/> because RabbitMQ publish topology always has an exchange address.</returns>
    public override bool TryGetPublishAddress(Uri baseAddress, [NotNullWhen(true)] out Uri? publishAddress)
    {
        publishAddress = _exchange.GetEndpointAddress(baseAddress);
        return true;
    }

    /// <summary>Creates send settings for this message exchange.</summary>
    /// <param name="hostAddress">The RabbitMQ host address.</param>
    /// <returns>The exchange send settings.</returns>
    public SendSettings GetSendSettings(Uri hostAddress)
    {
        return new RabbitMqSendSettings(_exchange.GetEndpointAddress(hostAddress));
    }

    /// <summary>Builds the complete broker topology required to publish this message contract.</summary>
    /// <returns>The publish broker topology.</returns>
    public BrokerTopology GetBrokerTopology()
    {
        var builder = new PublishEndpointBrokerTopologyBuilder(_publishTopology.BrokerTopologyOptions);

        Apply(builder);

        return builder.BuildBrokerTopology();
    }

    /// <summary>Applies this message contract's publish topology to an existing builder.</summary>
    /// <param name="builder">The publish-endpoint topology builder.</param>
    public void ApplyBrokerTopology(IPublishEndpointBrokerTopologyBuilder builder)
    {
        Apply(builder);
    }

    /// <summary>Gets this message contract's exchange declaration.</summary>
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

    /// <summary>Sets the alternate exchange used for unroutable messages.</summary>
    public string AlternateExchange
    {
        set => _exchange.SetExchangeArgument(RabbitMQ.Client.Headers.AlternateExchange, value);
    }

    /// <summary>Adds an exchange-to-queue binding to this message's publish topology.</summary>
    /// <param name="exchangeName">The exchange to declare and bind.</param>
    /// <param name="queueName">The queue to declare, or <see langword="null"/> to use the exchange name.</param>
    /// <param name="configure">An optional callback that customizes the exchange, queue, and binding.</param>
    public void BindQueue(string exchangeName, string? queueName, Action<IRabbitMqQueueBindingConfigurator>? configure)
    {
        if (string.IsNullOrWhiteSpace(exchangeName))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(exchangeName));

        var exchangeType = ExchangeTypeSelector.DefaultExchangeType;

        var specification = new ExchangeToQueueBindingPublishTopologySpecification(exchangeName, exchangeType, queueName);

        configure?.Invoke(specification);

        _specifications.Add(specification);
    }

    /// <summary>Adds an exchange-to-queue binding and selects that exchange as the alternate exchange.</summary>
    /// <param name="exchangeName">The alternate exchange to declare.</param>
    /// <param name="queueName">The queue to bind, or <see langword="null"/> to use the exchange name.</param>
    /// <param name="configure">An optional callback that customizes the exchange, queue, and binding.</param>
    public void BindAlternateExchangeQueue(string exchangeName, string? queueName, Action<IRabbitMqQueueBindingConfigurator>? configure)
    {
        BindQueue(exchangeName, queueName, configure);

        AlternateExchange = exchangeName;
    }

    /// <summary>Adds publish topology for a directly implemented message contract.</summary>
    /// <typeparam name="T">The implemented message contract type.</typeparam>
    /// <param name="configurator">The implemented contract's publish topology.</param>
    /// <param name="direct">Whether <typeparamref name="T"/> is implemented directly by <typeparamref name="TMessage"/>.</param>
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
