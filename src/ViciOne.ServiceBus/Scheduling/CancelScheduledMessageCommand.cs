using System;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>
/// Provides a cancel scheduled message command implementation.
/// </summary>
public class CancelScheduledMessageCommand :
    CancelScheduledMessage
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public CancelScheduledMessageCommand()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="tokenId">The token id value.</param>
    /// <param name="timestamp">The timestamp value.</param>
    public CancelScheduledMessageCommand(Guid tokenId, DateTimeOffset timestamp)
    {
        Timestamp = timestamp;
        TokenId = tokenId;
    }

    /// <summary>
    /// Gets or sets the timestamp value.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>
    /// Gets or sets the token id value.
    /// </summary>
    public Guid TokenId { get; set; }
}
