// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.MongoDbIntegration.Tests.Courier
{
    using System;
    using System.Collections.Generic;
    using ViciOne.ServiceBus.Courier.Contracts;


    public class RoutingSlipCompletedEvent :
        RoutingSlipCompleted
    {
        public RoutingSlipCompletedEvent(Guid trackingNumber, DateTime timestamp, TimeSpan duration)
        {
            Timestamp = timestamp;
            Duration = duration;
            TrackingNumber = trackingNumber;

            Variables = new Dictionary<string, object>
            {
                { "Client", 27 },
                { "Reason", "Because I said so" }
            };
        }

        public Guid TrackingNumber { get; private set; }
        public DateTime Timestamp { get; private set; }

        public TimeSpan Duration { get; private set; }

        public IDictionary<string, object> Variables { get; private set; }
    }
}
