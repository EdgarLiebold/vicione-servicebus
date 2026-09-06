using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Owns the host's named receive endpoints and coordinates their lifecycle and observation.</summary>
public sealed class ReceiveEndpointCollection :
    IReceiveEndpointCollection
{
    readonly SingleThreadedDictionary<string, ReceiveEndpoint> _endpoints;
    readonly ReceiveEndpointObservable _receiveEndpointObservers;
    bool _started;

    /// <summary>Initializes an empty receive-endpoint collection.</summary>
    public ReceiveEndpointCollection()
    {
        _receiveEndpointObservers = new ReceiveEndpointObservable();

        _endpoints = new SingleThreadedDictionary<string, ReceiveEndpoint>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Adds a named endpoint and connects it to collection-wide lifecycle and health observation.</summary>
    /// <param name="endpointName">The name that uniquely identifies the endpoint, ignoring case.</param>
    /// <param name="endpoint">The receive endpoint to add.</param>
    public void Add(string endpointName, ReceiveEndpoint endpoint)
    {
        if (endpoint == null)
            throw new ArgumentNullException(nameof(endpoint));
        if (string.IsNullOrWhiteSpace(endpointName))
            throw new ArgumentException($"The {nameof(endpointName)} must not be null or empty", nameof(endpointName));

        endpoint.HealthResult = _started
            ? EndpointHealthResult.Healthy(endpoint, "starting")
            : EndpointHealthResult.Unhealthy(endpoint, "not ready", null);

        var added = _endpoints.TryAdd(endpointName, _ =>
        {
            endpoint.ConnectReceiveEndpointObserver(new HealthResultReceiveEndpointObserver(endpoint));
            endpoint.ObserverHandle = endpoint.ConnectReceiveEndpointObserver(_receiveEndpointObservers);

            return endpoint;
        });

        if (!added)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Receive endpoint", "unknown", $"A receive endpoint with the same key was already added: {endpointName}", "Correct the named configuration before starting the host"));
    }

    /// <summary>Starts every endpoint that is not already running.</summary>
    /// <param name="cancellationToken">The token that cancels endpoint startup.</param>
    /// <returns>Handles for the endpoints started by this call.</returns>
    public HostReceiveEndpointHandle[] StartEndpoints(CancellationToken cancellationToken)
    {
        _started = true;

        KeyValuePair<string, ReceiveEndpoint>[] endpointsToStart = _endpoints.Where(x => !x.Value.IsStarted()).ToArray();

        return endpointsToStart.Select(x => StartEndpoint(x.Key, x.Value, cancellationToken)).ToArray();
    }

    /// <summary>Starts a named receive endpoint.</summary>
    /// <param name="endpointName">The registered endpoint name.</param>
    /// <param name="cancellationToken">The token that cancels endpoint startup.</param>
    /// <returns>A handle that exposes readiness and controls the endpoint.</returns>
    public HostReceiveEndpointHandle Start(string endpointName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(endpointName))
            throw new ArgumentException($"The {nameof(endpointName)} must not be null or empty", nameof(endpointName));

        if (!_endpoints.TryGetValue(endpointName, out var endpoint))
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Receive endpoint", "unknown", $"A receive endpoint with the key was not found: {endpointName}", "Correct the named configuration before starting the host"));

        if (endpoint.IsStarted())
            throw new ArgumentException($"The specified endpoint has already been started: {endpointName}", nameof(endpointName));

        return StartEndpoint(endpointName, endpoint, cancellationToken);
    }

    /// <summary>Adds diagnostics for every registered endpoint to a probe.</summary>
    /// <param name="context">The probe context that receives endpoint diagnostics.</param>
    public void Probe(ProbeContext context)
    {
        foreach (KeyValuePair<string, ReceiveEndpoint> endpoint in _endpoints)
        {
            var endpointScope = context.CreateScope("receiveEndpoint");
            endpointScope.Add("name", endpoint.Key);
            if (endpoint.Value.IsStarted())
                endpointScope.Add("started", true);

            endpoint.Value.Probe(endpointScope);
        }
    }

    /// <summary>Subscribes an observer to lifecycle notifications from every registered endpoint.</summary>
    /// <param name="observer">The observer that receives endpoint lifecycle notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectReceiveEndpointObserver(IReceiveEndpointObserver observer)
    {
        return _receiveEndpointObservers.Connect(observer);
    }

    /// <summary>Subscribes a typed observer to consume notifications from every registered endpoint.</summary>
    /// <typeparam name="T">The observed message contract.</typeparam>
    /// <param name="observer">The observer that receives typed consume notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumeMessageObserver<T>(IConsumeMessageObserver<T> observer)
        where T : class
    {
        return new MultipleConnectHandle(_endpoints.Values.Select(x => x.ConnectConsumeMessageObserver(observer)));
    }

    /// <summary>Creates a snapshot of the latest health observation for every endpoint.</summary>
    /// <returns>The endpoint health observations in collection order.</returns>
    public IEnumerable<EndpointHealthResult> CheckEndpointHealth()
    {
        return _endpoints.Values.Select(x => x.HealthResult).ToList();
    }

    /// <summary>Stops application endpoints before stopping the bus endpoint.</summary>
    /// <param name="cancellationToken">The token that cancels endpoint shutdown.</param>
    /// <returns>A task that completes when all running or paused endpoints have stopped.</returns>
    public async Task StopEndpointsAsync(CancellationToken cancellationToken)
    {
        ReceiveEndpoint[] endpoints = _endpoints.Values.Where(x => (x.IsStarted() || x.IsPaused) && !x.IsBusEndpoint).ToArray();

        await Task.WhenAll(endpoints.Select(x => x.StopAsync(cancellationToken))).ConfigureAwait(false);

        endpoints = _endpoints.Values.Where(x => (x.IsStarted() || x.IsPaused) && x.IsBusEndpoint).ToArray();

        await Task.WhenAll(endpoints.Select(x => x.StopAsync(cancellationToken))).ConfigureAwait(false);

        _started = false;
    }

    HostReceiveEndpointHandle StartEndpoint(string endpointName, ReceiveEndpoint endpoint, CancellationToken cancellationToken)
    {
        try
        {
            void RemoveEndpoint()
            {
                (endpoint.ObserverHandle
                    ?? throw new InvalidOperationException("The receive endpoint observer is not connected."))
                    .Disconnect();
                Remove(endpointName);
            }

            var handle = new Handle(endpoint, RemoveEndpoint);

            handle.Start(cancellationToken);

            return handle;
        }
        catch
        {
            _endpoints.TryRemove(endpointName, out _);

            throw;
        }
    }

    void Remove(string endpointName)
    {
        if (string.IsNullOrWhiteSpace(endpointName))
            throw new ArgumentException($"The {nameof(endpointName)} must not be null or empty", nameof(endpointName));

        _endpoints.TryRemove(endpointName, out _);
    }


    sealed class Handle :
        HostReceiveEndpointHandle
    {
        readonly ReceiveEndpoint _endpoint;
        readonly Action _remove;

        ReceiveEndpointHandle? _endpointHandle;

        public Handle(ReceiveEndpoint endpoint, Action remove)
        {
            _endpoint = endpoint;
            _remove = remove;
        }

        public IReceiveEndpoint ReceiveEndpoint => _endpoint;

        public Task<ReceiveEndpointReady> Ready => (_endpointHandle
            ?? throw new InvalidOperationException("The receive endpoint has not been started."))
            .Ready;

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _remove();

            return _endpoint.StopAsync(true, cancellationToken);
        }

        public void Start(CancellationToken cancellationToken)
        {
            if (_endpointHandle != null)
                return;

            _endpointHandle = _endpoint.Start(cancellationToken);
        }
    }
}
