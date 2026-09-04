using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.EventHubs.Configuration;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Provides an event hub rider implementation.
/// </summary>
public class EventHubRider :
    IEventHubRider
{
    readonly IBusInstance _busInstance;
    readonly IRiderRegistrationContext _context;
    readonly IReceiveEndpointCollection _endpoints;
    readonly IEventHubHostConfiguration _hostConfiguration;
    Lazy<IEventHubProducerProvider> _producerProvider = null!;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="busInstance">The bus instance value.</param>
    /// <param name="endpoints">The endpoints value.</param>
    /// <param name="context">The operation context.</param>
    public EventHubRider(IEventHubHostConfiguration hostConfiguration, IBusInstance busInstance, IReceiveEndpointCollection endpoints,
        IRiderRegistrationContext context)
    {
        _hostConfiguration = hostConfiguration;
        _busInstance = busInstance;
        _endpoints = endpoints;
        _context = context;

        InitializeProducerProvider();
    }

    /// <summary>
    /// Gets producer provider.
    /// </summary>
    /// <param name="consumeContext">The consume context value.</param>
    /// <returns>The result of the operation.</returns>
    public IEventHubProducerProvider GetProducerProvider(ConsumeContext? consumeContext = default)
    {
        return consumeContext == null
            ? _producerProvider.Value
            : new ConsumeContextEventHubProducerProvider(_producerProvider.Value, consumeContext);
    }

    /// <summary>
    /// Connects event hub endpoint.
    /// </summary>
    /// <param name="eventHubName">The event hub name value.</param>
    /// <param name="consumerGroup">The consumer group value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public HostReceiveEndpointHandle ConnectEventHubEndpoint(string eventHubName, string consumerGroup,
        Action<IRiderRegistrationContext, IEventHubReceiveEndpointConfigurator> configure)
    {
        var specification = _hostConfiguration.CreateSpecification(eventHubName, consumerGroup, configurator =>
        {
            configure?.Invoke(_context, configurator);
        });

        _endpoints.Add(specification.EndpointName, specification.CreateReceiveEndpoint(_busInstance));

        return _endpoints.Start(specification.EndpointName);
    }

    /// <summary>
    /// Starts the configured component.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public RiderHandle Start(CancellationToken cancellationToken = default)
    {
        HostReceiveEndpointHandle[] endpointsHandle = _endpoints.StartEndpoints(cancellationToken);

        var ready = endpointsHandle.Length == 0 ? Task.CompletedTask : _hostConfiguration.ConnectionContextSupervisor.Ready;

        var agent = new RiderAgent(_hostConfiguration.ConnectionContextSupervisor, _endpoints, ready, ResetProducerProviderAsync);

        return new Handle(endpointsHandle, agent);
    }

    /// <summary>
    /// Performs the check endpoint health operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
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
        readonly HostReceiveEndpointHandle[] _endpoints;

        public Handle(HostReceiveEndpointHandle[] endpoints, IAgent agent)
        {
            _endpoints = endpoints;
            _agent = agent;
        }

        public Task Ready => ReadyOrNotAsync(_endpoints.Select(x => x.Ready));

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return _agent.StopAsync("EvenHub stopped", cancellationToken);
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
