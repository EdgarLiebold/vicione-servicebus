// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Tests.Saga.Messages
{
    using System;


    [Serializable]
    public class UserVerificationEmailSent :
        CorrelatedMessage
    {
        public UserVerificationEmailSent(Guid correlationId, string email)
            :
            base(correlationId)
        {
            Email = email;
        }

        protected UserVerificationEmailSent()
        {
        }

        public string Email { get; set; }
    }
}
