using ViciOne.ServiceBus.Providers.Transports;

namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>Forwards receive-endpoint topology declarations to a message fabric.</summary>
/// <typeparam name="TMessage">The message envelope type carried by the fabric.</typeparam>
internal sealed class MessageFabricConsumeTopologyBuilder<TMessage> :
    IMessageFabricConsumeTopologyBuilder
    where TMessage : class
{
    readonly IMessageFabric<TMessage> _fabric;

    /// <summary>Initializes a builder for the specified fabric and receive endpoint entities.</summary>
    /// <param name="fabric">The fabric that receives declarations.</param>
    /// <param name="exchange">The receive endpoint exchange name.</param>
    /// <param name="queue">The receive endpoint queue name.</param>
    public MessageFabricConsumeTopologyBuilder(IMessageFabric<TMessage> fabric, string exchange, string queue)
    {
        ArgumentNullException.ThrowIfNull(fabric);
        ArgumentException.ThrowIfNullOrWhiteSpace(exchange);
        ArgumentException.ThrowIfNullOrWhiteSpace(queue);
        _fabric = fabric;
        Exchange = exchange;
        Queue = queue;
    }

    /// <inheritdoc />
    public string Exchange { get; }
    /// <inheritdoc />
    public string Queue { get; }
    /// <inheritdoc />
    public void ExchangeBind(string source, string destination, string? routingKey)
    {
        _fabric.ExchangeBind(source, destination, routingKey);
    }

    /// <inheritdoc />
    public void QueueBind(string source, string destination)
    {
        _fabric.QueueBind(source, destination);
    }

    /// <inheritdoc />
    public void ExchangeDeclare(string name, InMemoryExchangeType exchangeType)
    {
        _fabric.ExchangeDeclare(name, exchangeType);
    }

    /// <inheritdoc />
    public void QueueDeclare(string name)
    {
        _fabric.QueueDeclare(name);
    }
}
