// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Tests.Saga.Messages
{
    using System;


    [Serializable]
    public class SendValidationEmail :
        CorrelatedMessage
    {
        public SendValidationEmail(Guid correlationId)
            :
            base(correlationId)
        {
        }

        protected SendValidationEmail()
        {
        }
    }
}
