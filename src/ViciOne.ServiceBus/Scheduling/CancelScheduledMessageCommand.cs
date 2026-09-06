using System;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>Carries the command for cancel scheduled message.</summary>
public class CancelScheduledMessageCommand :
    CancelScheduledMessage
{
    /// <summary>Initializes a new instance.</summary>
    public CancelScheduledMessageCommand()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="tokenId">The token id.</param>
    /// <param name="timestamp">The timestamp.</param>
    public CancelScheduledMessageCommand(Guid tokenId, DateTimeOffset timestamp)
    {
        Timestamp = timestamp;
        TokenId = tokenId;
    }

    /// <summary>Gets or sets the timestamp.</summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>Gets or sets the token id.</summary>
    public Guid TokenId { get; set; }
}
