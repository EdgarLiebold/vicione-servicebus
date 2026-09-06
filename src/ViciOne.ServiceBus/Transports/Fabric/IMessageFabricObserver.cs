namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>Receives notifications about message fabric events.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public interface IMessageFabricObserver<in TContext>
    where TContext : class
{
    /// <summary>Reports that the exchange has been declared.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="name">The name.</param>
    /// <param name="exchangeType">The runtime exchange type used by the operation.</param>
    void ExchangeDeclared(TContext context, string name, ExchangeType exchangeType);

    /// <summary>Reports that exchange binding has been created.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="source">The source value.</param>
    /// <param name="destination">The destination.</param>
    /// <param name="routingKey">The routing key.</param>
    void ExchangeBindingCreated(TContext context, string source, string destination, string? routingKey = default);

    /// <summary>Reports that the queue has been declared.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="name">The name.</param>
    void QueueDeclared(TContext context, string name);

    /// <summary>Reports that queue binding has been created.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="source">The source value.</param>
    /// <param name="destination">The destination.</param>
    void QueueBindingCreated(TContext context, string source, string destination);

    /// <summary>Consumes r connected.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="handle">The handle.</param>
    /// <param name="queueName">The queue name.</param>
    /// <returns>The topology handle produced by the operation.</returns>
    TopologyHandle ConsumerConnected(TContext context, TopologyHandle handle, string queueName);
}
