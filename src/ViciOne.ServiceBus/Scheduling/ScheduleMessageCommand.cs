using System;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>
/// Provides a schedule message command implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class ScheduleMessageCommand<T> :
    ScheduleMessage
    where T : class
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public ScheduleMessageCommand()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="dueAt">The due at value.</param>
    /// <param name="destination">The destination value.</param>
    /// <param name="payload">The payload value.</param>
    /// <param name="tokenId">The token id value.</param>
    public ScheduleMessageCommand(DateTimeOffset dueAt, Uri destination, T payload, Guid tokenId)
    {
        TokenId = tokenId;

        DueAt = dueAt.ToUniversalTime();

        Destination = destination;
        Payload = payload;

        PayloadType = MessageTypeCache<T>.MessageTypeNames;
    }

    /// <summary>
    /// Gets or sets the token id value.
    /// </summary>
    public Guid TokenId { get; set; }
    /// <summary>
    /// Gets or sets the due at value.
    /// </summary>
    public DateTimeOffset DueAt { get; set; }
    /// <summary>
    /// Gets or sets the payload type value.
    /// </summary>
    public string[] PayloadType { get; set; } = null!;
    /// <summary>
    /// Gets or sets the destination value.
    /// </summary>
    public Uri Destination { get; set; } = null!;
    /// <summary>
    /// Gets or sets the payload value.
    /// </summary>
    public object Payload { get; set; } = null!;
}


/// <summary>
/// Provides a schedule message command implementation.
/// </summary>
public class ScheduleMessageCommand :
    ScheduleMessage
{
    /// <summary>
    /// Gets or sets the token id value.
    /// </summary>
    public Guid TokenId { get; set; }
    /// <summary>
    /// Gets or sets the due at value.
    /// </summary>
    public DateTimeOffset DueAt { get; set; }
    /// <summary>
    /// Gets or sets the payload type value.
    /// </summary>
    public string[] PayloadType { get; set; } = null!;
    /// <summary>
    /// Gets or sets the destination value.
    /// </summary>
    public Uri Destination { get; set; } = null!;
    /// <summary>
    /// Gets or sets the payload value.
    /// </summary>
    public object Payload { get; set; } = null!;
}
