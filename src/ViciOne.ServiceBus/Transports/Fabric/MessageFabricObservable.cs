using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>Publishes observations for message fabric.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public class MessageFabricObservable<TContext> :
    Connectable<IMessageFabricObserver<TContext>>,
    IMessageFabricObserver<TContext>
    where TContext : class
{
    /// <summary>Reports that the exchange has been declared.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="name">The name.</param>
    /// <param name="exchangeType">The runtime exchange type used by the operation.</param>
    public void ExchangeDeclared(TContext context, string name, ExchangeType exchangeType)
    {
        ForEach(x => x.ExchangeDeclared(context, name, exchangeType));
    }

    /// <summary>Reports that exchange binding has been created.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="source">The source value.</param>
    /// <param name="destination">The destination.</param>
    /// <param name="routingKey">The routing key.</param>
    public void ExchangeBindingCreated(TContext context, string source, string destination, string? routingKey)
    {
        ForEach(x => x.ExchangeBindingCreated(context, source, destination, routingKey));
    }

    /// <summary>Reports that the queue has been declared.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="name">The name.</param>
    public void QueueDeclared(TContext context, string name)
    {
        ForEach(x => x.QueueDeclared(context, name));
    }

    /// <summary>Reports that queue binding has been created.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="source">The source value.</param>
    /// <param name="destination">The destination.</param>
    public void QueueBindingCreated(TContext context, string source, string destination)
    {
        ForEach(x => x.QueueBindingCreated(context, source, destination));
    }

    /// <summary>Consumes r connected.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="handle">The handle.</param>
    /// <param name="queueName">The queue name.</param>
    /// <returns>The topology handle produced by the operation.</returns>
    public TopologyHandle ConsumerConnected(TContext context, TopologyHandle handle, string queueName)
    {
        ForEach(x => handle = x.ConsumerConnected(context, handle, queueName));

        return handle;
    }
}
