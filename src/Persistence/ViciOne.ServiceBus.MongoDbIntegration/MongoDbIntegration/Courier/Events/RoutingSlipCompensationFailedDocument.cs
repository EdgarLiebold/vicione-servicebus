// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.MongoDbIntegration.Courier.Events
{
    using Documents;
    using ViciOne.ServiceBus.Courier.Contracts;


    public class RoutingSlipCompensationFailedDocument :
        RoutingSlipEventDocument
    {
        public RoutingSlipCompensationFailedDocument(RoutingSlipCompensationFailed message)
            : base(message.Timestamp, message.Duration)
        {
            if (message.ExceptionInfo != null)
                ExceptionInfo = new ExceptionInfoDocument(message.ExceptionInfo);
        }

        public ExceptionInfoDocument ExceptionInfo { get; private set; }
    }
}
