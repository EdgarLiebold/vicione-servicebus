using System.Collections.Generic;

namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>
/// Provides a publish endpoint broker topology builder implementation.
/// </summary>
public class PublishEndpointBrokerTopologyBuilder :
    BrokerTopologyBuilder,
    IPublishEndpointBrokerTopologyBuilder
{
    readonly PublishBrokerTopologyOptions _options;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="options">The options value.</param>
    public PublishEndpointBrokerTopologyBuilder(PublishBrokerTopologyOptions options = PublishBrokerTopologyOptions.FlattenHierarchy)
    {
        _options = options;
    }

    /// <summary>
    /// The exchange to which the published message is sent
    /// </summary>
    public ExchangeHandle? Exchange { get; set; }

    /// <summary>
    /// Creates implemented builder.
    /// </summary>
    /// <returns>The result of the operation.</returns>
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
