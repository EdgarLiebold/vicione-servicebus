using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Owns named rider registrations and their restartable lifecycle generations.</summary>
public sealed class RiderCollection :
    Agent,
    IRiderCollection
{
    readonly Dictionary<string, Handle> _handles;
    readonly object _mutateLock = new object();
    readonly Dictionary<string, IRiderControl> _riders;

    /// <summary>Initializes an empty, case-insensitive rider registry.</summary>
    public RiderCollection()
    {
        _riders = new Dictionary<string, IRiderControl>(StringComparer.OrdinalIgnoreCase);
        _handles = new Dictionary<string, Handle>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Gets a rider from the active lifecycle generation.</summary>
    /// <param name="name">The rider registration name.</param>
    /// <returns>The active rider.</returns>
    public IRider Get(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("The rider name must not be empty or whitespace.", nameof(name));

        lock (_mutateLock)
        {
            if (!_riders.ContainsKey(name))
                throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Rider Collection", "unknown", $"A rider with the key was not found: {name}", "Correct the named configuration before starting the host"));

            if (!_handles.TryGetValue(name, out var handle))
                throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Rider Collection", "unknown", $"A rider has not yet been started: {name}", "Correct the named configuration before starting the host"));

            return handle.Rider;
        }
    }

    /// <summary>Adds a rider registration.</summary>
    /// <param name="name">The unique rider registration name.</param>
    /// <param name="rider">The rider lifecycle controller.</param>
    public void Add(string name, IRiderControl rider)
    {
        ArgumentNullException.ThrowIfNull(rider);

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("The rider name must not be empty or whitespace.", nameof(name));

        lock (_mutateLock)
        {
            if (_riders.ContainsKey(name))
                throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Rider Collection", "unknown", $"A rider with the same key was already added: {name}", "Correct the named configuration before starting the host"));

            _riders.Add(name, rider);
        }
    }

    /// <summary>Starts every registered rider that has no active generation.</summary>
    /// <param name="cancellationToken">The token that cancels rider startup.</param>
    /// <returns>The handles created for this start operation.</returns>
    public HostRiderHandle[] StartRiders(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_mutateLock)
        {
            var started = new List<HostRiderHandle>();
            foreach (KeyValuePair<string, IRiderControl> rider in _riders)
            {
                if (!_handles.ContainsKey(rider.Key))
                    started.Add(StartRiderLocked(rider.Key, rider.Value, cancellationToken));
            }

            return started.ToArray();
        }
    }

    /// <summary>Starts a named rider that has no active generation.</summary>
    /// <param name="name">The rider registration name.</param>
    /// <param name="cancellationToken">The token that cancels rider startup.</param>
    /// <returns>The handle for the new rider generation.</returns>
    public HostRiderHandle StartRider(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("The rider name must not be empty or whitespace.", nameof(name));

        cancellationToken.ThrowIfCancellationRequested();

        lock (_mutateLock)
        {
            if (!_riders.TryGetValue(name, out IRiderControl? rider) || rider == null)
                throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Rider Collection", "unknown", $"A rider with the key was not found: {name}", "Correct the named configuration before starting the host"));

            if (_handles.ContainsKey(name))
                throw new ArgumentException($"The specified rider has already been started: {name}", nameof(name));
            return StartRiderLocked(name, rider, cancellationToken);
        }
    }

    /// <summary>Gets health observations from every registered rider.</summary>
    /// <returns>A stable snapshot of the rider endpoint health results.</returns>
    public IEnumerable<EndpointHealthResult> CheckEndpointHealth()
    {
        IRiderControl[] riders;
        lock (_mutateLock)
            riders = _riders.Values.ToArray();

        return riders.SelectMany(x => x.CheckEndpointHealth()).ToArray();
    }

    HostRiderHandle StartRiderLocked(string name, IRiderControl rider, CancellationToken cancellationToken)
    {
        static async Task<RiderReady> ReadyAsync(RiderHandle riderHandle, string riderName)
        {
            await riderHandle.Ready.ConfigureAwait(false);
            return new ReadyEvent(riderName);
        }

        RiderHandle riderHandle = rider.Start(cancellationToken)
            ?? throw new InvalidOperationException($"The rider '{name}' returned no lifecycle handle.");
        _ = riderHandle.Ready
            ?? throw new InvalidOperationException($"The rider '{name}' returned a handle without a readiness task.");

        var handle = new Handle(riderHandle, rider, ReadyAsync(riderHandle, name), () => Remove(name));
        _handles.Add(name, handle);

        return handle;
    }

    void Remove(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("The rider name must not be empty or whitespace.", nameof(name));

        lock (_mutateLock)
        {
            _riders.Remove(name);
            _handles.Remove(name);
        }
    }

    /// <summary>Stops every active rider generation as part of collection shutdown.</summary>
    /// <param name="context">The collection stop context.</param>
    /// <returns>A task that completes when the active rider generations have stopped.</returns>
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


    sealed class Handle :
        HostRiderHandle
    {
        readonly object _lifecycleLock = new();
        readonly Action _remove;
        readonly RiderHandle _riderHandle;
        bool _removeAfterStop;
        bool _removed;
        bool _stopped;
        Task? _stopTask;

        public Handle(RiderHandle riderHandle, IRider rider, Task<RiderReady> ready, Action remove)
        {
            _riderHandle = riderHandle ?? throw new ArgumentNullException(nameof(riderHandle));
            Rider = rider ?? throw new ArgumentNullException(nameof(rider));
            Ready = ready ?? throw new ArgumentNullException(nameof(ready));
            _remove = remove ?? throw new ArgumentNullException(nameof(remove));
        }

        public IRider Rider { get; }

        public Task<RiderReady> Ready { get; }

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            return StopAsync(true, cancellationToken);
        }

        public Task StopAsync(bool remove, CancellationToken cancellationToken = default)
        {
            Task stopTask;
            TaskCompletionSource<bool>? completion = null;
            bool removeNow = false;
            lock (_lifecycleLock)
            {
                _removeAfterStop |= remove;

                if (_stopped)
                {
                    if (remove && !_removed)
                    {
                        _removed = true;
                        removeNow = true;
                    }

                    stopTask = Task.CompletedTask;
                }
                else if (_stopTask is not null)
                    stopTask = _stopTask;
                else if (cancellationToken.IsCancellationRequested)
                    stopTask = Task.FromCanceled(cancellationToken);
                else
                {
                    completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    stopTask = _stopTask = completion.Task;
                }
            }

            if (removeNow)
                _remove();

            if (completion is not null)
                _ = StopCoreAsync(cancellationToken, completion);

            return stopTask;
        }

        async Task StopCoreAsync(CancellationToken cancellationToken, TaskCompletionSource<bool> completion)
        {
            try
            {
                await _riderHandle.StopAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException exception)
            {
                lock (_lifecycleLock)
                {
                    if (ReferenceEquals(_stopTask, completion.Task))
                        _stopTask = null;
                }

                completion.TrySetCanceled(exception.CancellationToken);
                return;
            }
            catch (Exception exception)
            {
                lock (_lifecycleLock)
                {
                    if (ReferenceEquals(_stopTask, completion.Task))
                        _stopTask = null;
                }

                completion.TrySetException(exception);
                return;
            }

            bool remove;
            lock (_lifecycleLock)
            {
                _stopped = true;
                remove = _removeAfterStop && !_removed;
                if (remove)
                    _removed = true;
            }

            if (remove)
                _remove();

            completion.TrySetResult(true);
        }
    }


    sealed class ReadyEvent :
        RiderReady
    {
        public ReadyEvent(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
