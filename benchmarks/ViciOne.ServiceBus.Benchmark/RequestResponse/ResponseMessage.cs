using System;

namespace ViciOneServiceBusBenchmark.RequestResponse;

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
