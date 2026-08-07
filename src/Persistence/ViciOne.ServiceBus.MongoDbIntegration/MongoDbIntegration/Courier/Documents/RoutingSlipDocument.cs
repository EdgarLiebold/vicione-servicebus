// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.MongoDbIntegration.Courier.Documents
{
    using System;
    using Events;


    public class RoutingSlipDocument
    {
        public RoutingSlipDocument(Guid trackingNumber)
        {
            TrackingNumber = trackingNumber;
        }

        public Guid TrackingNumber { get; private set; }
        public RoutingSlipEventDocument[] Events { get; private set; }
    }
}
