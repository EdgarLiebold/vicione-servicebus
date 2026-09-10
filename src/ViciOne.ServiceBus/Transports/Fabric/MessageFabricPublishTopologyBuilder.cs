using ViciOne.ServiceBus.Providers.Transports;

namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>Forwards publish-topology declarations to a message fabric.</summary>
/// <typeparam name="TMessage">The message envelope type carried by the fabric.</typeparam>
internal sealed class MessageFabricPublishTopologyBuilder<TMessage> :
    IMessageFabricPublishTopologyBuilder
    where TMessage : class
{
    readonly IMessageFabric<TMessage> _messageFabric;

    /// <summary>Initializes a builder for the specified fabric.</summary>
    /// <param name="messageFabric">The fabric that receives declarations.</param>
    public MessageFabricPublishTopologyBuilder(IMessageFabric<TMessage> messageFabric)
    {
        ArgumentNullException.ThrowIfNull(messageFabric);
        _messageFabric = messageFabric;
    }

    /// <inheritdoc />
    public string? ExchangeName { get; set; }
    /// <inheritdoc />
    public InMemoryExchangeType ExchangeType { get; set; }

    /// <inheritdoc />
    public IMessageFabricPublishTopologyBuilder CreateImplementedBuilder()
    {
        return new ImplementedBuilder(this);
    }

    /// <inheritdoc />
    public void ExchangeBind(string source, string destination, string? routingKey)
    {
        _messageFabric.ExchangeBind(source, destination, routingKey);
    }

    /// <inheritdoc />
    public void QueueBind(string source, string destination)
    {
        _messageFabric.QueueBind(source, destination);
    }

    /// <inheritdoc />
    public void ExchangeDeclare(string name, InMemoryExchangeType exchangeType)
    {
        _messageFabric.ExchangeDeclare(name, exchangeType);
    }

    /// <inheritdoc />
    public void QueueDeclare(string name)
    {
        _messageFabric.QueueDeclare(name);
    }

    sealed class ImplementedBuilder :
        IMessageFabricPublishTopologyBuilder
    {
        readonly IMessageFabricPublishTopologyBuilder _builder;
        string? _exchangeName;

        public ImplementedBuilder(IMessageFabricPublishTopologyBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            _builder = builder;
        }

        public string? ExchangeName
        {
            get => _exchangeName;
            set
            {
                _exchangeName = value;
                if (_builder.ExchangeName is { } source && _exchangeName is { } destination)
                    _builder.ExchangeBind(source, destination, _builder.ExchangeType == InMemoryExchangeType.Topic ? "#" : default);
            }
        }

        public InMemoryExchangeType ExchangeType { get; set; }

        public void ExchangeBind(string source, string destination, string? routingKey)
        {
            _builder.ExchangeBind(source, destination, routingKey);
        }

        public void QueueBind(string source, string destination)
        {
            _builder.QueueBind(source, destination);
        }

        public void ExchangeDeclare(string name, InMemoryExchangeType exchangeType)
        {
            _builder.ExchangeDeclare(name, exchangeType);
        }

        public void QueueDeclare(string name)
        {
            _builder.QueueDeclare(name);
        }

        public IMessageFabricPublishTopologyBuilder CreateImplementedBuilder()
        {
            return new ImplementedBuilder(this);
        }
    }
}
