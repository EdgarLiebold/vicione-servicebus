// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Tests.Messages
{
    using System;


    [Serializable]
    public class RequestMessage :
        CorrelatedBy<Guid>
    {
        public RequestMessage()
        {
            CorrelationId = Guid.NewGuid();
        }

        public Guid CorrelationId { get; set; }
    }
}
