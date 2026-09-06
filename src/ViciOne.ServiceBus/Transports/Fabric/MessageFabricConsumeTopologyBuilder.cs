using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>Builds message fabric consume topology components.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
/// <typeparam name="T">The value type.</typeparam>
public class MessageFabricConsumeTopologyBuilder<TContext, T> :
    IMessageFabricConsumeTopologyBuilder
    where TContext : class
    where T : class
{
    readonly TContext _context;
    readonly IMessageFabric<TContext, T> _fabric;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="fabric">The fabric.</param>
    public MessageFabricConsumeTopologyBuilder(TContext context, IMessageFabric<TContext, T> fabric)
    {
        _context = context;
        _fabric = fabric;
    }

    /// <summary>Gets or sets the exchange.</summary>
    public string Exchange { get; set; } = null!;
    /// <summary>Gets or sets the queue.</summary>
    public string Queue { get; set; } = null!;
    /// <summary>Binds the configured exchange.</summary>
    /// <param name="source">The source value.</param>
    /// <param name="destination">The destination.</param>
    /// <param name="routingKey">The routing key.</param>
    public void ExchangeBind(string source, string destination, string? routingKey)
    {
        _fabric.ExchangeBind(_context, source, destination, routingKey);
    }

    /// <summary>Binds the configured queue.</summary>
    /// <param name="source">The source value.</param>
    /// <param name="destination">The destination.</param>
    public void QueueBind(string source, string destination)
    {
        _fabric.QueueBind(_context, source, destination);
    }

    /// <summary>Declares the configured exchange.</summary>
    /// <param name="name">The name.</param>
    /// <param name="exchangeType">The runtime exchange type used by the operation.</param>
    public void ExchangeDeclare(string name, ExchangeType exchangeType)
    {
        _fabric.ExchangeDeclare(_context, name, exchangeType);
    }

    /// <summary>Declares the configured queue.</summary>
    /// <param name="name">The name.</param>
    public void QueueDeclare(string name)
    {
        _fabric.QueueDeclare(_context, name);
    }
}
