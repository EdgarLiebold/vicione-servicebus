using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus;

namespace ViciOneServiceBusBenchmark.RequestResponse;

public class RabbitMqRequestResponseTransport :
    IRequestResponseTransport
{
    readonly RabbitMqHostSettings _hostSettings;
    readonly IRequestResponseSettings _settings;
    IBusControl _busControl;
    IClientFactory _clientFactory;
    Uri _targetEndpointAddress;

    public RabbitMqRequestResponseTransport(RabbitMqHostSettings hostSettings, IRequestResponseSettings settings)
    {
        _hostSettings = hostSettings;
        _settings = settings;
    }

    public async Task<IRequestClient<T>> GetRequestClientAsync<T>(TimeSpan settingsRequestTimeout)
        where T : class
    {
        return _clientFactory.CreateRequestClient<T>(_targetEndpointAddress, new RequestTimeout(settingsRequestTimeout));
    }

    public async Task StartAsync(Action<IReceiveEndpointConfigurator> configureReceiveEndpoint, CancellationToken cancellationToken = default)
    {
        _busControl = Bus.Factory.CreateUsingRabbitMq(x =>
        {
            x.AutoStart = true;
            x.Host(_hostSettings);

            x.ReceiveEndpoint("rpc_consumer" + (_settings.Durable ? "" : "_express"), e =>
            {
                e.PurgeOnStartup = true;
                e.Durable = _settings.Durable;
                e.PrefetchCount = _settings.PrefetchCount;

                configureReceiveEndpoint(e);

                _targetEndpointAddress = e.InputAddress;
            });
        });

        await _busControl.StartAsync(cancellationToken).ConfigureAwait(false);

        _clientFactory = _busControl.CreateReplyToClientFactory();
    }

    public async ValueTask DisposeAsync()
    {
        if (_busControl is not null)
            await _busControl.StopAsync().ConfigureAwait(false);
    }
}
