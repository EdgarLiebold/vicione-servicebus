// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOneServiceBusBenchmark.RequestResponse
{
    using System;


    public class ResponseMessage
    {
        public ResponseMessage()
        {
        }

        public ResponseMessage(Guid correlationId)
        {
            CorrelationId = correlationId;
        }

        public Guid CorrelationId { get; set; }
    }
}
