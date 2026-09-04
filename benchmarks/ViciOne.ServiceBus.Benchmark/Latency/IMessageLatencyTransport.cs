using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus;

namespace ViciOneServiceBusBenchmark.Latency;

public interface IMessageLatencyTransport :
    IAsyncDisposable
{
    Task SendAsync(LatencyTestMessage message);

    /// <summary>
    /// The bus control
    /// </summary>
    /// <param name="callback"></param>
    /// <param name="reportConsumerMetric"></param>
    Task StartAsync(Action<IReceiveEndpointConfigurator> callback, IReportConsumerMetric reportConsumerMetric);
}
