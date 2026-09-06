using System.Collections.Generic;

namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>Defines the operations required by message source.</summary>
/// <typeparam name="T">The value type.</typeparam>
public interface IMessageSource<T>
    where T : class
{
    /// <summary>Gets the sinks.</summary>
    IEnumerable<IMessageSink<T>> Sinks { get; }

    /// <summary>Connects the configured observer or endpoint.</summary>
    /// <param name="sink">The sink.</param>
    /// <param name="routingKey">The routing key.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ConnectHandle Connect(IMessageSink<T> sink, string? routingKey);
}
