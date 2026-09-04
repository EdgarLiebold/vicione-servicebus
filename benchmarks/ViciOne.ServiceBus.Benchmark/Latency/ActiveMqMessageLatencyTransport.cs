using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus;

namespace ViciOneServiceBusBenchmark.Latency;

public class ActiveMqMessageLatencyTransport :
    IMessageLatencyTransport
{
    readonly ActiveMqHostSettings _hostSettings;
    readonly IMessageLatencySettings _settings;
    IBusControl _busControl;
    Uri _targetAddress;
    ISendEndpoint _targetEndpoint;

    public ActiveMqMessageLatencyTransport(ActiveMqHostSettings hostSettings, IMessageLatencySettings settings)
    {
        _hostSettings = hostSettings;
        _settings = settings;
    }

    public Task SendAsync(LatencyTestMessage message)
    {
        return _targetEndpoint.SendAsync(message);
    }

    public async Task StartAsync(Action<IReceiveEndpointConfigurator> callback, IReportConsumerMetric reportConsumerMetric)
    {
        _busControl = Bus.Factory.CreateUsingActiveMq(x =>
        {
            x.Host(_hostSettings);

            x.ReceiveEndpoint("latency_consumer" + (_settings.Durable ? "" : "_express"), e =>
            {
                e.Durable = _settings.Durable;
                e.PrefetchCount = _settings.PrefetchCount;

                if (_settings.ConcurrencyLimit > 0)
                    e.ConcurrentMessageLimit = _settings.ConcurrencyLimit;

                callback(e);

                _targetAddress = e.InputAddress;
            });
        });

        await _busControl.StartAsync();

        _targetEndpoint = await _busControl.GetSendEndpointAsync(_targetAddress);
    }

    public async ValueTask DisposeAsync()
    {
        await _busControl.StopAsync();
    }
}
