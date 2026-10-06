using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Provides shared endpoint, rider, lifecycle, health, and diagnostics orchestration for a transport host.</summary>
public abstract class BaseHost :
    IHost
{
    readonly IHostConfiguration _hostConfiguration;
    readonly object _lifecycleLock = new();
    IHostHandle? _handle;
    Task _stopTask = Task.CompletedTask;

    /// <summary>Creates a transport host from its configuration and topology.</summary>
    /// <param name="hostConfiguration">The configuration shared by the host's endpoints and observers.</param>
    /// <param name="busTopology">The topology exposed by the host.</param>
    protected BaseHost(IHostConfiguration hostConfiguration, IBusTopology busTopology)
    {
        _hostConfiguration = hostConfiguration ?? throw new ArgumentNullException(nameof(hostConfiguration));
        Topology = busTopology ?? throw new ArgumentNullException(nameof(busTopology));

        ReceiveEndpoints = new ReceiveEndpointCollection();
        Riders = new RiderCollection();
    }

    /// <summary>Gets the collection that owns the host's receive endpoints.</summary>
    protected IReceiveEndpointCollection ReceiveEndpoints { get; }
    RiderCollection Riders { get; }

    /// <summary>Gets the transport address represented by this host.</summary>
    public Uri Address => _hostConfiguration.HostAddress
        ?? throw new InvalidOperationException("The host configuration returned no host address.");

    /// <summary>Gets the bus topology associated with this host.</summary>
    public IBusTopology Topology { get; }

    /// <summary>Connects a receive endpoint described by an endpoint definition.</summary>
    /// <param name="definition">The definition that supplies endpoint identity and settings.</param>
    /// <param name="endpointNameFormatter">The formatter used to derive the endpoint name, or <see langword="null" /> to use the configured default.</param>
    /// <param name="configureEndpoint">An optional callback that further configures the endpoint.</param>
    /// <returns>A handle that controls the endpoint registration and exposes its readiness.</returns>
    public abstract IHostReceiveEndpointHandle ConnectReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<IReceiveEndpointConfigurator>? configureEndpoint = null);

    /// <summary>Connects a receive endpoint for a transport queue.</summary>
    /// <param name="queueName">The transport queue name.</param>
    /// <param name="configureEndpoint">An optional callback that further configures the endpoint.</param>
    /// <returns>A handle that controls the endpoint registration and exposes its readiness.</returns>
    public abstract IHostReceiveEndpointHandle ConnectReceiveEndpoint(string queueName, Action<IReceiveEndpointConfigurator>? configureEndpoint = null);

    ConnectHandle IConsumeMessageObserverConnector.ConnectConsumeMessageObserver<T>(IConsumeMessageObserver<T> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return ReceiveEndpoints.ConnectConsumeMessageObserver(observer);
    }

    ConnectHandle IConsumeObserverConnector.ConnectConsumeObserver(IConsumeObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _hostConfiguration.ConnectConsumeObserver(observer);
    }

    ConnectHandle IReceiveObserverConnector.ConnectReceiveObserver(IReceiveObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _hostConfiguration.ConnectReceiveObserver(observer);
    }

    ConnectHandle IReceiveEndpointObserverConnector.ConnectReceiveEndpointObserver(IReceiveEndpointObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return ReceiveEndpoints.ConnectReceiveEndpointObserver(observer);
    }

    /// <summary>Subscribes an observer to endpoint-configuration events.</summary>
    /// <param name="observer">The observer that receives configuration events.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectEndpointConfigurationObserver(IEndpointConfigurationObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _hostConfiguration.ConnectEndpointConfigurationObserver(observer);
    }

    ConnectHandle IPublishObserverConnector.ConnectPublishObserver(IPublishObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _hostConfiguration.ConnectPublishObserver(observer);
    }

    ConnectHandle ISendObserverConnector.ConnectSendObserver(ISendObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _hostConfiguration.ConnectSendObserver(observer);
    }

    /// <summary>Starts the host's endpoints and riders, or returns the handle for the active generation.</summary>
    /// <param name="cancellationToken">The token that cancels startup.</param>
    /// <returns>A handle that exposes aggregate readiness and controls the active host generation.</returns>
    public IHostHandle Start(CancellationToken cancellationToken)
    {
        lock (_lifecycleLock)
        {
            if (_handle != null)
            {
                try
                {
                    LogContext.Warning?.Log("Start called, but the host was already started: {Address} ({Reason})", _hostConfiguration.HostAddress,
                        "Already Started");
                }
                catch
                {
                    // Optional diagnostics cannot prevent returning the active host generation.
                }

                return _handle;
            }

            if (!_stopTask.IsCompleted)
                throw new InvalidOperationException($"The transport host is still stopping: {Address}.");

            cancellationToken.ThrowIfCancellationRequested();

            LogContext.SetCurrentIfNull(_hostConfiguration.LogContext);

            try
            {
                LogContext.Debug?.Log("Starting bus: {HostAddress}", _hostConfiguration.HostAddress);
            }
            catch
            {
                // Optional diagnostics cannot prevent admission of the host generation.
            }

            try
            {
                IHostReceiveEndpointHandle[] handles = ReceiveEndpoints.StartEndpoints(cancellationToken);
                HostRiderHandle[] riders = Riders.StartRiders(cancellationToken);

                _handle = new StartHostHandle(this, handles, riders);
                return _handle;
            }
            catch (Exception startException)
            {
                try
                {
                    RollbackStartAsync().GetAwaiter().GetResult();
                }
                catch (Exception cleanupException)
                {
                    throw new AggregateException("Transport-host startup and rollback both failed.", startException, cleanupException);
                }

                throw;
            }
        }
    }

    /// <summary>Adds a configured receive endpoint to this host.</summary>
    /// <param name="endpointName">The name used to identify the endpoint within the host.</param>
    /// <param name="receiveEndpoint">The endpoint owned by the host.</param>
    public void AddReceiveEndpoint(string endpointName, ReceiveEndpoint receiveEndpoint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpointName);
        ArgumentNullException.ThrowIfNull(receiveEndpoint);
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
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(riderControl);
        Riders.Add(name, riderControl);
    }

    /// <summary>Combines the bus lifecycle state with endpoint and rider health observations.</summary>
    /// <param name="busState">The current bus lifecycle state.</param>
    /// <param name="healthMessage">The diagnostic associated with the bus state.</param>
    /// <returns>The aggregate bus health result.</returns>
    public BusHealthResult CheckHealth(BusState busState, string healthMessage)
    {
        ArgumentNullException.ThrowIfNull(healthMessage);
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
        ArgumentNullException.ThrowIfNull(context);
        var scope = context.CreateScope("host");

        Probe(scope);

        ReceiveEndpoints.Probe(scope);
    }

    /// <summary>Stops the current rider and endpoint generation and releases the host's active agents.</summary>
    /// <param name="cancellationToken">The token that cancels shutdown.</param>
    /// <returns>A task that completes after the active host generation has stopped.</returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        lock (_lifecycleLock)
        {
            if (!_stopTask.IsCompleted)
                return _stopTask;

            if (_handle is null)
                return Task.CompletedTask;

            _stopTask = StopCoreAsync(_handle, cancellationToken);
            return _stopTask;
        }
    }

    async Task StopCoreAsync(IHostHandle handle, CancellationToken cancellationToken)
    {
        var stopped = false;
        LogContext.Current = _hostConfiguration.LogContext;

        try
        {
            await StopResourcesAsync(cancellationToken).ConfigureAwait(false);
            stopped = true;
        }
        finally
        {
            if (stopped)
            {
                lock (_lifecycleLock)
                {
                    if (ReferenceEquals(_handle, handle))
                        _handle = null;
                }
            }
        }
    }

    async Task StopResourcesAsync(CancellationToken cancellationToken)
    {
        try
        {
            LogContext.Debug?.Log("Stopping bus: {HostAddress}", Address);
        }
        catch (Exception)
        {
        }

        var failures = new List<Exception>();
        try
        {
            await StopStartedEndpointsAndRidersAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            failures.Add(exception);
        }

        IAgent[] agents;
        try
        {
            agents = GetAgentHandles()
                ?? throw new InvalidOperationException("The transport host returned no agent collection.");
        }
        catch (Exception exception)
        {
            failures.Add(exception);
            ThrowStopFailures(failures, cancellationToken);
            return;
        }

        foreach (IAgent agent in agents)
        {
            try
            {
                if (agent is null)
                    throw new InvalidOperationException("The transport host returned an agent collection containing null.");

                await agent.StopAsync("Bus stopped", cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
        }

        ThrowStopFailures(failures, cancellationToken);
    }

    Task RollbackStartAsync()
    {
        return StopStartedEndpointsAndRidersAsync(CancellationToken.None);
    }

    async Task StopStartedEndpointsAndRidersAsync(CancellationToken cancellationToken)
    {
        var failures = new List<Exception>();
        try
        {
            await Riders.StopRidersAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            failures.Add(exception);
        }

        try
        {
            await ReceiveEndpoints.StopEndpointsAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            failures.Add(exception);
        }

        ThrowStopFailures(failures, cancellationToken);
    }

    static void ThrowStopFailures(List<Exception> failures, CancellationToken cancellationToken)
    {
        if (failures.Count == 1)
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1)
        {
            var aggregate = new AggregateException("Host shutdown failed.", failures);
            if (cancellationToken.IsCancellationRequested
                && aggregate.InnerExceptions.All(IsCancellationFailure))
                throw new OperationCanceledException("Host shutdown was canceled.", aggregate, cancellationToken);

            throw aggregate;
        }
    }

    static bool IsCancellationFailure(Exception exception)
    {
        return exception is OperationCanceledException
            || exception is AggregateException aggregate && aggregate.InnerExceptions.Count > 0
            && aggregate.InnerExceptions.All(IsCancellationFailure);
    }

    /// <summary>Adds transport-specific diagnostic information to a probe.</summary>
    /// <param name="context">The probe context that receives the diagnostics.</param>
    protected abstract void Probe(ProbeContext context);

    /// <summary>Gets the active transport agents that must stop with the host.</summary>
    /// <returns>The active transport agents.</returns>
    protected virtual IAgent[] GetAgentHandles()
    {
        return [];
    }
}
