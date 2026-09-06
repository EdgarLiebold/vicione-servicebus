using System;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>Controls the lifetime of scheduled message.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class ScheduledMessageHandle<T> :
    ScheduledMessage<T>
    where T : class
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="tokenId">The token id.</param>
    /// <param name="dueAt">The due at.</param>
    /// <param name="destination">The destination.</param>
    /// <param name="payload">The payload.</param>
    public ScheduledMessageHandle(Guid tokenId, DateTimeOffset dueAt, Uri destination, T payload)
    {
        TokenId = tokenId;
        DueAt = dueAt;
        Destination = destination;
        Payload = payload;
    }

    /// <summary>Gets the token id.</summary>
    public Guid TokenId { get; }
    /// <summary>Gets the due at.</summary>
    public DateTimeOffset DueAt { get; }
    /// <summary>Gets the destination.</summary>
    public Uri Destination { get; }
    /// <summary>Gets the payload.</summary>
    public T Payload { get; }
}
