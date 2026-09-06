using System.Collections.Generic;

namespace ViciOne.ServiceBus.Context;

/// <summary>Exposes state for transport send operations.</summary>
public interface TransportSendContext :
    PublishContext
{
    /// <summary>Gets the body.</summary>
    MessageBody Body { get; }

    /// <summary>Writes properties to.</summary>
    /// <param name="properties">The properties.</param>
    void WritePropertiesTo(IDictionary<string, object> properties);

    /// <summary>Reads properties from.</summary>
    /// <param name="properties">The properties.</param>
    void ReadPropertiesFrom(IReadOnlyDictionary<string, object> properties);
}


/// <summary>Exposes state for transport send operations.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface TransportSendContext<out TMessage> :
    PublishContext<TMessage>,
    TransportSendContext
    where TMessage : class
{
}
