using System;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>Requests cancellation of a scheduled message by token.</summary>
public sealed class CancelScheduledMessageCommand :
    CancelScheduledMessage
{
    /// <summary>Creates an empty command for deserialization.</summary>
    public CancelScheduledMessageCommand()
    {
    }

    /// <summary>Creates a cancellation command.</summary>
    /// <param name="tokenId">The scheduling token.</param>
    /// <param name="timestamp">The time at which cancellation was requested.</param>
    public CancelScheduledMessageCommand(Guid tokenId, DateTimeOffset timestamp)
    {
        Timestamp = timestamp;
        TokenId = tokenId;
    }

    /// <summary>Gets or sets the time at which cancellation was requested.</summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>Gets or sets the scheduling token.</summary>
    public Guid TokenId { get; set; }
}
