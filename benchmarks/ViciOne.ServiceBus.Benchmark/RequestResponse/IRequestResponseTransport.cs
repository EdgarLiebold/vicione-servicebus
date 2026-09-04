using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus;

namespace ViciOneServiceBusBenchmark.RequestResponse;

public interface IRequestResponseTransport :
    IDisposable
{
    Task<IRequestClient<T>> GetRequestClientAsync<T>(TimeSpan settingsRequestTimeout)
        where T : class;

    void GetBusControl(Action<IReceiveEndpointConfigurator> callback);
}
