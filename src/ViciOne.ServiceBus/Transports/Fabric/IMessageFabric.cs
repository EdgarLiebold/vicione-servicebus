using ViciOne.ServiceBus.InMemoryTransport;

namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>Defines the operations required by message fabric.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
/// <typeparam name="T">The value type.</typeparam>
public interface IMessageFabric<TContext, T> :
    IMessageFabricObserverConnector<TContext>,
    IAgent,
    IProbeSite
    where T : class
    where TContext : class
{
    /// <summary>Gets the delay provider.</summary>
    IInMemoryDelayProvider DelayProvider { get; }

    /// <summary>Declares the configured exchange.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="name">The name.</param>
    /// <param name="exchangeType">The runtime exchange type used by the operation.</param>
    void ExchangeDeclare(TContext context, string name, ExchangeType exchangeType);

    /// <summary>Binds the configured exchange.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="source">The source value.</param>
    /// <param name="destination">The destination.</param>
    /// <param name="routingKey">The routing key.</param>
    void ExchangeBind(TContext context, string source, string destination, string? routingKey);

    /// <summary>Declares the configured queue.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="name">The name.</param>
    void QueueDeclare(TContext context, string name);

    /// <summary>Binds the configured queue.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="source">The source value.</param>
    /// <param name="destination">The destination.</param>
    void QueueBind(TContext context, string source, string destination);

    /// <summary>Gets exchange.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="name">The name.</param>
    /// <param name="exchangeType">The runtime exchange type used by the operation.</param>
    /// <returns>The exchange.</returns>
    IMessageExchange<T> GetExchange(TContext context, string name, ExchangeType exchangeType = ExchangeType.FanOut);

    /// <summary>Gets queue.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="name">The name.</param>
    /// <returns>The queue.</returns>
    IMessageQueue<TContext, T> GetQueue(TContext context, string name);
}
