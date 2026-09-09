using System;

namespace ViciOne.ServiceBus.Providers.Persistence;

/// <summary>Represents a decoded seek position ordered by descending quarantine time and ascending durable-send identity.</summary>
/// <param name="QuarantinedAt">The last quarantine timestamp returned to the caller.</param>
/// <param name="Id">The last durable-send identity returned at that timestamp.</param>
public readonly record struct DurableSendQuarantineSeek(DateTimeOffset QuarantinedAt, DurableSendId Id)
{
    /// <summary>Gets whether the seek contains a nonempty durable-send identity.</summary>
    public bool HasValue => Id.Value != Guid.Empty;
}
