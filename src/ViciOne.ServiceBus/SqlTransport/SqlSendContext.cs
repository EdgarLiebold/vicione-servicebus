using System;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>
/// Defines the contract for sql send context.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public interface SqlSendContext<out T> :
    SqlSendContext,
    SendContext<T>
    where T : class
{
}


/// <summary>
/// Defines the contract for sql send context.
/// </summary>
public interface SqlSendContext :
    SendContext,
    RoutingKeySendContext,
    PartitionKeySendContext
{
    /// <summary>
    /// Gets the transport message id value.
    /// </summary>
    Guid TransportMessageId { get; }

    /// <summary>
    /// Gets or sets the priority value.
    /// </summary>
    public short? Priority { set; }
}
