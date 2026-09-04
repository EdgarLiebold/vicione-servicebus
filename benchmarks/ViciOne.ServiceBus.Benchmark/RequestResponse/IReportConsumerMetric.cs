using System;
using System.Threading.Tasks;

namespace ViciOneServiceBusBenchmark.RequestResponse;

public interface IReportConsumerMetric
{
    Task ConsumedAsync<T>(Guid messageId)
        where T : class;

    Task<T> ResponseReceivedAsync<T>(Guid messageId, Func<Task<T>> request)
        where T : class;
}
