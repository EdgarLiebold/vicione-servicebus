// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.MongoDbIntegration.Courier.Events
{
    using System.Collections.Generic;
    using ViciOne.ServiceBus.Courier.Contracts;


    public class RoutingSlipCompletedDocument :
        RoutingSlipEventDocument
    {
        public RoutingSlipCompletedDocument(RoutingSlipCompleted message)
            : base(message.Timestamp, message.Duration)
        {
            Variables = message.Variables;
        }

        public IDictionary<string, object> Variables { get; private set; }
    }
}
