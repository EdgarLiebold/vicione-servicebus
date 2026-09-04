using System;

namespace ViciOneServiceBusBenchmark.RequestResponse;

public class RequestMessage
{
    public RequestMessage()
    {
    }

    public RequestMessage(Guid correlationId)
    {
        CorrelationId = correlationId;
    }

    public Guid CorrelationId { get; set; }
}
