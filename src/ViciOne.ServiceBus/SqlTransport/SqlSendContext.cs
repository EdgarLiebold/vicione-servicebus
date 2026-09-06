using System;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Exposes state for sql send operations.</summary>
/// <typeparam name="T">The value type.</typeparam>
public interface SqlSendContext<out T> :
    SqlSendContext,
    SendContext<T>
    where T : class
{
}


/// <summary>Exposes state for sql send operations.</summary>
public interface SqlSendContext :
    SendContext,
    RoutingKeySendContext,
    PartitionKeySendContext
{
    /// <summary>Gets the transport message id.</summary>
    Guid TransportMessageId { get; }

    /// <summary>Gets or sets the priority.</summary>
    public short? Priority { set; }
}
