using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus;

namespace ViciOneServiceBusBenchmark.RequestResponse;

public class InMemoryRequestResponseTransport :
    IRequestResponseTransport
{
    readonly InMemoryOptionSet _optionSet;
    IBusControl _busControl;

    IClientFactory _clientFactory;
    IRequestResponseSettings _settings;

    Uri _targetEndpointAddress;

    public InMemoryRequestResponseTransport(InMemoryOptionSet optionSet, IRequestResponseSettings settings)
    {
        _optionSet = optionSet;
        _settings = settings;
    }

    public async Task StartAsync(Action<IReceiveEndpointConfigurator> configureReceiveEndpoint, CancellationToken cancellationToken = default)
    {
        _busControl = Bus.Factory.CreateUsingInMemory(x =>
        {
            x.AutoStart = true;
            x.ConcurrentMessageLimit = _optionSet.TransportConcurrencyLimit;

            x.ReceiveEndpoint("rpc_consumer", e =>
            {
                configureReceiveEndpoint(e);
                _targetEndpointAddress = e.InputAddress;
            });
        });

        await _busControl.StartAsync(cancellationToken).ConfigureAwait(false);

        _clientFactory = _busControl.CreateReplyToClientFactory();
    }

    public async Task<IRequestClient<T>> GetRequestClientAsync<T>(TimeSpan settingsRequestTimeout)
        where T : class
    {
        return _clientFactory.CreateRequestClient<T>(_targetEndpointAddress, new RequestTimeout(settingsRequestTimeout));
    }

    public async ValueTask DisposeAsync()
    {
        if (_busControl is not null)
            await _busControl.StopAsync().ConfigureAwait(false);
    }
}
