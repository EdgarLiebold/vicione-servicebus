// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Tests.Saga.Messages
{
    using System;


    [Serializable]
    public class UserValidated :
        CorrelatedMessage
    {
        public UserValidated(Guid correlationId)
            :
            base(correlationId)
        {
        }

        protected UserValidated()
        {
        }
    }
}
