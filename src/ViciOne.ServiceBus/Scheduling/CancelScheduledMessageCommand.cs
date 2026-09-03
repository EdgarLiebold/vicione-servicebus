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
            CorrelationId = NewId.NextGuid();
            Timestamp = timestamp;
            TokenId = tokenId;
        }

        public Guid CorrelationId { get; set; }
        public DateTime Timestamp { get; set; }
        public Guid TokenId { get; set; }
    }
}
