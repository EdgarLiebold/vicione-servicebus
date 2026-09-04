using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.Mediator;

namespace ViciOneServiceBusBenchmark.Latency;

public class MediatorMessageLatencyTransport :
    IMessageLatencyTransport
{
    readonly IMessageLatencySettings _settings;
    IMediator _mediator;

    public MediatorMessageLatencyTransport(IMessageLatencySettings settings)
    {
        _settings = settings;
    }

    public Task SendAsync(LatencyTestMessage message)
    {
        return _mediator.SendAsync(message);
    }

    public Task StartAsync(Action<IReceiveEndpointConfigurator> callback, IReportConsumerMetric reportConsumerMetric)
    {
        _mediator = Bus.Factory.CreateMediator(callback);

        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        return _mediator switch
        {
            IAsyncDisposable disposable => disposable.DisposeAsync(),
            _ => default
        };
    }
}
