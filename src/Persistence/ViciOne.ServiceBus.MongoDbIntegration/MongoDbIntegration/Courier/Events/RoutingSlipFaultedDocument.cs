// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.MongoDbIntegration.Courier.Events
{
    using System.Linq;
    using Documents;
    using ViciOne.ServiceBus.Courier.Contracts;


    public class RoutingSlipFaultedDocument :
        RoutingSlipEventDocument
    {
        public RoutingSlipFaultedDocument(RoutingSlipFaulted message)
            : base(message.Timestamp, message.Duration)
        {
            if (message.ActivityExceptions != null)
                ActivityExceptions = message.ActivityExceptions.Select(x => new ActivityExceptionDocument(x)).ToArray();
        }

        public ActivityExceptionDocument[] ActivityExceptions { get; private set; }
    }
}
