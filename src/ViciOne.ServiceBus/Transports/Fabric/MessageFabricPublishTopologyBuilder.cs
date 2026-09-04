using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>
/// Provides a message fabric publish topology builder implementation.
/// </summary>
/// <typeparam name="TContext">The t context type.</typeparam>
/// <typeparam name="T">The t type.</typeparam>
public class MessageFabricPublishTopologyBuilder<TContext, T> :
    IMessageFabricPublishTopologyBuilder
    where TContext : class
    where T : class
{
    readonly TContext _context;
    readonly IMessageFabric<TContext, T> _messageFabric;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="messageFabric">The message fabric value.</param>
    public MessageFabricPublishTopologyBuilder(TContext context, IMessageFabric<TContext, T> messageFabric)
    {
        _context = context;
        _messageFabric = messageFabric;
    }

    /// <summary>
    /// Gets or sets the exchange name value.
    /// </summary>
    public string ExchangeName { get; set; } = null!;
    /// <summary>
    /// Gets or sets the exchange type value.
    /// </summary>
    public ExchangeType ExchangeType { get; set; }

    /// <summary>
    /// Creates implemented builder.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IMessageFabricPublishTopologyBuilder CreateImplementedBuilder()
    {
        return new ImplementedBuilder(this);
    }

    /// <summary>
    /// Performs the exchange bind operation.
    /// </summary>
    /// <param name="source">The source value.</param>
    /// <param name="destination">The destination value.</param>
    /// <param name="routingKey">The routing key value.</param>
    public void ExchangeBind(string source, string destination, string? routingKey)
    {
        _messageFabric.ExchangeBind(_context, source, destination, routingKey);
    }

    /// <summary>
    /// Performs the queue bind operation.
    /// </summary>
    /// <param name="source">The source value.</param>
    /// <param name="destination">The destination value.</param>
    public void QueueBind(string source, string destination)
    {
        _messageFabric.QueueBind(_context, source, destination);
    }

    /// <summary>
    /// Performs the exchange declare operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="exchangeType">The exchange type value.</param>
    public void ExchangeDeclare(string name, ExchangeType exchangeType)
    {
        _messageFabric.ExchangeDeclare(_context, name, exchangeType);
    }

    /// <summary>
    /// Performs the queue declare operation.
    /// </summary>
    /// <param name="name">The name value.</param>
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
