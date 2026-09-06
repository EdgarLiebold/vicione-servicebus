using System.Collections.Generic;
using System.Threading;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.InMemoryTransport;

/// <summary>Carries state for in memory send operations.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class InMemorySendContext<T> :
    MessageSendContext<T>,
    RoutingKeySendContext
    where T : class
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public InMemorySendContext(T message, CancellationToken cancellationToken = default)
        : base(message, cancellationToken)
    {
    }

    /// <summary>Gets or sets the routing key.</summary>
    public string? RoutingKey { get; set; }

    /// <summary>Reads properties from.</summary>
    /// <param name="properties">The properties.</param>
    public override void ReadPropertiesFrom(IReadOnlyDictionary<string, object> properties)
    {
        base.ReadPropertiesFrom(properties);

        RoutingKey = ReadString(properties, PropertyNames.RoutingKey);
    }

    /// <summary>Writes properties to.</summary>
    /// <param name="properties">The properties.</param>
    public override void WritePropertiesTo(IDictionary<string, object> properties)
    {
        base.WritePropertiesTo(properties);

        if (!string.IsNullOrWhiteSpace(RoutingKey))
            properties[PropertyNames.RoutingKey] = RoutingKey!;
    }


    static class PropertyNames
    {
        public const string RoutingKey = "RoutingKey";
    }
}
