using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>
/// Provides a message fabric observable implementation.
/// </summary>
/// <typeparam name="TContext">The t context type.</typeparam>
public class MessageFabricObservable<TContext> :
    Connectable<IMessageFabricObserver<TContext>>,
    IMessageFabricObserver<TContext>
    where TContext : class
{
    /// <summary>
    /// Performs the exchange declared operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="name">The name value.</param>
    /// <param name="exchangeType">The exchange type value.</param>
    public void ExchangeDeclared(TContext context, string name, ExchangeType exchangeType)
    {
        ForEach(x => x.ExchangeDeclared(context, name, exchangeType));
    }

    /// <summary>
    /// Performs the exchange binding created operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="source">The source value.</param>
    /// <param name="destination">The destination value.</param>
    /// <param name="routingKey">The routing key value.</param>
    public void ExchangeBindingCreated(TContext context, string source, string destination, string? routingKey)
    {
        ForEach(x => x.ExchangeBindingCreated(context, source, destination, routingKey));
    }

    /// <summary>
    /// Performs the queue declared operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="name">The name value.</param>
    public void QueueDeclared(TContext context, string name)
    {
        ForEach(x => x.QueueDeclared(context, name));
    }

    /// <summary>
    /// Performs the queue binding created operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="source">The source value.</param>
    /// <param name="destination">The destination value.</param>
    public void QueueBindingCreated(TContext context, string source, string destination)
    {
        ForEach(x => x.QueueBindingCreated(context, source, destination));
    }

    /// <summary>
    /// Consumes r connected.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="handle">The handle value.</param>
    /// <param name="queueName">The queue name value.</param>
    /// <returns>The result of the operation.</returns>
    public TopologyHandle ConsumerConnected(TContext context, TopologyHandle handle, string queueName)
    {
        ForEach(x => handle = x.ConsumerConnected(context, handle, queueName));

        return handle;
    }
}
