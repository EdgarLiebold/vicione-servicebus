using System;
using System.Threading.Tasks;

namespace ViciOneServiceBusBenchmark.Latency;

public interface IReportConsumerMetric
{
    Task ConsumedAsync<T>(Guid messageId)
        where T : class;

    Task SentAsync(Guid messageId, Func<Task> send, bool postSend = false);
    Task PostSendAsync(Guid messageId);
}
