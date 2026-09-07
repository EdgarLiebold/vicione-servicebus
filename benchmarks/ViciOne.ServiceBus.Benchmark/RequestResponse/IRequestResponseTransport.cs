using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus;

namespace ViciOneServiceBusBenchmark.RequestResponse;

public interface IRequestResponseTransport :
    IAsyncDisposable
{
    Task<IRequestClient<T>> GetRequestClientAsync<T>(TimeSpan settingsRequestTimeout)
        where T : class;

    Task StartAsync(Action<IReceiveEndpointConfigurator> configureReceiveEndpoint, CancellationToken cancellationToken = default);
}
