namespace ViciOne.ServiceBus.Scheduling
{
    using System;


    public interface CancelScheduledMessage
    {
        /// <summary>
        /// The date/time this message was created
        /// </summary>
        DateTime Timestamp { get; }

        /// <summary>
        /// The token of the scheduled message
        /// </summary>
        Guid TokenId { get; }
    }
}
