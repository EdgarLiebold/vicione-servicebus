using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Provides shared endpoint, rider, lifecycle, health, and diagnostics orchestration for a transport host.</summary>
public abstract class BaseHost :
    IHost
{
    readonly IHostConfiguration _hostConfiguration;
    IHostHandle? _handle;

    /// <summary>Creates a transport host from its configuration and topology.</summary>
    /// <param name="hostConfiguration">The configuration shared by the host's endpoints and observers.</param>
    /// <param name="busTopology">The topology exposed by the host.</param>
    protected BaseHost(IHostConfiguration hostConfiguration, IBusTopology busTopology)
    {
        _hostConfiguration = hostConfiguration;
        Topology = busTopology;

        ReceiveEndpoints = new ReceiveEndpointCollection();
        Riders = new RiderCollection();
    }

    /// <summary>Gets the collection that owns the host's receive endpoints.</summary>
    protected IReceiveEndpointCollection ReceiveEndpoints { get; }
    RiderCollection Riders { get; }

    /// <summary>Gets the transport address represented by this host.</summary>
    public Uri Address => _hostConfiguration.HostAddress;

    /// <summary>Gets the bus topology associated with this host.</summary>
    public IBusTopology Topology { get; }

    /// <summary>Connects a receive endpoint described by an endpoint definition.</summary>
    /// <param name="definition">The definition that supplies endpoint identity and settings.</param>
    /// <param name="endpointNameFormatter">The formatter used to derive the endpoint name, or <see langword="null" /> to use the configured default.</param>
    /// <param name="configureEndpoint">An optional callback that further configures the endpoint.</param>
    /// <returns>A handle that controls the endpoint registration and exposes its readiness.</returns>
    public abstract HostReceiveEndpointHandle ConnectReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<IReceiveEndpointConfigurator>? configureEndpoint = null);

    /// <summary>Connects a receive endpoint for a transport queue.</summary>
    /// <param name="queueName">The transport queue name.</param>
    /// <param name="configureEndpoint">An optional callback that further configures the endpoint.</param>
    /// <returns>A handle that controls the endpoint registration and exposes its readiness.</returns>
    public abstract HostReceiveEndpointHandle ConnectReceiveEndpoint(string queueName, Action<IReceiveEndpointConfigurator>? configureEndpoint = null);

    ConnectHandle IConsumeMessageObserverConnector.ConnectConsumeMessageObserver<T>(IConsumeMessageObserver<T> observer)
    {
        return ReceiveEndpoints.ConnectConsumeMessageObserver(observer);
    }

    ConnectHandle IConsumeObserverConnector.ConnectConsumeObserver(IConsumeObserver observer)
    {
        return _hostConfiguration.ConnectConsumeObserver(observer);
    }

    ConnectHandle IReceiveObserverConnector.ConnectReceiveObserver(IReceiveObserver observer)
    {
        return _hostConfiguration.ConnectReceiveObserver(observer);
    }

    ConnectHandle IReceiveEndpointObserverConnector.ConnectReceiveEndpointObserver(IReceiveEndpointObserver observer)
    {
        return ReceiveEndpoints.ConnectReceiveEndpointObserver(observer);
    }

    /// <summary>Subscribes an observer to endpoint-configuration events.</summary>
    /// <param name="observer">The observer that receives configuration events.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectEndpointConfigurationObserver(IEndpointConfigurationObserver observer)
    {
        return _hostConfiguration.ConnectEndpointConfigurationObserver(observer);
    }

    ConnectHandle IPublishObserverConnector.ConnectPublishObserver(IPublishObserver observer)
    {
        return _hostConfiguration.ConnectPublishObserver(observer);
    }

    ConnectHandle ISendObserverConnector.ConnectSendObserver(ISendObserver observer)
    {
        return _hostConfiguration.ConnectSendObserver(observer);
    }

    /// <summary>Starts the host's endpoints and riders, or returns the handle for the active generation.</summary>
    /// <param name="cancellationToken">The token that cancels startup.</param>
    /// <returns>A handle that exposes aggregate readiness and controls the active host generation.</returns>
    public IHostHandle Start(CancellationToken cancellationToken)
    {
        if (_handle != null)
        {
            LogContext.Warning?.Log("Start called, but the host was already started: {Address} ({Reason})", _hostConfiguration.HostAddress,
                "Already Started");

            return _handle;
        }

        LogContext.SetCurrentIfNull(_hostConfiguration.LogContext);

        LogContext.Debug?.Log("Starting bus: {HostAddress}", _hostConfiguration.HostAddress);

        HostReceiveEndpointHandle[] handles = ReceiveEndpoints.StartEndpoints(cancellationToken);

        HostRiderHandle[] riders = Riders.StartRiders(cancellationToken);

        _handle = new StartHostHandle(this, handles, riders);

        return _handle;
    }

    /// <summary>Adds a configured receive endpoint to this host.</summary>
    /// <param name="endpointName">The name used to identify the endpoint within the host.</param>
    /// <param name="receiveEndpoint">The endpoint owned by the host.</param>
    public void AddReceiveEndpoint(string endpointName, ReceiveEndpoint receiveEndpoint)
    {
        ReceiveEndpoints.Add(endpointName, receiveEndpoint);
    }

    /// <summary>Gets a rider registered with this host.</summary>
    /// <param name="name">The rider's registration name.</param>
    /// <returns>The registered rider.</returns>
    public IRider GetRider(string name)
    {
        return Riders.Get(name);
    }

    /// <summary>Adds a rider controlled by this host.</summary>
    /// <param name="name">The rider's unique registration name.</param>
    /// <param name="riderControl">The rider lifecycle controller.</param>
    public void AddRider(string name, IRiderControl riderControl)
    {
        Riders.Add(name, riderControl);
    }

    /// <summary>Combines the bus lifecycle state with endpoint and rider health observations.</summary>
    /// <param name="busState">The current bus lifecycle state.</param>
    /// <param name="healthMessage">The diagnostic associated with the bus state.</param>
    /// <returns>The aggregate bus health result.</returns>
    public BusHealthResult CheckHealth(BusState busState, string healthMessage)
    {
        EndpointHealthResult[] results = ReceiveEndpoints.CheckEndpointHealth()
            .Concat(Riders.CheckEndpointHealth())
            .ToArray();

        EndpointHealthResult[] unhealthy = results.Where(x => x.Status == BusHealthStatus.Unhealthy).ToArray();
        EndpointHealthResult[] degraded = results.Where(x => x.Status == BusHealthStatus.Degraded).ToArray();

        EndpointHealthResult[] unhappy = unhealthy.Union(degraded).ToArray();

        var names = unhappy.Select(x => x.InputAddress.AbsolutePath.Split('/').LastOrDefault()).ToArray();

        Dictionary<string, EndpointHealthResult> data = results.ToDictionary(x => x.InputAddress.ToString(), x => x);

        var exception = results.Where(x => x.Exception != null).Select(x => x.Exception).FirstOrDefault();

        if (busState != BusState.Started || (unhealthy.Any() && unhappy.Length == results.Length))
            return BusHealthResult.Unhealthy($"Not ready: {healthMessage}", exception, data);

        if (unhappy.Any())
            return BusHealthResult.Degraded($"Degraded Endpoints: {string.Join(",", names)}", exception, data);

        return BusHealthResult.Healthy("Ready", data);
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        var scope = context.CreateScope("host");

        Probe(scope);

        ReceiveEndpoints.Probe(scope);
    }

    /// <summary>Stops the current rider and endpoint generation and releases the host's active agents.</summary>
    /// <param name="cancellationToken">The token that cancels shutdown.</param>
    /// <returns>A task that completes after the active host generation has stopped.</returns>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        LogContext.Current = _hostConfiguration.LogContext;

        LogContext.Debug?.Log("Stopping bus: {HostAddress}", Address);

        // Hosts are restartable while RiderCollection is one-shot, so stop only its current riders.
        await Riders.StopRidersAsync(cancellationToken).ConfigureAwait(false);

        await ReceiveEndpoints.StopEndpointsAsync(cancellationToken).ConfigureAwait(false);

        foreach (var agent in GetAgentHandles())
            await agent.StopAsync("Bus stopped", cancellationToken).ConfigureAwait(false);

        _handle = null;
    }

    /// <summary>Adds transport-specific diagnostic information to a probe.</summary>
    /// <param name="context">The probe context that receives the diagnostics.</param>
    protected abstract void Probe(ProbeContext context);

    /// <summary>Gets the active transport agents that must stop with the host.</summary>
    /// <returns>The active transport agents.</returns>
    protected virtual IAgent[] GetAgentHandles()
    {
        return new IAgent[0];
    }
}
