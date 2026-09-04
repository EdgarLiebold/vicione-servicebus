using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus;
using ViciOneServiceBusBenchmark.Latency;

namespace ViciOneServiceBusBenchmark.BusOutbox;

public class BusOutboxMessageConsumer :
    IConsumer<BusOutboxMessage>
{
    public static int CurrentConsumerCount;
    public static int MaxConsumerCount;
    readonly IReportConsumerMetric _report;

    public BusOutboxMessageConsumer(IReportConsumerMetric report)
    {
        _report = report;
    }

    public async Task ConsumeAsync(ConsumeContext<BusOutboxMessage> context)
    {
        var current = Interlocked.Increment(ref CurrentConsumerCount);
        var maxConsumerCount = MaxConsumerCount;
        if (current > maxConsumerCount)
            Interlocked.CompareExchange(ref MaxConsumerCount, current, maxConsumerCount);

        try
        {
            await _report.ConsumedAsync<LatencyTestMessage>(context.Message.CorrelationId).ConfigureAwait(false);
        }
        finally
        {
            Interlocked.Decrement(ref CurrentConsumerCount);
        }
    }
}
