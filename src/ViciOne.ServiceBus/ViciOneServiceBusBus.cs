using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Initializers;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Events;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus;

internal sealed class ViciOneServiceBusBus :
    IBusControl,
    Advanced.IAdvancedPublishEndpoint,
    IMessageRouteProvider
{
    /// <summary>
    /// How long a consumer connection waits for the on-demand bus endpoint. Same value StartAsync
    /// falls back to when the caller supplies no token, so both express one notion of "too long".
    /// </summary>
    static readonly TimeSpan ReadyTimeout = TimeSpan.FromSeconds(60);

    readonly IBusObserver _busObservable;
    readonly IConsumePipe _consumePipe;
    readonly IHost _host;
    readonly ILogContext _logContext;
    readonly IPublishEndpoint _publishEndpoint;
    readonly IReceiveEndpoint _receiveEndpoint;
    readonly TimeProvider _timeProvider;
    Handle? _busHandle;

    /// <summary>The bus endpoint's failure that waiting cannot resolve, once there is one.</summary>
    TerminalFaultObserver? _terminalFault;
    ConnectHandle? _terminalFaultHandle;

    BusState _busState;
    string _healthMessage = "not started";

    public ViciOneServiceBusBus(IHost host, IBusObserver busObservable, IReceiveEndpointConfiguration endpointConfiguration,
        TimeProvider? timeProvider = null)
    {
        Address = endpointConfiguration.InputAddress;
        _consumePipe = endpointConfiguration.ConsumePipe;
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _busObservable = busObservable;
        _receiveEndpoint = endpointConfiguration.ReceiveEndpoint;
        _timeProvider = timeProvider ?? TimeProvider.System;

        _busState = BusState.Created;

        Topology = host.Topology;

        if (LogContext.Current == null)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Vici One Service Bus Bus", "unknown", "The LogContext was not set.", "Correct the named configuration before starting the host"));

        _logContext = LogContext.Current;

        _publishEndpoint = new PublishEndpoint(_receiveEndpoint);
    }

    /// <summary>
    /// Waits for the bus endpoint to be ready after a consumer has been connected to a running bus,
    /// and stops waiting when there is nothing left to wait for.
    /// <para>
    /// The bus endpoint is materialised on demand: it declares its queue when something first
    /// consumes on the bus, not when the bus starts. Connecting a consumer is therefore what brings
    /// it up, and waiting here is deliberate — handing back a subscription that is not live yet
    /// would silently drop messages.
    /// </para>
    /// <para>
    /// The wait is bounded and cancellable. An endpoint that can never start, for instance because
    /// another connection already holds its exclusive queue, would otherwise block the caller on its
    /// thread for good: no exception, no timeout, nothing naming a cause. The bound is the same
    /// sixty seconds this class applies in <see cref="StartAsync" /> when a caller supplies no token
    /// of its own, so there is no second notion of "too long".
    /// </para>
    /// <para>
    /// A fault the transport recovers from leaves the wait exactly as it is: the retry runs and the
    /// endpoint still becomes ready, which is the behaviour every recoverable hiccup during startup
    /// depends on. A fault it cannot recover from ends the wait with the transport's own exception,
    /// because the endpoint will not become ready by itself and holding the caller for the full
    /// readiness limit replaces the broker's answer with a timeout that says nothing.
    /// </para>
    /// <para>
    /// The observer is connected here rather than in the receive endpoint, and that is deliberate.
    /// Completing the endpoint's own Started task on an unrecoverable fault reaches every endpoint
    /// in the process and not only the one being waited for, including the specs that deliberately
    /// provoke a refused credential. The wait is the only place that needs to know, so it is the
    /// only place that is told.
    /// </para>
    /// </summary>
    void WaitUntilBusEndpointIsReady()
    {
        if (_busHandle == null || _receiveEndpoint.Started.IsCompletedSuccessfully())
            return;

        var terminal = _terminalFault;

        using var timeout = new CancellationTokenSource(ReadyTimeout, _timeProvider);

        terminal?.Attach(timeout);
        try
        {
            TaskBlocking.Wait(_receiveEndpoint.Started, timeout.Token);
        }
        // The linked source distinguishes readiness timeout or terminal failure from unrelated cancellation.
        catch (OperationCanceledException) when (timeout.IsCancellationRequested || terminal?.Cause != null)
        {
            // The broker's own answer first; the safety limit only when there is none to give.
            if (terminal?.Cause is { } cause)
                throw cause;

            throw new ConnectionException(
                $"The bus endpoint did not become ready within {ReadyTimeout.TotalSeconds:0} s, so the consumer "
                + $"could not be connected: {Address}");
        }
        finally
        {
            terminal?.Detach(timeout);
        }
    }


    /// <summary>
    /// Preserves the terminal bus-endpoint failure and cancels every registered readiness waiter.
    /// <para>
    /// Terminality is supplied by the receive transport's retry owner. Recoverable attempt faults leave
    /// waiters attached; retry exhaustion or another definitive startup failure wakes them with the
    /// original cause.
    /// </para>
    /// </summary>
    internal class TerminalFaultObserver :
        IReceiveEndpointObserver
    {
        readonly object _lock = new();
        readonly List<CancellationTokenSource> _waiting = new();

        Exception? _cause;

        /// <summary>
        /// Gets the terminal failure under the same lock that publishes it before waiter cancellation,
        /// ensuring a released waiter observes the original cause.
        /// </summary>
        public Exception? Cause
        {
            get
            {
                lock (_lock)
                    return _cause;
            }
        }

        /// <summary>Registers a waiter, and wakes it at once if the failure already happened.</summary>
        /// <param name="waiter">The waiter.</param>
        public void Attach(CancellationTokenSource waiter)
        {
            lock (_lock)
            {
                if (_cause == null)
                {
                    _waiting.Add(waiter);
                    return;
                }
            }

            CancelAttachedWaiter(waiter);
        }



        public void Detach(CancellationTokenSource waiter)
        {
            lock (_lock)
                _waiting.Remove(waiter);
        }

        static void CancelAttachedWaiter(CancellationTokenSource waiter)
        {
            try
            {
                waiter.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // The waiter completed between observing the terminal cause and this cancellation.
            }
            catch (Exception exception)
            {
                // The terminal cause remains authoritative. A cancellation callback is observation
                // attached to the internal wait and must not replace the transport failure.
                LogContext.Warning?.Log(exception, "Bus endpoint readiness cancellation callback faulted");
            }
        }

        static async Task CancelWaitersAsync(CancellationTokenSource[] waiters)
        {
            foreach (var waiter in waiters)
            {
                try
                {
                    await waiter.CancelAsync().ConfigureAwait(false);
                }
                catch (ObjectDisposedException)
                {
                    // The waiter completed independently while the terminal fault was being published.
                }
                catch (Exception exception)
                {
                    // Cancellation wakes an internal readiness wait. Callback failures must be owned and
                    // observable, but must never replace the receive transport's terminal failure.
                    LogContext.Warning?.Log(exception, "Bus endpoint readiness cancellation callback faulted");
                }
            }
        }

        async Task IReceiveEndpointObserver.FaultedAsync(ReceiveEndpointFaulted faulted)
        {
            CancellationTokenSource[] waiting;

            lock (_lock)
            {
                if (_cause != null || !faulted.IsTerminal)
                    return;

                _cause = faulted.Exception;

                waiting = _waiting.ToArray();
                _waiting.Clear();
            }

            await CancelWaitersAsync(waiting).ConfigureAwait(false);
        }

        Task IReceiveEndpointObserver.ReadyAsync(ReceiveEndpointReady ready)
        {
            return Task.CompletedTask;
        }

        Task IReceiveEndpointObserver.StoppingAsync(ReceiveEndpointStopping stopping)
        {
            return Task.CompletedTask;
        }

        Task IReceiveEndpointObserver.CompletedAsync(ReceiveEndpointCompleted completed)
        {
            return Task.CompletedTask;
        }
    }


    /// <summary>
    /// Waits for the bus endpoint and returns the live connection handle.
    /// <para>
    /// If readiness fails, disconnects the handle before propagating the failure so no pipe registration
    /// or request identifier remains attached to the bus.
    /// </para>
    /// </summary>
    /// <param name="handle">The handle.</param>
    /// <returns>The connect handle produced by the operation.</returns>
    ConnectHandle WaitForBusEndpoint(ConnectHandle handle)
    {
        try
        {
            WaitUntilBusEndpointIsReady();
        }
        catch
        {
            handle.Disconnect();
            throw;
        }

        return handle;
    }

    ConnectHandle IConsumePipeConnector.ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe)
    {
        LogContext.SetCurrentIfNull(_logContext);

        return WaitForBusEndpoint(_consumePipe.ConnectConsumePipe(pipe));
    }

    ConnectHandle IConsumePipeConnector.ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options)
    {
        LogContext.SetCurrentIfNull(_logContext);

        return WaitForBusEndpoint(_consumePipe.ConnectConsumePipe(pipe, options));
    }

    ConnectHandle IRequestPipeConnector.ConnectRequestPipe<T>(Guid requestId, IPipe<ConsumeContext<T>> pipe)
    {
        LogContext.SetCurrentIfNull(_logContext);

        return WaitForBusEndpoint(_consumePipe.ConnectRequestPipe(requestId, pipe));
    }

    Task IPublishEndpoint.PublishAsync<T>(T message, CancellationToken cancellationToken)
    {
        LogContext.SetCurrentIfNull(_logContext);

        return _publishEndpoint.PublishAsync(message, cancellationToken);
    }

    Task Advanced.IAdvancedPublishEndpoint.PublishAsync<T>(T message, IPipe<PublishContext<T>> publishPipe, CancellationToken cancellationToken)
    {
        LogContext.SetCurrentIfNull(_logContext);

        return _publishEndpoint.PublishAsync(message, publishPipe, cancellationToken);
    }

    Task Advanced.IAdvancedPublishEndpoint.PublishAsync<T>(T message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken)
    {
        LogContext.SetCurrentIfNull(_logContext);

        return _publishEndpoint.PublishAsync(message, publishPipe, cancellationToken);
    }

    Task Advanced.IAdvancedPublishEndpoint.PublishAsync(object message, CancellationToken cancellationToken)
    {
        LogContext.SetCurrentIfNull(_logContext);

        ArgumentNullException.ThrowIfNull(message);
        return _publishEndpoint.PublishAsync(message, message.GetType(), cancellationToken);
    }

    Task Advanced.IAdvancedPublishEndpoint.PublishAsync(object message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken)
    {
        LogContext.SetCurrentIfNull(_logContext);

        ArgumentNullException.ThrowIfNull(message);
        return _publishEndpoint.PublishAsync(message, message.GetType(), publishPipe, cancellationToken);
    }

    Task Advanced.IAdvancedPublishEndpoint.PublishAsync(object message, Type messageType, CancellationToken cancellationToken)
    {
        LogContext.SetCurrentIfNull(_logContext);

        return _publishEndpoint.PublishAsync(message, messageType, cancellationToken);
    }

    Task Advanced.IAdvancedPublishEndpoint.PublishAsync(object message, Type messageType, IPipe<PublishContext> publishPipe,
        CancellationToken cancellationToken)
    {
        LogContext.SetCurrentIfNull(_logContext);

        return _publishEndpoint.PublishAsync(message, messageType, publishPipe, cancellationToken);
    }

    Task Advanced.IAdvancedPublishEndpoint.PublishAsync<T>(object values, CancellationToken cancellationToken)
    {
        LogContext.SetCurrentIfNull(_logContext);

        return _publishEndpoint.PublishAsync<T>(values, cancellationToken);
    }

    Task Advanced.IAdvancedPublishEndpoint.PublishAsync<T>(object values, IPipe<PublishContext<T>> publishPipe,
        CancellationToken cancellationToken)
    {
        LogContext.SetCurrentIfNull(_logContext);

        return _publishEndpoint.PublishAsync(values, publishPipe, cancellationToken);
    }

    Task Advanced.IAdvancedPublishEndpoint.PublishAsync<T>(object values, IPipe<PublishContext> publishPipe,
        CancellationToken cancellationToken)
    {
        LogContext.SetCurrentIfNull(_logContext);

        return _publishEndpoint.PublishAsync<T>(values, publishPipe, cancellationToken);
    }

    public Uri Address { get; }

    IMessageRouteTable IMessageRouteProvider.MessageRoutes => EndpointConvention.GetMessageRoutes(_receiveEndpoint);

    public IBusTopology Topology { get; }

    Task<ISendEndpoint> ISendEndpointProvider.GetSendEndpointAsync(Uri address, CancellationToken cancellationToken)
    {
        LogContext.SetCurrentIfNull(_logContext);

        return _receiveEndpoint.GetSendEndpointAsync(address, cancellationToken: cancellationToken);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        LogContext.SetCurrentIfNull(_logContext);

        if (_busHandle != null)
        {
            LogContext.Warning?.Log("StartAsync called, but the bus was already started: {Address} ({Reason})", Address, "Already Started");
            return;
        }

        await _busObservable.PreStartAsync(this).ConfigureAwait(false);

        Handle? busHandle = null;

        CancellationTokenSource? tokenSource = null;
        try
        {
            if (cancellationToken == default)
            {
                tokenSource = new CancellationTokenSource(ReadyTimeout, _timeProvider);
                cancellationToken = tokenSource.Token;
            }

            var hostHandle = _host.Start(cancellationToken);

            busHandle = new Handle(_host, hostHandle, this, _busObservable, _logContext);

            try
            {
                await busHandle.Ready.OrCanceledAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException exception) when (exception.CancellationToken == cancellationToken)
            {
                LogContext.Warning?.Log(exception, "Bus start canceled: {HostAddress}", _host.Address);

                try
                {
                    using var stopTimeoutTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30), _timeProvider);
                    using var stopTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, stopTimeoutTokenSource.Token);

                    await busHandle.StopAsync(stopTokenSource.Token).ConfigureAwait(false);
                }
                catch (Exception stopException)
                {
                    LogContext.Warning?.Log(stopException, "Bus start canceled, bus stop faulted: {HostAddress}", _host.Address);
                }

                await busHandle.Ready.ConfigureAwait(false);
            }

            await _busObservable.PostStartAsync(this, busHandle.Ready).ConfigureAwait(false);

            _busHandle = busHandle;

            _terminalFault = new TerminalFaultObserver();
            _terminalFaultHandle = (_receiveEndpoint as ReceiveEndpoint)?.ConnectReceiveEndpointObserver(_terminalFault);

            _busState = BusState.Started;
            _healthMessage = "";

            LogContext.Info?.Log("Bus started: {HostAddress}", _host.Address);

            return;
        }
        catch (Exception ex)
        {
            try
            {
                if (busHandle != null)
                {
                    LogContext.Warning?.Log(ex, "Bus start faulted: {HostAddress}", _host.Address);

                    await busHandle.StopAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception stopException)
            {
                LogContext.Warning?.Log(stopException, "Bus start faulted, bus stop faulted: {HostAddress}", _host.Address);
            }

            _busState = BusState.Faulted;
            _healthMessage = $"start faulted: {ex.Message}";

            await _busObservable.StartFaultedAsync(this, ex).ConfigureAwait(false);

            throw;
        }
        finally
        {
            tokenSource?.Dispose();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        LogContext.SetCurrentIfNull(_logContext);

        if (_busHandle == null)
        {
            LogContext.Warning?.Log("Failed to stop bus: {Address} ({Reason})", Address, "Not Started");
            return;
        }

        // Terminal-fault observation belongs to the current handle and must end even when stopping fails.
        try
        {
            await _busHandle.StopAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _terminalFaultHandle?.Disconnect();
            _terminalFaultHandle = null;
            _terminalFault = null;

            _busHandle = null;
        }
    }

    public BusHealthResult CheckHealth()
    {
        return _host.CheckHealth(_busState, _healthMessage);
    }

    ConnectHandle IConsumeObserverConnector.ConnectConsumeObserver(IConsumeObserver observer)
    {
        LogContext.SetCurrentIfNull(_logContext);

        return _host.ConnectConsumeObserver(observer);
    }

    ConnectHandle IConsumeMessageObserverConnector.ConnectConsumeMessageObserver<T>(IConsumeMessageObserver<T> observer)
    {
        LogContext.SetCurrentIfNull(_logContext);

        return _host.ConnectConsumeMessageObserver(observer);
    }

    public ConnectHandle ConnectReceiveObserver(IReceiveObserver observer)
    {
        LogContext.SetCurrentIfNull(_logContext);

        return _host.ConnectReceiveObserver(observer);
    }

    ConnectHandle IReceiveEndpointObserverConnector.ConnectReceiveEndpointObserver(IReceiveEndpointObserver observer)
    {
        LogContext.SetCurrentIfNull(_logContext);

        return _host.ConnectReceiveEndpointObserver(observer);
    }

    public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
    {
        LogContext.SetCurrentIfNull(_logContext);

        return _host.ConnectPublishObserver(observer);
    }

    public Task<ISendEndpoint> GetPublishSendEndpointAsync<T>(CancellationToken cancellationToken = default)
        where T : class
    {
        LogContext.SetCurrentIfNull(_logContext);

        return _receiveEndpoint.GetPublishSendEndpointAsync<T>(cancellationToken: cancellationToken);
    }

    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        LogContext.SetCurrentIfNull(_logContext);

        return _host.ConnectSendObserver(observer);
    }

    ConnectHandle IEndpointConfigurationObserverConnector.ConnectEndpointConfigurationObserver(IEndpointConfigurationObserver observer)
    {
        LogContext.SetCurrentIfNull(_logContext);

        return _host.ConnectEndpointConfigurationObserver(observer);
    }

    HostReceiveEndpointHandle IReceiveConnector.ConnectReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<IReceiveEndpointConfigurator>? configureEndpoint)
    {
        return _host.ConnectReceiveEndpoint(definition, endpointNameFormatter, configureEndpoint);
    }

    HostReceiveEndpointHandle IReceiveConnector.ConnectReceiveEndpoint(string queueName, Action<IReceiveEndpointConfigurator>? configureEndpoint)
    {
        return _host.ConnectReceiveEndpoint(queueName, configureEndpoint);
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        var scope = context.CreateScope("bus");
        scope.Add("address", Address);

        _host.Probe(scope);
    }


    sealed class Handle
    {
        readonly ViciOneServiceBusBus _bus;
        readonly IBusObserver _busObserver;
        readonly IHost _host = null!;
        readonly HostHandle _hostHandle;
        readonly ILogContext _logContext;
        bool _stopped;

        public Handle(IHost host, HostHandle hostHandle, ViciOneServiceBusBus bus, IBusObserver busObserver, ILogContext logContext)
        {
            _host = host;
            _bus = bus;
            _busObserver = busObserver;
            _logContext = logContext;
            _hostHandle = hostHandle;

            Ready = ReadyOrNotAsync(hostHandle.Ready);
        }

        public Task<BusReady> Ready { get; }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            LogContext.SetCurrentIfNull(_logContext);

            if (_stopped)
                return;

            await _busObserver.PreStopAsync(_bus).ConfigureAwait(false);

            try
            {
                await _hostHandle.StopAsync(cancellationToken).ConfigureAwait(false);

                await _busObserver.PostStopAsync(_bus).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                await _busObserver.StopFaultedAsync(_bus, exception).ConfigureAwait(false);

                LogContext.Warning?.Log(exception, "Bus stop faulted: {HostAddress}", _host.Address);

                _bus._busState = BusState.Faulted;
                _bus._healthMessage = $"stop faulted: {exception.Message}";

                throw;
            }

            LogContext.Info?.Log("Bus stopped: {HostAddress}", _host.Address);

            _stopped = true;

            _bus._busState = BusState.Stopped;
            _bus._healthMessage = "stopped";
        }

        async Task<BusReady> ReadyOrNotAsync(Task<HostReady> ready)
        {
            var hostReady = await ready.ConfigureAwait(false);

            return new BusReadyEvent(hostReady, _bus);
        }
    }
}
