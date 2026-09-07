using System;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>Identifies a scheduled message and its original payload.</summary>
/// <typeparam name="T">The scheduled message contract.</typeparam>
public sealed class ScheduledMessageHandle<T> :
    ScheduledMessage<T>
    where T : class
{
    /// <summary>Creates a handle for an accepted scheduling operation.</summary>
    /// <param name="tokenId">The scheduling token.</param>
    /// <param name="dueAt">The requested delivery time.</param>
    /// <param name="destination">The delivery destination.</param>
    /// <param name="payload">The original message payload.</param>
    public ScheduledMessageHandle(Guid tokenId, DateTimeOffset dueAt, Uri destination, T payload)
    {
        TokenId = tokenId;
        DueAt = dueAt;
        Destination = destination ?? throw new ArgumentNullException(nameof(destination));
        Payload = payload ?? throw new ArgumentNullException(nameof(payload));
    }

    /// <summary>Gets the scheduling token.</summary>
    public Guid TokenId { get; }
    /// <summary>Gets the requested delivery time.</summary>
    public DateTimeOffset DueAt { get; }
    /// <summary>Gets the delivery destination.</summary>
    public Uri Destination { get; }
    /// <summary>Gets the original message payload.</summary>
    public T Payload { get; }
}
