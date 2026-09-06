using System.Collections.Generic;

namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>Builds publish exchanges and optionally preserves implemented-message exchange hierarchy.</summary>
public class PublishEndpointBrokerTopologyBuilder :
    BrokerTopologyBuilder,
    IPublishEndpointBrokerTopologyBuilder
{
    readonly PublishBrokerTopologyOptions _options;

    /// <summary>Creates a publish topology builder.</summary>
    /// <param name="options">The hierarchy behavior applied to implemented message contracts.</param>
    public PublishEndpointBrokerTopologyBuilder(PublishBrokerTopologyOptions options = PublishBrokerTopologyOptions.FlattenHierarchy)
    {
        _options = options;
    }

    /// <summary>Gets or sets the exchange to which the current message contract is published.</summary>
    public ExchangeHandle? Exchange { get; set; }

    /// <summary>Creates a child builder for an implemented message contract.</summary>
    /// <returns>A hierarchy-aware child builder, or this builder when implemented contracts are flattened.</returns>
    public IPublishEndpointBrokerTopologyBuilder CreateImplementedBuilder()
    {
        if (_options.HasFlag(PublishBrokerTopologyOptions.MaintainHierarchy))
            return new ImplementedBuilder(this, _options);

        return this;
    }


    class ImplementedBuilder :
        IPublishEndpointBrokerTopologyBuilder
    {
        readonly IPublishEndpointBrokerTopologyBuilder _builder;
        readonly PublishBrokerTopologyOptions _options;
        ExchangeHandle? _exchange;

        public ImplementedBuilder(IPublishEndpointBrokerTopologyBuilder builder, PublishBrokerTopologyOptions options)
        {
            _builder = builder;
            _options = options;
        }

        public ExchangeHandle? Exchange
        {
            get => _exchange;
            set
            {
                _exchange = value;
                if (_builder.Exchange is { } parentExchange && _exchange is { } exchange)
                    _builder.ExchangeBind(parentExchange, exchange, "", new Dictionary<string, object?>());
            }
        }

        public IPublishEndpointBrokerTopologyBuilder CreateImplementedBuilder()
        {
            if (_options.HasFlag(PublishBrokerTopologyOptions.MaintainHierarchy))
                return new ImplementedBuilder(this, _options);

            return this;
        }

        public ExchangeHandle ExchangeDeclare(string name, string type, bool durable, bool autoDelete, IDictionary<string, object?> arguments)
        {
            return _builder.ExchangeDeclare(name, type, durable, autoDelete, arguments);
        }

        public ExchangeBindingHandle ExchangeBind(ExchangeHandle source, ExchangeHandle destination, string routingKey,
            IDictionary<string, object?> arguments)
        {
            return _builder.ExchangeBind(source, destination, routingKey, arguments);
        }

        public QueueHandle QueueDeclare(string name, bool durable, bool autoDelete, bool exclusive, IDictionary<string, object?> arguments)
        {
            return _builder.QueueDeclare(name, durable, autoDelete, exclusive, arguments);
        }

        public QueueBindingHandle QueueBind(ExchangeHandle exchange, QueueHandle queue, string routingKey, IDictionary<string, object?> arguments)
        {
            return _builder.QueueBind(exchange, queue, routingKey, arguments);
        }
    }
}
