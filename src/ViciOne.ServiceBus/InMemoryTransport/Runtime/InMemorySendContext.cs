using System.Collections.Generic;
using System.Threading;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.InMemoryTransport.Runtime;

/// <summary>Represents a message being sent through the in-memory transport.</summary>
/// <typeparam name="TMessage">The message contract.</typeparam>
internal sealed class InMemorySendContext<TMessage> :
    MessageSendContext<TMessage>,
    RoutingKeySendContext
    where TMessage : class
{
    /// <summary>Creates a send context for a message.</summary>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">The token that cancels the send operation.</param>
    public InMemorySendContext(TMessage message, CancellationToken cancellationToken = default)
        : base(message, cancellationToken)
    {
    }

    /// <summary>Gets or sets the routing key.</summary>
    public string? RoutingKey { get; set; }

    /// <summary>Restores transport-specific values from a property bag.</summary>
    /// <param name="properties">The properties captured from an earlier transport context.</param>
    public override void ReadPropertiesFrom(IReadOnlyDictionary<string, object> properties)
    {
        base.ReadPropertiesFrom(properties);

        RoutingKey = ReadString(properties, InMemoryTransportPropertyNames.RoutingKey);
    }

    /// <summary>Writes the transport-specific values required to replay this send.</summary>
    /// <param name="properties">The property bag that receives the values.</param>
    public override void WritePropertiesTo(IDictionary<string, object> properties)
    {
        base.WritePropertiesTo(properties);

        if (!string.IsNullOrWhiteSpace(RoutingKey))
            properties[InMemoryTransportPropertyNames.RoutingKey] = RoutingKey!;
    }
}
