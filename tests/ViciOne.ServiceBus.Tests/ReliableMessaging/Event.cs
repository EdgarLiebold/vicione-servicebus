// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
#nullable enable
namespace ViciOne.ServiceBus.Tests.ReliableMessaging
{
    using System;


    public class Event
    {
        public Guid MessageId { get; set; }
        public string? Text { get; set; }
    }
}
