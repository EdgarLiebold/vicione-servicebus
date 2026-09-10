using System.Collections.Generic;

namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>Exposes the destinations connected to an in-memory message source.</summary>
/// <typeparam name="TMessage">The message type.</typeparam>
internal interface IMessageSource<TMessage>
    where TMessage : class
{
    /// <summary>Gets a snapshot of the connected destinations.</summary>
    IEnumerable<IMessageSink<TMessage>> Sinks { get; }

    /// <summary>Connects a destination for the specified routing key or pattern.</summary>
    /// <param name="sink">The destination to connect.</param>
    /// <param name="routingKey">The routing key or topic pattern associated with the connection.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle Connect(IMessageSink<TMessage> sink, string? routingKey);
}
