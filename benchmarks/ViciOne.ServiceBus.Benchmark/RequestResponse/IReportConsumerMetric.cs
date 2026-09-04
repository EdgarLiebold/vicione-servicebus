using System;
using System.Threading.Tasks;

namespace ViciOneServiceBusBenchmark.RequestResponse;

public interface IReportConsumerMetric
{
    Task Consumed<T>(Guid messageId)
        where T : class;

    Task<T> ResponseReceived<T>(Guid messageId, Func<Task<T>> request)
        where T : class;
}
