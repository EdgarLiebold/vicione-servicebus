using ViciOne.ServiceBus.InMemoryTransport;

namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>
/// Defines the contract for message fabric.
/// </summary>
/// <typeparam name="TContext">The t context type.</typeparam>
/// <typeparam name="T">The t type.</typeparam>
public interface IMessageFabric<TContext, T> :
    IMessageFabricObserverConnector<TContext>,
    IAgent,
    IProbeSite
    where T : class
    where TContext : class
{
    /// <summary>
    /// Gets the delay provider value.
    /// </summary>
    IInMemoryDelayProvider DelayProvider { get; }

    /// <summary>
    /// Performs the exchange declare operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="name">The name value.</param>
    /// <param name="exchangeType">The exchange type value.</param>
    void ExchangeDeclare(TContext context, string name, ExchangeType exchangeType);

    /// <summary>
    /// Performs the exchange bind operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="source">The source value.</param>
    /// <param name="destination">The destination value.</param>
    /// <param name="routingKey">The routing key value.</param>
    void ExchangeBind(TContext context, string source, string destination, string? routingKey);

    /// <summary>
    /// Performs the queue declare operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="name">The name value.</param>
    void QueueDeclare(TContext context, string name);

    /// <summary>
    /// Performs the queue bind operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="source">The source value.</param>
    /// <param name="destination">The destination value.</param>
    void QueueBind(TContext context, string source, string destination);

    /// <summary>
    /// Gets exchange.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="name">The name value.</param>
    /// <param name="exchangeType">The exchange type value.</param>
    /// <returns>The result of the operation.</returns>
    IMessageExchange<T> GetExchange(TContext context, string name, ExchangeType exchangeType = ExchangeType.FanOut);

    /// <summary>
    /// Gets queue.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="name">The name value.</param>
    /// <returns>The result of the operation.</returns>
    IMessageQueue<TContext, T> GetQueue(TContext context, string name);
}
