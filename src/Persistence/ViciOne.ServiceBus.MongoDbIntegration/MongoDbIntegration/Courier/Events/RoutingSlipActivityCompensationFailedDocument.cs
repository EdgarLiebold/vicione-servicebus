// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.MongoDbIntegration.Courier.Events
{
    using System;
    using Documents;
    using ViciOne.ServiceBus.Courier.Contracts;


    public class RoutingSlipActivityCompensationFailedDocument :
        RoutingSlipEventDocument
    {
        public RoutingSlipActivityCompensationFailedDocument(RoutingSlipActivityCompensationFailed message)
            : base(message.Timestamp, message.Duration, message.Host)
        {
            ActivityName = message.ActivityName;
            ExecutionId = message.ExecutionId;

            if (message.ExceptionInfo != null)
                ExceptionInfo = new ExceptionInfoDocument(message.ExceptionInfo);
        }

        public string ActivityName { get; private set; }
        public Guid ExecutionId { get; private set; }
        public ExceptionInfoDocument ExceptionInfo { get; private set; }
    }
}
