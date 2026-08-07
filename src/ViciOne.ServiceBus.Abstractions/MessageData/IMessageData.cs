// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;


    public interface IMessageData
    {
        /// <summary>
        /// Returns the address of the message data
        /// </summary>
        Uri Address { get; }

        /// <summary>
        /// True if the value is present in the message, and not null
        /// </summary>
        bool HasValue { get; }
    }
}
