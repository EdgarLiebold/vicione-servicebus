using System;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>
/// Provides a scheduled message handle implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class ScheduledMessageHandle<T> :
    ScheduledMessage<T>
    where T : class
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="tokenId">The token id value.</param>
    /// <param name="dueAt">The due at value.</param>
    /// <param name="destination">The destination value.</param>
    /// <param name="payload">The payload value.</param>
    public ScheduledMessageHandle(Guid tokenId, DateTimeOffset dueAt, Uri destination, T payload)
    {
        TokenId = tokenId;
        DueAt = dueAt;
        Destination = destination;
        Payload = payload;
    }

    /// <summary>
    /// Gets the token id value.
    /// </summary>
    public Guid TokenId { get; }
    /// <summary>
    /// Gets the due at value.
    /// </summary>
    public DateTimeOffset DueAt { get; }
    /// <summary>
    /// Gets the destination value.
    /// </summary>
    public Uri Destination { get; }
    /// <summary>
    /// Gets the payload value.
    /// </summary>
    public T Payload { get; }
}
