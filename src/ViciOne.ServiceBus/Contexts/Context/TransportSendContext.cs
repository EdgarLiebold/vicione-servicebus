using System.Collections.Generic;

namespace ViciOne.ServiceBus.Context;

/// <summary>
/// Defines the contract for transport send context.
/// </summary>
public interface TransportSendContext :
    PublishContext
{
    /// <summary>
    /// Gets the serialized body used by the transport. The value is created once and shared by
    /// transport-adjacent features so serialization is never repeated with a divergent result.
    /// </summary>
    MessageBody Body { get; }

    /// <summary>
    /// Performs the write properties to operation.
    /// </summary>
    /// <param name="properties">The properties value.</param>
    void WritePropertiesTo(IDictionary<string, object> properties);

    /// <summary>
    /// Performs the read properties from operation.
    /// </summary>
    /// <param name="properties">The properties value.</param>
    void ReadPropertiesFrom(IReadOnlyDictionary<string, object> properties);
}


/// <summary>
/// Defines the contract for transport send context.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface TransportSendContext<out TMessage> :
    PublishContext<TMessage>,
    TransportSendContext
    where TMessage : class
{
}
