namespace ViciOneServiceBusBenchmark.RequestResponse
{
    using System;
    using System.Threading.Tasks;


    public interface IReportConsumerMetric
    {
        Task Consumed<T>(Guid messageId)
            where T : class;

        Task<T> ResponseReceived<T>(Guid messageId, Func<Task<T>> request)
            where T : class;
    }
}
