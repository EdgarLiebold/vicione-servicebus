using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Events;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Aggregates the readiness and lifetime of the endpoints and riders started by a transport host.</summary>
internal sealed class StartHostHandle :
    IHostHandle
{
    readonly IHostReceiveEndpointHandle[] _handles;
    readonly BaseHost _host;
    readonly HostRiderHandle[] _riderHandles;

    /// <summary>Creates a handle for one started generation of a transport host.</summary>
    /// <param name="host">The host controlled by this handle.</param>
    /// <param name="handles">The receive-endpoint handles whose readiness is aggregated.</param>
    /// <param name="riderHandles">The rider handles whose readiness is aggregated.</param>
    internal StartHostHandle(BaseHost host, IHostReceiveEndpointHandle[] handles, HostRiderHandle[] riderHandles)
    {
        _host = host;
        _handles = handles;
        _riderHandles = riderHandles;
    }

    Task<HostReady> IHostHandle.Ready
    {
        get { return ReadyOrNotAsync(_handles.Select(x => x.Ready).ToArray(), _riderHandles.Select(x => x.Ready).ToArray()); }
    }

    Task IHostHandle.StopAsync(CancellationToken cancellationToken)
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
