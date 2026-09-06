using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Represents the host for base.</summary>
public abstract class BaseHost :
    IHost
{
    readonly IHostConfiguration _hostConfiguration;
    HostHandle? _handle;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="hostConfiguration">The host configuration.</param>
    /// <param name="busTopology">The bus topology.</param>
    protected BaseHost(IHostConfiguration hostConfiguration, IBusTopology busTopology)
    {
        _hostConfiguration = hostConfiguration;
        Topology = busTopology;

        ReceiveEndpoints = new ReceiveEndpointCollection();
        Riders = new RiderCollection();
    }

    /// <summary>Gets the receive endpoints.</summary>
    protected IReceiveEndpointCollection ReceiveEndpoints { get; }
    RiderCollection Riders { get; }

    /// <summary>Gets the address.</summary>
    public Uri Address => _hostConfiguration.HostAddress;

    /// <summary>Gets the topology.</summary>
    public IBusTopology Topology { get; }

    /// <summary>Connects receive endpoint.</summary>
    /// <param name="definition">The definition.</param>
    /// <param name="endpointNameFormatter">The endpoint name formatter.</param>
    /// <param name="configureEndpoint">The configure endpoint.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public abstract HostReceiveEndpointHandle ConnectReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<IReceiveEndpointConfigurator>? configureEndpoint = null);

    /// <summary>Connects receive endpoint.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="configureEndpoint">The configure endpoint.</param>
    /// <returns>A handle that disconnects the registration.</returns>
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

    /// <summary>Connects endpoint configuration observer.</summary>
    /// <param name="observer">The observer to connect.</param>
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

    /// <summary>Starts the configured component.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The host handle produced by the operation.</returns>
    public HostHandle Start(CancellationToken cancellationToken)
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

    /// <summary>Adds receive endpoint to the configuration.</summary>
    /// <param name="endpointName">The endpoint name.</param>
    /// <param name="receiveEndpoint">The receive endpoint.</param>
    public void AddReceiveEndpoint(string endpointName, ReceiveEndpoint receiveEndpoint)
    {
        ReceiveEndpoints.Add(endpointName, receiveEndpoint);
    }

    /// <summary>Gets rider.</summary>
    /// <param name="name">The name.</param>
    /// <returns>The rider.</returns>
    public IRider GetRider(string name)
    {
        return Riders.Get(name);
    }

    /// <summary>Adds rider to the configuration.</summary>
    /// <param name="name">The name.</param>
    /// <param name="riderControl">The rider control.</param>
    public void AddRider(string name, IRiderControl riderControl)
    {
        Riders.Add(name, riderControl);
    }

    /// <summary>Checks health.</summary>
    /// <param name="busState">The bus state.</param>
    /// <param name="healthMessage">The health message.</param>
    /// <returns>The bus health result produced by the operation.</returns>
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

    /// <summary>Stops the configured component.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    protected abstract void Probe(ProbeContext context);

    /// <summary>Gets agent handles.</summary>
    /// <returns>The agent handles.</returns>
    protected virtual IAgent[] GetAgentHandles()
    {
        return new IAgent[0];
    }
}
