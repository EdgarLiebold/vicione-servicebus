namespace ViciOne.ServiceBus.Transports;

/// <summary>Formats transport partition keys for arbitrary message contracts.</summary>
public interface IPartitionKeyFormatter
{
    /// <summary>Formats the partition key used by transports that support partitioned delivery.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="context">The message send context.</param>
    /// <returns>The non-null partition key to assign to the transport message.</returns>
    string FormatPartitionKey<T>(SendContext<T> context)
        where T : class;
}
