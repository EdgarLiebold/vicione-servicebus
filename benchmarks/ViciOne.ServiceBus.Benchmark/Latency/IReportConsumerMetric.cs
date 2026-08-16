namespace ViciOneServiceBusBenchmark.Latency
{
    using System;
    using System.Threading.Tasks;


    public interface IReportConsumerMetric
    {
        Task Consumed<T>(Guid messageId)
            where T : class;

        Task Sent(Guid messageId, Func<Task> send, bool postSend = false);
        Task PostSend(Guid messageId);
    }
}
