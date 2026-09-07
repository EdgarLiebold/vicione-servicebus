using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus;

namespace ViciOneServiceBusBenchmark.RequestResponse;

public class ServiceBusRequestResponseTransport :
    IRequestResponseTransport
{
    readonly ServiceBusHostSettings _hostSettings;
    readonly IRequestResponseSettings _settings;
    IBusControl _busControl;
    IClientFactory _clientFactory;

    Uri _targetEndpointAddress;

    public ServiceBusRequestResponseTransport(ServiceBusHostSettings hostSettings, IRequestResponseSettings settings)
    {
        _hostSettings = hostSettings;
        _settings = settings;
    }

    public Task<IRequestClient<T>> GetRequestClientAsync<T>(TimeSpan settingsRequestTimeout)
        where T : class
    {
        return Task.FromResult(_clientFactory.CreateRequestClient<T>(_targetEndpointAddress, settingsRequestTimeout));
    }

    public async Task StartAsync(Action<IReceiveEndpointConfigurator> configureReceiveEndpoint, CancellationToken cancellationToken = default)
    {
        _busControl = Bus.Factory.CreateUsingAzureServiceBus(x =>
        {
            x.AutoStart = true;
            x.Host(_hostSettings);

            x.ReceiveEndpoint("rpc_consumer" + (_settings.Durable ? "" : "_express"), e =>
            {
                e.PrefetchCount = _settings.PrefetchCount;
                if (_settings.ConcurrencyLimit > 0)
                    e.ConcurrentMessageLimit = _settings.ConcurrencyLimit;

                configureReceiveEndpoint(e);

                _targetEndpointAddress = e.InputAddress;
            });
        });

        await _busControl.StartAsync(cancellationToken).ConfigureAwait(false);

        _clientFactory = _busControl.CreateClientFactory();
    }

    public async ValueTask DisposeAsync()
    {
        if (_busControl is not null)
            await _busControl.StopAsync().ConfigureAwait(false);
    }
}
