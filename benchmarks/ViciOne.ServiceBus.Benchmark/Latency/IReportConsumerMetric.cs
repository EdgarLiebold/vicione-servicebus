using System;
using System.Threading.Tasks;

namespace ViciOneServiceBusBenchmark.Latency;

public interface IReportConsumerMetric
{
    Task Consumed<T>(Guid messageId)
        where T : class;

    Task Sent(Guid messageId, Func<Task> send, bool postSend = false);
    Task PostSend(Guid messageId);
}
