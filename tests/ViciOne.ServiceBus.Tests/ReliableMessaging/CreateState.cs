// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Tests.ReliableMessaging
{
    using System;


    public class CreateState
    {
        public Guid CorrelationId { get; set; }
        public bool FailOnFirstAttempt { get; set; }
        public bool FailMessageDelivery { get; set; }
    }
}
