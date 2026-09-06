using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Stores a collection of receive endpoint values.</summary>
public class ReceiveEndpointCollection :
    IReceiveEndpointCollection
{
    readonly SingleThreadedDictionary<string, ReceiveEndpoint> _endpoints;
    readonly ReceiveEndpointObservable _receiveEndpointObservers;
    bool _started;

    /// <summary>Initializes a new instance.</summary>
    public ReceiveEndpointCollection()
    {
        _receiveEndpointObservers = new ReceiveEndpointObservable();

        _endpoints = new SingleThreadedDictionary<string, ReceiveEndpoint>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <param name="endpointName">The endpoint name.</param>
    /// <param name="endpoint">The endpoint.</param>
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

    /// <summary>Starts endpoints.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The host receive endpoint handle array produced by the operation.</returns>
    public HostReceiveEndpointHandle[] StartEndpoints(CancellationToken cancellationToken)
    {
        _started = true;

        KeyValuePair<string, ReceiveEndpoint>[] endpointsToStart = _endpoints.Where(x => !x.Value.IsStarted()).ToArray();

        return endpointsToStart.Select(x => StartEndpoint(x.Key, x.Value, cancellationToken)).ToArray();
    }

    /// <summary>Starts the configured component.</summary>
    /// <param name="endpointName">The endpoint name.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The host receive endpoint handle produced by the operation.</returns>
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

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
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

    /// <summary>Connects receive endpoint observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectReceiveEndpointObserver(IReceiveEndpointObserver observer)
    {
        return _receiveEndpointObservers.Connect(observer);
    }

    /// <summary>Connects consume message observer.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumeMessageObserver<T>(IConsumeMessageObserver<T> observer)
        where T : class
    {
        return new MultipleConnectHandle(_endpoints.Values.Select(x => x.ConnectConsumeMessageObserver(observer)));
    }

    /// <summary>Checks endpoint health.</summary>
    /// <returns>The enumerable produced by the operation.</returns>
    public IEnumerable<EndpointHealthResult> CheckEndpointHealth()
    {
        return _endpoints.Values.Select(x => x.HealthResult).ToList();
    }

    /// <summary>Stops endpoints.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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
                endpoint.ObserverHandle.Disconnect();
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


    class Handle :
        HostReceiveEndpointHandle
    {
        readonly ReceiveEndpoint _endpoint;
        readonly Action _remove;

        ReceiveEndpointHandle _endpointHandle = null!;

        public Handle(ReceiveEndpoint endpoint, Action remove)
        {
            _endpoint = endpoint;
            _remove = remove;
        }

        public IReceiveEndpoint ReceiveEndpoint => _endpoint;

        public Task<ReceiveEndpointReady> Ready => _endpointHandle.Ready;

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
