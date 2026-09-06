using System;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>Carries the command for schedule message.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class ScheduleMessageCommand<T> :
    ScheduleMessage
    where T : class
{
    /// <summary>Initializes a new instance.</summary>
    public ScheduleMessageCommand()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="dueAt">The due at.</param>
    /// <param name="destination">The destination.</param>
    /// <param name="payload">The payload.</param>
    /// <param name="tokenId">The token id.</param>
    public ScheduleMessageCommand(DateTimeOffset dueAt, Uri destination, T payload, Guid tokenId)
    {
        TokenId = tokenId;

        DueAt = dueAt.ToUniversalTime();

        Destination = destination;
        Payload = payload;

        PayloadType = MessageTypeCache<T>.MessageTypeNames.ToArray();
    }

    /// <summary>Gets or sets the token id.</summary>
    public Guid TokenId { get; set; }
    /// <summary>Gets or sets the due at.</summary>
    public DateTimeOffset DueAt { get; set; }
    /// <summary>Gets or sets the payload type.</summary>
    public string[] PayloadType { get; set; } = null!;
    /// <summary>Gets or sets the destination.</summary>
    public Uri Destination { get; set; } = null!;
    /// <summary>Gets or sets the payload.</summary>
    public object Payload { get; set; } = null!;
}


/// <summary>Carries the command for schedule message.</summary>
public class ScheduleMessageCommand :
    ScheduleMessage
{
    /// <summary>Gets or sets the token id.</summary>
    public Guid TokenId { get; set; }
    /// <summary>Gets or sets the due at.</summary>
    public DateTimeOffset DueAt { get; set; }
    /// <summary>Gets or sets the payload type.</summary>
    public string[] PayloadType { get; set; } = null!;
    /// <summary>Gets or sets the destination.</summary>
    public Uri Destination { get; set; } = null!;
    /// <summary>Gets or sets the payload.</summary>
    public object Payload { get; set; } = null!;
}
