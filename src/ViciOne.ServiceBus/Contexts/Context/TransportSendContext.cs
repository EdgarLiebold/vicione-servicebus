using System.Collections.Generic;

namespace ViciOne.ServiceBus.Context;

/// <summary>Exposes serialized body and replayable transport properties for an outgoing message.</summary>
public interface TransportSendContext :
    PublishContext
{
    /// <summary>Gets the serialized message body.</summary>
    MessageBody Body { get; }

    /// <summary>Writes transport-specific properties into a durable property bag.</summary>
    /// <param name="properties">The property bag to populate.</param>
    void WritePropertiesTo(IDictionary<string, object> properties);

    /// <summary>Restores transport-specific properties from a durable property bag.</summary>
    /// <param name="properties">The stored property bag.</param>
    void ReadPropertiesFrom(IReadOnlyDictionary<string, object> properties);
}

/// <summary>Exposes a typed outgoing message together with its transport send state.</summary>
/// <typeparam name="TMessage">The outgoing message contract.</typeparam>
public interface TransportSendContext<out TMessage> :
    PublishContext<TMessage>,
    TransportSendContext
    where TMessage : class
{
}
