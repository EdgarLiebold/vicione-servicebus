namespace ViciOne.ServiceBus.Transports;

/// <summary>Formats transport routing keys for arbitrary message contracts.</summary>
public interface IRoutingKeyFormatter
{
    /// <summary>Formats the routing key used by transports that support routed delivery.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="context">The message send context.</param>
    /// <returns>The non-null routing key to assign to the transport message.</returns>
    string FormatRoutingKey<T>(SendContext<T> context)
        where T : class;
}
