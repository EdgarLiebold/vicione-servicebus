namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>
/// Defines the contract for message fabric observer.
/// </summary>
/// <typeparam name="TContext">The t context type.</typeparam>
public interface IMessageFabricObserver<in TContext>
    where TContext : class
{
    /// <summary>
    /// Performs the exchange declared operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="name">The name value.</param>
    /// <param name="exchangeType">The exchange type value.</param>
    void ExchangeDeclared(TContext context, string name, ExchangeType exchangeType);

    /// <summary>
    /// Performs the exchange binding created operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="source">The source value.</param>
    /// <param name="destination">The destination value.</param>
    /// <param name="routingKey">The routing key value.</param>
    void ExchangeBindingCreated(TContext context, string source, string destination, string? routingKey = default);

    /// <summary>
    /// Performs the queue declared operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="name">The name value.</param>
    void QueueDeclared(TContext context, string name);

    /// <summary>
    /// Performs the queue binding created operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="source">The source value.</param>
    /// <param name="destination">The destination value.</param>
    void QueueBindingCreated(TContext context, string source, string destination);

    /// <summary>
    /// Consumes r connected.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="handle">The handle value.</param>
    /// <param name="queueName">The queue name value.</param>
    /// <returns>The result of the operation.</returns>
    TopologyHandle ConsumerConnected(TContext context, TopologyHandle handle, string queueName);
}
