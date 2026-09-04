using System.Collections.Generic;
using System.Threading;
using ViciOne.ServiceBus.Context;

#nullable enable
namespace ViciOne.ServiceBus.InMemoryTransport;

/// <summary>
/// Provides an in memory send context implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class InMemorySendContext<T> :
    MessageSendContext<T>,
    RoutingKeySendContext
    where T : class
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public InMemorySendContext(T message, CancellationToken cancellationToken = default)
        : base(message, cancellationToken)
    {
    }

    /// <summary>
    /// Gets or sets the routing key value.
    /// </summary>
    public string? RoutingKey { get; set; }

    /// <summary>
    /// Performs the read properties from operation.
    /// </summary>
    /// <param name="properties">The properties value.</param>
    public override void ReadPropertiesFrom(IReadOnlyDictionary<string, object> properties)
    {
        base.ReadPropertiesFrom(properties);

        RoutingKey = ReadString(properties, PropertyNames.RoutingKey);
    }

    /// <summary>
    /// Performs the write properties to operation.
    /// </summary>
    /// <param name="properties">The properties value.</param>
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
