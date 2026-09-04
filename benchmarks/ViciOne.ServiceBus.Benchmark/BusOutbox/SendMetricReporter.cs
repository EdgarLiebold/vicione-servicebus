using System;
using System.Threading.Tasks;
using ViciOneServiceBusBenchmark.Latency;

namespace ViciOneServiceBusBenchmark.BusOutbox;

/// <summary>
/// The send observer's entire decision, separated from <c>SendContext</c> so it can be proved
/// without standing up a bus.
/// <para>
/// It exists because the observer used to call the capture and then return
/// <see cref="Task.CompletedTask" />, which threw away whatever the capture had to say. A completion
/// for a message the capture never registered then vanished, and the run reported a latency nobody
/// had measured. Returning the capture's own task is the whole fix, and a separated function is what
/// lets a test hold it.
/// </para>
/// </summary>
public static class SendMetricReporter
{
    public static Task Report(IReportConsumerMetric metric, Guid? messageId)
    {
        if (metric == null)
            throw new ArgumentNullException(nameof(metric));

        if (messageId == null)
            throw new InvalidOperationException("The bus outbox send observer saw a message without an id.");

        return metric.PostSend(messageId.Value);
    }
}
