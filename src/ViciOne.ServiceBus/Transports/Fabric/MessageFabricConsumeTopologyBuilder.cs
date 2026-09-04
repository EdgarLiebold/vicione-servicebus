using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>
/// Provides a message fabric consume topology builder implementation.
/// </summary>
/// <typeparam name="TContext">The t context type.</typeparam>
/// <typeparam name="T">The t type.</typeparam>
public class MessageFabricConsumeTopologyBuilder<TContext, T> :
    IMessageFabricConsumeTopologyBuilder
    where TContext : class
    where T : class
{
    readonly TContext _context;
    readonly IMessageFabric<TContext, T> _fabric;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="fabric">The fabric value.</param>
    public MessageFabricConsumeTopologyBuilder(TContext context, IMessageFabric<TContext, T> fabric)
    {
        _context = context;
        _fabric = fabric;
    }

    /// <summary>
    /// Gets or sets the exchange value.
    /// </summary>
    public string Exchange { get; set; } = null!;
    /// <summary>
    /// Gets or sets the queue value.
    /// </summary>
    public string Queue { get; set; } = null!;
    /// <summary>
    /// Performs the exchange bind operation.
    /// </summary>
    /// <param name="source">The source value.</param>
    /// <param name="destination">The destination value.</param>
    /// <param name="routingKey">The routing key value.</param>
    public void ExchangeBind(string source, string destination, string? routingKey)
    {
        _fabric.ExchangeBind(_context, source, destination, routingKey);
    }

    /// <summary>
    /// Performs the queue bind operation.
    /// </summary>
    /// <param name="source">The source value.</param>
    /// <param name="destination">The destination value.</param>
    public void QueueBind(string source, string destination)
    {
        _fabric.QueueBind(_context, source, destination);
    }

    /// <summary>
    /// Performs the exchange declare operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="exchangeType">The exchange type value.</param>
    public void ExchangeDeclare(string name, ExchangeType exchangeType)
    {
        _fabric.ExchangeDeclare(_context, name, exchangeType);
    }

    /// <summary>
    /// Performs the queue declare operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    public void QueueDeclare(string name)
    {
        _fabric.QueueDeclare(_context, name);
    }
}
