using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Events;

namespace ViciOne.ServiceBus.Transports;

public class StartHostHandle :
    HostHandle
{
    readonly HostReceiveEndpointHandle[] _handles;
    readonly BaseHost _host;
    readonly HostRiderHandle[] _riderHandles;

    public StartHostHandle(BaseHost host, HostReceiveEndpointHandle[] handles, HostRiderHandle[] riderHandles)
    {
        _host = host;
        _handles = handles;
        _riderHandles = riderHandles;
    }

    Task<HostReady> HostHandle.Ready
    {
        get { return ReadyOrNotAsync(_handles.Select(x => x.Ready).ToArray(), _riderHandles.Select(x => x.Ready).ToArray()); }
    }

    Task HostHandle.StopAsync(CancellationToken cancellationToken)
    {
        return _host.StopAsync(cancellationToken);
    }

    async Task<HostReady> ReadyOrNotAsync(Task<ReceiveEndpointReady>[] endpoints, Task<RiderReady>[] riders)
    {
        ReceiveEndpointReady[] endpointsReady = await EndpointsReadyAsync(endpoints).ConfigureAwait(false);

        RiderReady[] ridersReady = await RidersReadyAsync(riders).ConfigureAwait(false);

        return new HostReadyEvent(_host.Address, endpointsReady, ridersReady);
    }

    static async Task<ReceiveEndpointReady[]> EndpointsReadyAsync(Task<ReceiveEndpointReady>[] endpoints)
    {
        foreach (Task<ReceiveEndpointReady> ready in endpoints)
            await ready.ConfigureAwait(false);

        return await Task.WhenAll(endpoints).ConfigureAwait(false);
    }

    static async Task<RiderReady[]> RidersReadyAsync(Task<RiderReady>[] riders)
    {
        foreach (Task<RiderReady> ready in riders)
            await ready.ConfigureAwait(false);

        return await Task.WhenAll(riders).ConfigureAwait(false);
    }
}
