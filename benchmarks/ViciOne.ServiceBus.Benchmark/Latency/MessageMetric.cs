using System;

namespace ViciOneServiceBusBenchmark.Latency;

public class MessageMetric
{
    public MessageMetric(Guid messageId, long sendCompletionLatency, long consumeLatency)
    {
        MessageId = messageId;
        SendCompletionLatency = sendCompletionLatency;
        ConsumeLatency = consumeLatency;
    }

    public Guid MessageId { get; }
    public long SendCompletionLatency { get; set; }
    public long ConsumeLatency { get; set; }
}
