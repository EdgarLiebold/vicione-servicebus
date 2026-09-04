using System;

namespace ViciOne.ServiceBus.Scheduling;

public class CancelScheduledMessageCommand :
    CancelScheduledMessage
{
    public CancelScheduledMessageCommand()
    {
    }

    public CancelScheduledMessageCommand(Guid tokenId, DateTimeOffset timestamp)
    {
        Timestamp = timestamp;
        TokenId = tokenId;
    }

    public DateTimeOffset Timestamp { get; set; }
    public Guid TokenId { get; set; }
}
