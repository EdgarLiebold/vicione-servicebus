using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Provides a rider collection implementation.
/// </summary>
public class RiderCollection :
    Agent,
    IRiderCollection
{
    readonly Dictionary<string, Handle> _handles;
    readonly object _mutateLock = new object();
    readonly Dictionary<string, IRiderControl> _riders;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public RiderCollection()
    {
        _riders = new Dictionary<string, IRiderControl>(StringComparer.OrdinalIgnoreCase);
        _handles = new Dictionary<string, Handle>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Performs the get operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns>The result of the operation.</returns>
    public IRider Get(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException($"The {nameof(name)} must not be null or empty", nameof(name));

        lock (_mutateLock)
        {
            if (!_riders.ContainsKey(name))
                throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Rider Collection", "unknown", $"A rider with the key was not found: {name}", "Correct the named configuration before starting the host"));

            if (!_handles.TryGetValue(name, out var handle))
                throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Rider Collection", "unknown", $"A rider has not yet been started: {name}", "Correct the named configuration before starting the host"));

            return handle.Rider;
        }
    }

    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="rider">The rider value.</param>
    public void Add(string name, IRiderControl rider)
    {
        if (rider == null)
            throw new ArgumentNullException(nameof(rider));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException($"The {nameof(name)} must not be null or empty", nameof(name));

        lock (_mutateLock)
        {
            if (_riders.ContainsKey(name))
                throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Rider Collection", "unknown", $"A rider with the same key was already added: {name}", "Correct the named configuration before starting the host"));

            _riders.Add(name, rider);
        }
    }

    /// <summary>
    /// Starts riders.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public HostRiderHandle[] StartRiders(CancellationToken cancellationToken = default)
    {
        KeyValuePair<string, IRiderControl>[] ridersToStart;
        lock (_mutateLock)
            ridersToStart = _riders.Where(x => !_handles.ContainsKey(x.Key)).ToArray();

        return ridersToStart.Select(x => StartRider(x.Key, x.Value, cancellationToken)).ToArray();
    }

    /// <summary>
    /// Starts rider.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public HostRiderHandle StartRider(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException($"The {nameof(name)} must not be null or empty", nameof(name));

        IRiderControl? rider;
        lock (_mutateLock)
        {
            if (!_riders.TryGetValue(name, out rider) || rider == null)
                throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Rider Collection", "unknown", $"A rider with the key was not found: {name}", "Correct the named configuration before starting the host"));

            if (_handles.ContainsKey(name))
                throw new ArgumentException($"The specified rider has already been started: {name}", nameof(name));
        }

        return StartRider(name, rider, cancellationToken);
    }

    /// <summary>
    /// Performs the check endpoint health operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<EndpointHealthResult> CheckEndpointHealth()
    {
        return _riders.Values.SelectMany(x => x.CheckEndpointHealth()).ToList();
    }

    HostRiderHandle StartRider(string name, IRiderControl rider, CancellationToken cancellationToken)
    {
        try
        {
            static async Task<RiderReady> ReadyAsync(RiderHandle r, string n)
            {
                await r.Ready.ConfigureAwait(false);

                return new ReadyEvent(n);
            }

            var riderHandle = rider.Start(cancellationToken);

            var handle = new Handle(riderHandle, rider, ReadyAsync(riderHandle, name), () => Remove(name));

            lock (_mutateLock)
                _handles.Add(name, handle);

            return handle;
        }
        catch
        {
            lock (_mutateLock)
                _riders.Remove(name);

            throw;
        }
    }

    void Remove(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException($"The {nameof(name)} must not be null or empty", nameof(name));

        lock (_mutateLock)
        {
            _riders.Remove(name);
            _handles.Remove(name);
        }
    }

    /// <summary>
    /// Stops agent.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    protected override async Task StopAgentAsync(StopContext context)
    {
        await StopRidersAsync(context.CancellationToken).ConfigureAwait(false);

        await base.StopAgentAsync(context).ConfigureAwait(false);
    }

    internal async Task StopRidersAsync(CancellationToken cancellationToken)
    {
        KeyValuePair<string, Handle>[] handles;
        lock (_mutateLock)
            handles = _handles.ToArray();

        await Task.WhenAll(handles.Select(x => x.Value.StopAsync(false, cancellationToken))).ConfigureAwait(false);

        lock (_mutateLock)
        {
            foreach (KeyValuePair<string, Handle> handle in handles)
            {
                if (_handles.TryGetValue(handle.Key, out var current) && ReferenceEquals(current, handle.Value))
                    _handles.Remove(handle.Key);
            }
        }
    }


    class Handle :
        HostRiderHandle
    {
        readonly Action _remove;
        readonly RiderHandle _riderHandle;
        bool _stopped;

        public Handle(RiderHandle riderHandle, IRider rider, Task<RiderReady> ready, Action remove)
        {
            Rider = rider;
            Ready = ready;
            _riderHandle = riderHandle;
            _remove = remove;
        }

        public IRider Rider { get; }

        public Task<RiderReady> Ready { get; }

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            return StopAsync(true, cancellationToken);
        }

        public async Task StopAsync(bool remove, CancellationToken cancellationToken = default)
        {
            if (_stopped)
                return;

            await _riderHandle.StopAsync(cancellationToken).ConfigureAwait(false);

            if (remove)
                _remove();

            _stopped = true;
        }
    }


    class ReadyEvent :
        RiderReady
    {
        public ReadyEvent(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
