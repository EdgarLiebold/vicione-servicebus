using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Events;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Aggregates the readiness and lifetime of the endpoints and riders started by a transport host.</summary>
internal sealed class StartHostHandle :
    IHostHandle
{
    readonly BaseHost _host;
    readonly Task<HostReady> _ready;

    /// <summary>Creates a handle for one started generation of a transport host.</summary>
    /// <param name="host">The host controlled by this handle.</param>
    /// <param name="handles">The receive-endpoint handles whose readiness is aggregated.</param>
    /// <param name="riderHandles">The rider handles whose readiness is aggregated.</param>
    internal StartHostHandle(BaseHost host, IHostReceiveEndpointHandle[] handles, HostRiderHandle[] riderHandles)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        ArgumentNullException.ThrowIfNull(handles);
        ArgumentNullException.ThrowIfNull(riderHandles);

        if (handles.Any(static handle => handle is null))
            throw new ArgumentException("The receive-endpoint handle collection cannot contain null.", nameof(handles));
        if (riderHandles.Any(static handle => handle is null))
            throw new ArgumentException("The rider handle collection cannot contain null.", nameof(riderHandles));

        Task<ReceiveEndpointReady>[] endpointReadiness = handles.Select(handle => handle.Ready
            ?? throw new ArgumentException("A receive-endpoint handle returned no readiness task.", nameof(handles))).ToArray();
        Task<RiderReady>[] riderReadiness = riderHandles.Select(handle => handle.Ready
            ?? throw new ArgumentException("A rider handle returned no readiness task.", nameof(riderHandles))).ToArray();
        _ready = ReadyOrNotAsync(endpointReadiness, riderReadiness);
    }

    Task<HostReady> IHostHandle.Ready => _ready;

    Task IHostHandle.StopAsync(CancellationToken cancellationToken)
    {
        return _host.StopAsync(cancellationToken);
    }

    async Task<HostReady> ReadyOrNotAsync(Task<ReceiveEndpointReady>[] endpoints, Task<RiderReady>[] riders)
    {
        Task<ReceiveEndpointReady[]> endpointsReady = Task.WhenAll(endpoints);
        Task<RiderReady[]> ridersReady = Task.WhenAll(riders);
        await Task.WhenAll(endpointsReady, ridersReady).ConfigureAwait(false);

        return new HostReadyEvent(_host.Address, endpointsReady.Result, ridersReady.Result);
    }
}
