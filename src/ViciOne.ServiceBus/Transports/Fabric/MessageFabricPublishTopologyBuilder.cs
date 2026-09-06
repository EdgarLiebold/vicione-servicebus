using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>Builds message fabric publish topology components.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
/// <typeparam name="T">The value type.</typeparam>
public class MessageFabricPublishTopologyBuilder<TContext, T> :
    IMessageFabricPublishTopologyBuilder
    where TContext : class
    where T : class
{
    readonly TContext _context;
    readonly IMessageFabric<TContext, T> _messageFabric;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="messageFabric">The message fabric.</param>
    public MessageFabricPublishTopologyBuilder(TContext context, IMessageFabric<TContext, T> messageFabric)
    {
        _context = context;
        _messageFabric = messageFabric;
    }

    /// <summary>Gets or sets the exchange name.</summary>
    public string ExchangeName { get; set; } = null!;
    /// <summary>Gets or sets the exchange type.</summary>
    public ExchangeType ExchangeType { get; set; }

    /// <summary>Creates implemented builder.</summary>
    /// <returns>The created implemented builder.</returns>
    public IMessageFabricPublishTopologyBuilder CreateImplementedBuilder()
    {
        return new ImplementedBuilder(this);
    }

    /// <summary>Binds the configured exchange.</summary>
    /// <param name="source">The source value.</param>
    /// <param name="destination">The destination.</param>
    /// <param name="routingKey">The routing key.</param>
    public void ExchangeBind(string source, string destination, string? routingKey)
    {
        _messageFabric.ExchangeBind(_context, source, destination, routingKey);
    }

    /// <summary>Binds the configured queue.</summary>
    /// <param name="source">The source value.</param>
    /// <param name="destination">The destination.</param>
    public void QueueBind(string source, string destination)
    {
        _messageFabric.QueueBind(_context, source, destination);
    }

    /// <summary>Declares the configured exchange.</summary>
    /// <param name="name">The name.</param>
    /// <param name="exchangeType">The runtime exchange type used by the operation.</param>
    public void ExchangeDeclare(string name, ExchangeType exchangeType)
    {
        _messageFabric.ExchangeDeclare(_context, name, exchangeType);
    }

    /// <summary>Declares the configured queue.</summary>
    /// <param name="name">The name.</param>
    public void QueueDeclare(string name)
    {
        _messageFabric.QueueDeclare(_context, name);
    }


    class ImplementedBuilder :
        IMessageFabricPublishTopologyBuilder
    {
        readonly IMessageFabricPublishTopologyBuilder _builder;
        string _exchangeName = null!;

        public ImplementedBuilder(IMessageFabricPublishTopologyBuilder builder)
        {
            _builder = builder;
        }

        public string ExchangeName
        {
            get => _exchangeName;
            set
            {
                _exchangeName = value;
                if (_builder.ExchangeName != null)
                    _builder.ExchangeBind(_builder.ExchangeName, _exchangeName, _builder.ExchangeType == ExchangeType.Topic ? "#" : default);
            }
        }

        public ExchangeType ExchangeType { get; set; }

        public void ExchangeBind(string source, string destination, string? routingKey)
        {
            _builder.ExchangeBind(source, destination, routingKey);
        }

        public void QueueBind(string source, string destination)
        {
            _builder.QueueBind(source, destination);
        }

        public void ExchangeDeclare(string name, ExchangeType exchangeType)
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
