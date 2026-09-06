using System.Collections.Generic;

namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>
/// Defines the contract for message source.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public interface IMessageSource<T>
    where T : class
{
    /// <summary>
    /// Gets the sinks value.
    /// </summary>
    IEnumerable<IMessageSink<T>> Sinks { get; }

    /// <summary>
    /// Performs the connect operation.
    /// </summary>
    /// <param name="sink">The sink value.</param>
    /// <param name="routingKey">The routing key value.</param>
    /// <returns>The result of the operation.</returns>
    ConnectHandle Connect(IMessageSink<T> sink, string? routingKey);
}
