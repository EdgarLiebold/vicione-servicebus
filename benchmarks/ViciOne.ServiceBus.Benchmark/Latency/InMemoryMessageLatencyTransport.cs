using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus;

namespace ViciOneServiceBusBenchmark.Latency;

class InMemoryMessageLatencyTransport : IMessageLatencyTransport
{
    readonly InMemoryOptionSet _optionSet;
    readonly IMessageLatencySettings _settings;
    IBusControl _busControl;
    Uri _targetAddress;
    ISendEndpoint _targetEndpoint;

    public InMemoryMessageLatencyTransport(InMemoryOptionSet optionSet, IMessageLatencySettings settings)
    {
        _optionSet = optionSet;
        _settings = settings;
    }

    public Task SendAsync(LatencyTestMessage message)
    {
        return _targetEndpoint.SendAsync(message);
    }

    public async ValueTask DisposeAsync()
    {
        await _busControl.StopAsync();
    }

    public async Task StartAsync(Action<IReceiveEndpointConfigurator> callback, IReportConsumerMetric reportConsumerMetric)
    {
        _busControl = Bus.Factory.CreateUsingInMemory(x =>
        {
            x.ConcurrentMessageLimit = _optionSet.TransportConcurrencyLimit;

            x.ReceiveEndpoint("latency_consumer", e =>
            {
                callback(e);
                _targetAddress = e.InputAddress;
            });
        });

        await _busControl.StartAsync();

        _targetEndpoint = await _busControl.GetSendEndpointAsync(_targetAddress);
    }
}
