namespace ViciOne.ServiceBus.Scheduling
{
    using System;


    public class CancelScheduledMessageCommand :
        CancelScheduledMessage
    {
        public CancelScheduledMessageCommand()
        {
        }

        public CancelScheduledMessageCommand(Guid tokenId, DateTime timestamp)
        {
            Timestamp = timestamp;
            TokenId = tokenId;
        }

        public DateTime Timestamp { get; set; }
        public Guid TokenId { get; set; }
    }
}
