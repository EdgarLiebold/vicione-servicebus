using System;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>Represents a typed message submitted to an endpoint scheduler.</summary>
/// <typeparam name="T">The message contract.</typeparam>
public sealed class ScheduleMessageCommand<T> :
    ScheduleMessage
    where T : class
{
    /// <summary>Creates an empty command for deserialization.</summary>
    public ScheduleMessageCommand()
    {
    }

    /// <summary>Creates a scheduler command for a typed message.</summary>
    /// <param name="dueAt">The requested delivery time.</param>
    /// <param name="destination">The delivery destination.</param>
    /// <param name="payload">The message payload.</param>
    /// <param name="tokenId">The scheduling token.</param>
    public ScheduleMessageCommand(DateTimeOffset dueAt, Uri destination, T payload, Guid tokenId)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(payload);

        TokenId = tokenId;
        DueAt = dueAt.ToUniversalTime();

        Destination = destination;
        Payload = payload;

        PayloadType = MessageTypeCache<T>.MessageTypeNames.ToArray();
    }

    /// <summary>Gets or sets the scheduling token.</summary>
    public Guid TokenId { get; set; }
    /// <summary>Gets or sets the requested delivery time.</summary>
    public DateTimeOffset DueAt { get; set; }
    /// <summary>Gets or sets the payload type.</summary>
    public string[] PayloadType { get; set; } = null!;
    /// <summary>Gets or sets the destination.</summary>
    public Uri Destination { get; set; } = null!;
    /// <summary>Gets or sets the payload.</summary>
    public object Payload { get; set; } = null!;
}


/// <summary>Provides the mutable representation used to deserialize endpoint-scheduler commands.</summary>
public sealed class ScheduleMessageCommand :
    ScheduleMessage
{
    /// <summary>Gets or sets the scheduling token.</summary>
    public Guid TokenId { get; set; }
    /// <summary>Gets or sets the requested delivery time.</summary>
    public DateTimeOffset DueAt { get; set; }
    /// <summary>Gets or sets the payload type.</summary>
    public string[] PayloadType { get; set; } = null!;
    /// <summary>Gets or sets the destination.</summary>
    public Uri Destination { get; set; } = null!;
    /// <summary>Gets or sets the payload.</summary>
    public object Payload { get; set; } = null!;
}
