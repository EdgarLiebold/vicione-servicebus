using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.EventHubs.Configuration;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Owns Event Hubs endpoints, shared connection state, and producer-provider lifetime for a bus instance.</summary>
public class EventHubRider :
    IEventHubRider
{
    readonly IBusInstance _busInstance;
    readonly IRiderRegistrationContext _context;
    readonly IReceiveEndpointCollection _endpoints;
    readonly IEventHubHostConfiguration _hostConfiguration;
    Lazy<IEventHubProducerProvider> _producerProvider = null!;

    /// <summary>Creates a rider from its built host configuration and receive-endpoint collection.</summary>
    /// <param name="hostConfiguration">The Event Hubs host configuration.</param>
    /// <param name="busInstance">The bus instance that owns the rider.</param>
    /// <param name="endpoints">The rider's configured receive endpoints.</param>
    /// <param name="context">The rider registration context used for dynamically connected endpoints.</param>
    public EventHubRider(IEventHubHostConfiguration hostConfiguration, IBusInstance busInstance, IReceiveEndpointCollection endpoints,
        IRiderRegistrationContext context)
    {
        _hostConfiguration = hostConfiguration;
        _busInstance = busInstance;
        _endpoints = endpoints;
        _context = context;

        InitializeProducerProvider();
    }

    /// <summary>Gets the shared producer provider, optionally wrapped to propagate consume-context headers.</summary>
    /// <param name="consumeContext">The consume context whose headers should flow to produced messages, or <see langword="null" />.</param>
    /// <returns>The shared provider or a consume-context-aware wrapper.</returns>
    public IEventHubProducerProvider GetProducerProvider(ConsumeContext? consumeContext = default)
    {
        return consumeContext == null
            ? _producerProvider.Value
            : new ConsumeContextEventHubProducerProvider(_producerProvider.Value, consumeContext);
    }

    /// <summary>Builds, adds, and starts a receive endpoint on the running rider.</summary>
    /// <param name="eventHubName">The Event Hub entity name.</param>
    /// <param name="consumerGroup">The consumer group used to coordinate partition ownership.</param>
    /// <param name="configure">Configures the connected receive endpoint.</param>
    /// <returns>A handle for observing readiness and stopping the endpoint.</returns>
    public IHostReceiveEndpointHandle ConnectEventHubEndpoint(string eventHubName, string consumerGroup,
        Action<IRiderRegistrationContext, IEventHubReceiveEndpointConfigurator> configure)
    {
        var specification = _hostConfiguration.CreateSpecification(eventHubName, consumerGroup, configurator =>
        {
            configure?.Invoke(_context, configurator);
        });

        _endpoints.Add(specification.EndpointName, specification.CreateReceiveEndpoint(_busInstance));

        return _endpoints.Start(specification.EndpointName);
    }

    /// <summary>Starts configured receive endpoints and binds rider completion to the shared connection supervisor.</summary>
    /// <param name="cancellationToken">Cancels endpoint startup.</param>
    /// <returns>A handle that reports readiness and stops the rider.</returns>
    public RiderHandle Start(CancellationToken cancellationToken = default)
    {
        IHostReceiveEndpointHandle[] endpointsHandle = _endpoints.StartEndpoints(cancellationToken);

        var ready = endpointsHandle.Length == 0 ? Task.CompletedTask : _hostConfiguration.ConnectionContextSupervisor.Ready;

        var agent = new RiderAgent(_hostConfiguration.ConnectionContextSupervisor, _endpoints, ready, ResetProducerProviderAsync);

        return new Handle(endpointsHandle, agent);
    }

    /// <summary>Returns the current health result for every configured or connected receive endpoint.</summary>
    /// <returns>The endpoint health results.</returns>
    public IEnumerable<EndpointHealthResult> CheckEndpointHealth()
    {
        return _endpoints.CheckEndpointHealth();
    }

    void InitializeProducerProvider()
    {
        _producerProvider = new Lazy<IEventHubProducerProvider>(() => new EventHubProducerProvider(_hostConfiguration, _busInstance));
    }

    async ValueTask ResetProducerProviderAsync()
    {
        if (_producerProvider.IsValueCreated && _producerProvider.Value is IAsyncDisposable disposable)
            await disposable.DisposeAsync().ConfigureAwait(false);

        InitializeProducerProvider();
    }


    class RiderAgent :
        Agent
    {
        readonly IReceiveEndpointCollection _endpoints;
        readonly Func<ValueTask> _onStop;
        readonly IConnectionContextSupervisor _supervisor;

        public RiderAgent(IConnectionContextSupervisor supervisor, IReceiveEndpointCollection endpoints, Task ready, Func<ValueTask> onStop)
        {
            _supervisor = supervisor;
            _endpoints = endpoints;
            _onStop = onStop;

            SetReady(ready);
            SetCompleted(_supervisor.Completed);
        }

        protected override async Task StopAgentAsync(StopContext context)
        {
            await _endpoints.StopEndpointsAsync(context.CancellationToken).ConfigureAwait(false);
            await _supervisor.StopAsync(context).ConfigureAwait(false);
            await base.StopAgentAsync(context).ConfigureAwait(false);

            await _onStop().ConfigureAwait(false);
        }
    }


    class Handle :
        RiderHandle
    {
        readonly IAgent _agent;
        readonly IHostReceiveEndpointHandle[] _endpoints;

        public Handle(IHostReceiveEndpointHandle[] endpoints, IAgent agent)
        {
            _endpoints = endpoints;
            _agent = agent;
        }

        public Task Ready => ReadyOrNotAsync(_endpoints.Select(x => x.Ready));

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return _agent.StopAsync("Event Hub stopped", cancellationToken);
        }

        async Task ReadyOrNotAsync(IEnumerable<Task<ReceiveEndpointReady>> endpoints)
        {
            Task<ReceiveEndpointReady>[] readyTasks = endpoints as Task<ReceiveEndpointReady>[] ?? endpoints.ToArray();
            foreach (Task<ReceiveEndpointReady> ready in readyTasks)
                await ready.ConfigureAwait(false);

            await _agent.Ready.ConfigureAwait(false);

            await Task.WhenAll(readyTasks).ConfigureAwait(false);
        }
    }
}
