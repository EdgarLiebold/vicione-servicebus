using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.Mediator;

namespace ViciOneServiceBusBenchmark.RequestResponse;

public class MediatorRequestResponseTransport :
    IRequestResponseTransport
{
    readonly IRequestResponseSettings _settings;
    IMediator _mediator;

    public MediatorRequestResponseTransport(IRequestResponseSettings settings)
    {
        _settings = settings;
    }

    public Task<IRequestClient<T>> GetRequestClientAsync<T>(TimeSpan settingsRequestTimeout)
        where T : class
    {
        return Task.FromResult(_mediator.CreateRequestClient<T>(new RequestTimeout(settingsRequestTimeout)));
    }

    public Task StartAsync(Action<IReceiveEndpointConfigurator> configureReceiveEndpoint, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _mediator = MediatorFactory.Create(configureReceiveEndpoint);
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_mediator is not null)
            await _mediator.DisposeAsync().ConfigureAwait(false);
    }
}
