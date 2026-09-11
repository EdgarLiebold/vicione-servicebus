using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Events.Receiving;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Controls a receive transport and exposes its configured consume pipeline, lifecycle, and health.</summary>
public sealed class ReceiveEndpoint :
    IReceiveEndpoint,
    IRestartableReceiveEndpoint,
    IMessageRouteProvider
{
    /// <summary>Specifies the receive endpoint lifecycle state.</summary>
    public enum State
    {
        /// <summary>The endpoint has not been started.</summary>
        Initial,
        /// <summary>The transport is starting and awaiting readiness.</summary>
        Starting,
        /// <summary>The endpoint is ready to consume messages.</summary>
        Ready,
        /// <summary>The endpoint is stopping its active transport generation.</summary>
        Stopping,
        /// <summary>The endpoint is stopped and retains its configuration.</summary>
        Stopped,
        /// <summary>The endpoint is paused and may be restarted by its policy.</summary>
        Paused,
        /// <summary>The endpoint encountered a transport failure.</summary>
        Faulted
    }


    readonly ReceiveEndpointContext _context;
    readonly SemaphoreSlim _lifecycleGate;
    readonly TaskCompletionSource<ReceiveEndpointReady> _started;
    readonly StartObserver _startObserver;
    readonly IReceiveTransport _transport;
    EndpointHandle? _handle;
    bool _paused;
    bool _resetPending;

    /// <summary>Initializes an endpoint for a receive transport and its configured pipeline.</summary>
    /// <param name="transport">The transport that receives messages.</param>
    /// <param name="context">The endpoint configuration and receive pipeline.</param>
    public ReceiveEndpoint(IReceiveTransport transport, ReceiveEndpointContext context)
    {
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentNullException.ThrowIfNull(context);

        _context = context;
        _transport = transport;
        _lifecycleGate = new SemaphoreSlim(1, 1);

        _started = new TaskCompletionSource<ReceiveEndpointReady>(TaskCreationOptions.RunContinuationsAsynchronously);

        InputAddress = context.InputAddress;
        Message = "not ready";
        HealthResult = EndpointHealthResult.Unhealthy(this, Message, null);

        _startObserver = new StartObserver();

        ConnectReceiveEndpointObserver(new HealthResultReceiveEndpointObserver(this));
        ConnectReceiveEndpointObserver(_startObserver);

        transport.ConnectReceiveTransportObserver(new Observer(this, context.EndpointObservers));
    }

    /// <summary>Gets the current lifecycle state.</summary>
    public State CurrentState { get; internal set; }

    /// <summary>Gets the diagnostic associated with the current lifecycle state.</summary>
    public string Message { get; internal set; }

    /// <summary>Gets the latest endpoint health observation.</summary>
    public EndpointHealthResult HealthResult { get; internal set; }

    /// <summary>Gets whether this endpoint receives messages addressed to the bus itself.</summary>
    public bool IsBusEndpoint => _context.IsBusEndpoint;

    /// <summary>Gets the address on which the endpoint receives messages.</summary>
    public Uri InputAddress { get; internal set; }

    IMessageRouteTable IMessageRouteProvider.MessageRoutes => _context.MessageRoutes;

    /// <summary>Gets a task that completes the first time the endpoint becomes ready.</summary>
    public Task<ReceiveEndpointReady> Started => _started.Task;

    internal ConnectHandle? ObserverHandle { get; set; }

    Logging.ILogContext IRestartableReceiveEndpoint.LogContext => _context.LogContext;

    /// <summary>Starts the receive transport.</summary>
    /// <param name="cancellationToken">The token that cancels endpoint startup.</param>
    /// <returns>A handle that exposes readiness and controls the started endpoint.</returns>
    public IReceiveEndpointHandle Start(CancellationToken cancellationToken)
    {
        _lifecycleGate.Wait(cancellationToken);
        try
        {
            return StartTransport(cancellationToken);
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    IReceiveEndpointHandle StartTransport(CancellationToken cancellationToken)
    {
        LogContext.SetCurrentIfNull(_context.LogContext);

        if (_handle != null)
            throw new InvalidOperationException($"The receive endpoint was already started: {InputAddress}");

        var handle = new EndpointHandle(this, _transport, _startObserver, cancellationToken);
        _handle = handle;
        Message = "starting";
        HealthResult = EndpointHealthResult.Degraded(this, Message);
        CurrentState = State.Starting;

        try
        {
            handle.Start();
            _paused = false;
        }
        catch (Exception exception)
        {
            _handle = null;
            Message = $"start faulted ({exception.Message})";
            HealthResult = EndpointHealthResult.Unhealthy(this, Message, exception);
            CurrentState = State.Faulted;
            throw;
        }

        return _handle;
    }

    /// <summary>Stops the receive transport without removing the endpoint from its host.</summary>
    /// <param name="cancellationToken">The token that cancels the stop operation.</param>
    /// <returns>A task that completes after the endpoint and its owned resources have stopped.</returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return StopAsync(false, cancellationToken);
    }

    internal bool IsPaused => Volatile.Read(ref _paused);

    /// <summary>Adds transport and receive-pipeline diagnostics to a probe.</summary>
    /// <param name="context">The probe context that receives the diagnostics.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _transport.Probe(context);

        _context.ReceivePipe.Probe(context);
    }

    /// <summary>Subscribes an observer to consume pipeline notifications.</summary>
    /// <param name="observer">The observer that receives consume notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumeObserver(IConsumeObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _context.ReceivePipe.ConnectConsumeObserver(observer);
    }

    /// <summary>Connects a pipeline that receives every consumed message of a contract type.</summary>
    /// <typeparam name="T">The consumed message contract.</typeparam>
    /// <param name="pipe">The pipeline invoked for matching messages.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(pipe);
        return _context.ReceivePipe.ConnectConsumePipe(pipe);
    }

    /// <summary>Connects a configurable pipeline that receives every consumed message of a contract type.</summary>
    /// <typeparam name="T">The consumed message contract.</typeparam>
    /// <param name="pipe">The pipeline invoked for matching messages.</param>
    /// <param name="options">The settings that control pipe connection and scheduling.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(pipe);
        return _context.ReceivePipe.ConnectConsumePipe(pipe, options);
    }

    /// <summary>Connects a pipeline for a specific request and response message contract.</summary>
    /// <typeparam name="T">The response message contract.</typeparam>
    /// <param name="requestId">The request correlation identifier.</param>
    /// <param name="pipe">The pipeline invoked for matching responses.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectRequestPipe<T>(Guid requestId, IPipe<ConsumeContext<T>> pipe)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(pipe);
        return _context.ReceivePipe.ConnectRequestPipe(requestId, pipe);
    }

    /// <summary>Subscribes an observer to messages published from this endpoint context.</summary>
    /// <param name="observer">The observer that receives publish notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _context.ConnectPublishObserver(observer);
    }

    /// <summary>Subscribes an observer to messages sent from this endpoint context.</summary>
    /// <param name="observer">The observer that receives send notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _context.ConnectSendObserver(observer);
    }

    /// <summary>Resolves an endpoint that sends messages to a destination.</summary>
    /// <param name="address">The destination address.</param>
    /// <param name="cancellationToken">The token that cancels endpoint resolution.</param>
    /// <returns>A task that produces the send endpoint.</returns>
    public Task<ISendEndpoint> GetSendEndpointAsync(Uri address, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(address);
        cancellationToken.ThrowIfCancellationRequested();
        return _context.SendEndpointProvider.GetSendEndpointAsync(address, cancellationToken: cancellationToken);
    }

    /// <summary>Resolves the publish destination for a message contract.</summary>
    /// <typeparam name="T">The published message contract.</typeparam>
    /// <param name="cancellationToken">The token that cancels endpoint resolution.</param>
    /// <returns>A task that produces the publish send endpoint.</returns>
    public Task<ISendEndpoint> GetPublishSendEndpointAsync<T>(CancellationToken cancellationToken = default)
        where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _context.PublishEndpointProvider.GetPublishSendEndpointAsync<T>(cancellationToken: cancellationToken);
    }

    /// <summary>Subscribes an observer to receive transport notifications.</summary>
    /// <param name="observer">The observer that receives transport notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectReceiveObserver(IReceiveObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _context.ConnectReceiveObserver(observer);
    }

    /// <summary>Subscribes a typed observer to consume pipeline notifications.</summary>
    /// <typeparam name="T">The observed message contract.</typeparam>
    /// <param name="observer">The observer that receives typed consume notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumeMessageObserver<T>(IConsumeMessageObserver<T> observer)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _context.ReceivePipe.ConnectConsumeMessageObserver(observer);
    }

    /// <summary>Subscribes an observer to this endpoint's lifecycle notifications.</summary>
    /// <param name="observer">The observer that receives endpoint lifecycle notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectReceiveEndpointObserver(IReceiveEndpointObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return _context.ConnectReceiveEndpointObserver(observer);
    }

    /// <summary>Determines whether the endpoint still owns an active transport handle.</summary>
    /// <returns><see langword="true" /> while an active transport generation is owned; otherwise, <see langword="false" />.</returns>
    internal bool IsStarted()
    {
        return Volatile.Read(ref _handle) is not null;
    }

    /// <summary>Stops the receive transport and optionally reports removal from the host.</summary>
    /// <param name="removed">Whether the endpoint is being removed from its host.</param>
    /// <param name="cancellationToken">The token that cancels the stop operation.</param>
    /// <returns>A task that completes after the endpoint context has reset.</returns>
    internal async Task StopAsync(bool removed, CancellationToken cancellationToken)
    {
        await _lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            LogContext.SetCurrentIfNull(_context.LogContext);

            if (_handle != null)
            {
                _resetPending = true;
                await StopTransportAsync(removed, cancellationToken).ConfigureAwait(false);
            }
            else if (_paused)
            {
                // An external stop terminates a paused endpoint generation and cancels any pending policy restart.
                await _context.EndpointObservers.StoppingAsync(new ReceiveEndpointStoppingEvent(_context.InputAddress, this, removed)).ConfigureAwait(false);
            }

            _paused = false;
            if (_resetPending)
            {
                await _context.ResetAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                _resetPending = false;
            }

            if (CurrentState == State.Stopping || CurrentState == State.Paused)
            {
                Message = "stopped";
                HealthResult = EndpointHealthResult.Degraded(this, Message);
                CurrentState = State.Stopped;
            }
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    async Task IRestartableReceiveEndpoint.PauseAsync(CancellationToken cancellationToken)
    {
        await _lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            LogContext.SetCurrentIfNull(_context.LogContext);

            if (_handle == null)
            {
                if (_paused && _resetPending)
                {
                    await _context.ResetAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                    _resetPending = false;
                    Message = "paused";
                    HealthResult = EndpointHealthResult.Degraded(this, Message);
                    CurrentState = State.Paused;
                }

                return;
            }

            // The paused state remains visible after the active transport handle has been released.
            _paused = true;
            _resetPending = true;
            await StopTransportAsync(false, cancellationToken).ConfigureAwait(false);
            await _context.ResetAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            _resetPending = false;
            Message = "paused";
            HealthResult = EndpointHealthResult.Degraded(this, Message);
            CurrentState = State.Paused;
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    async Task<IReceiveEndpointHandle> IRestartableReceiveEndpoint.RestartAsync(CancellationToken cancellationToken)
    {
        await _lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!_paused)
                throw new InvalidOperationException("The receive endpoint cannot restart because it is not paused.");

            return StartTransport(cancellationToken);
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    async Task StopTransportAsync(bool removed, CancellationToken cancellationToken)
    {
        var handle = _handle ?? throw new InvalidOperationException("The receive endpoint is not running.");

        await _context.DependentsCompleted.OrCanceledAsync(cancellationToken).ConfigureAwait(false);

        await _context.EndpointObservers.StoppingAsync(new ReceiveEndpointStoppingEvent(_context.InputAddress, this, removed)).ConfigureAwait(false);

        await handle.TransportHandle.StopAsync(cancellationToken).ConfigureAwait(false);

        _handle = null;
    }


    sealed class Observer :
        IReceiveTransportObserver
    {
        readonly ReceiveEndpoint _endpoint;
        readonly IReceiveEndpointObserver _observer;

        public Observer(ReceiveEndpoint endpoint, IReceiveEndpointObserver observer)
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            _observer = observer ?? throw new ArgumentNullException(nameof(observer));
        }

        public Task ReadyAsync(ReceiveTransportReady ready)
        {
            ArgumentNullException.ThrowIfNull(ready);
            var endpointReadyEvent = new ReceiveEndpointReadyEvent(ready.InputAddress, _endpoint, ready.IsStarted);
            if (ready.IsStarted)
                _endpoint._started.TrySetResult(endpointReadyEvent);

            return _observer.ReadyAsync(endpointReadyEvent)
                ?? throw new InvalidOperationException("The receive endpoint observer returned no ready-notification task.");
        }

        public Task CompletedAsync(ReceiveTransportCompleted completed)
        {
            ArgumentNullException.ThrowIfNull(completed);
            return _observer.CompletedAsync(new ReceiveEndpointCompletedEvent(completed, _endpoint))
                ?? throw new InvalidOperationException("The receive endpoint observer returned no completion-notification task.");
        }

        public Task FaultedAsync(ReceiveTransportFaulted faulted)
        {
            ArgumentNullException.ThrowIfNull(faulted);
            return _observer.FaultedAsync(new ReceiveEndpointFaultedEvent(faulted, _endpoint))
                ?? throw new InvalidOperationException("The receive endpoint observer returned no fault-notification task.");
        }
    }


    sealed class StartObserver :
        IReceiveEndpointObserver
    {
        readonly Connectable<EndpointHandle> _handles;

        public StartObserver()
        {
            _handles = new Connectable<EndpointHandle>();
        }

        Task IReceiveEndpointObserver.ReadyAsync(ReceiveEndpointReady ready)
        {
            ArgumentNullException.ThrowIfNull(ready);
            return _handles.ForEachAsync(x => x.SetReadyAsync(ready));
        }

        public Task StoppingAsync(ReceiveEndpointStopping stopping)
        {
            ArgumentNullException.ThrowIfNull(stopping);
            return _handles.ForEachAsync(static x => x.SetStoppedAsync());
        }

        Task IReceiveEndpointObserver.CompletedAsync(ReceiveEndpointCompleted completed)
        {
            ArgumentNullException.ThrowIfNull(completed);
            return Task.CompletedTask;
        }

        Task IReceiveEndpointObserver.FaultedAsync(ReceiveEndpointFaulted faulted)
        {
            ArgumentNullException.ThrowIfNull(faulted);
            return _handles.ForEachAsync(x => x.SetFaultedAsync(faulted));
        }

        public ConnectHandle ConnectEndpointHandle(EndpointHandle handle)
        {
            ArgumentNullException.ThrowIfNull(handle);
            return _handles.Connect(handle);
        }
    }


    sealed class EndpointHandle :
        IReceiveEndpointHandle
    {
        readonly CancellationToken _cancellationToken;
        readonly ReceiveEndpoint _endpoint;
        readonly ConnectHandle _handle;
        readonly TaskCompletionSource<ReceiveEndpointReady> _ready;
        readonly IReceiveTransport _transport;
        CancellationTokenRegistration _registration;

        public EndpointHandle(ReceiveEndpoint endpoint, IReceiveTransport transport, StartObserver startObserver, CancellationToken cancellationToken)
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            ArgumentNullException.ThrowIfNull(startObserver);

            _cancellationToken = cancellationToken;
            _ready = new TaskCompletionSource<ReceiveEndpointReady>(TaskCreationOptions.RunContinuationsAsynchronously);
            _handle = startObserver.ConnectEndpointHandle(this);

            if (cancellationToken.CanBeCanceled)
                _registration = cancellationToken.UnsafeRegister(static state =>
                    (state as EndpointHandle ?? throw new InvalidOperationException("The endpoint handle cancellation state is invalid.")).CancelReady(), this);
        }

        ReceiveTransportHandle? _transportHandle;

        public ReceiveTransportHandle TransportHandle => _transportHandle
            ?? throw new InvalidOperationException("The receive transport has not been started.");
        public Task<ReceiveEndpointReady> Ready => _ready.Task;

        Task IReceiveEndpointHandle.StopAsync(CancellationToken cancellationToken)
        {
            return _endpoint.StopAsync(cancellationToken);
        }

        public void Start()
        {
            _cancellationToken.ThrowIfCancellationRequested();

            try
            {
                _transportHandle = _transport.Start()
                    ?? throw new InvalidOperationException("The receive transport returned no lifecycle handle.");
            }
            catch (Exception exception)
            {
                _handle.Disconnect();
                _registration.Dispose();
                _ready.TrySetException(exception);
                throw;
            }
        }

        public Task SetReadyAsync(ReceiveEndpointReady ready)
        {
            ArgumentNullException.ThrowIfNull(ready);
            _handle.Disconnect();
            _registration.Dispose();

            _ready.TrySetResult(ready);

            return Task.CompletedTask;
        }

        public Task SetFaultedAsync(ReceiveEndpointFaulted faulted)
        {
            ArgumentNullException.ThrowIfNull(faulted);
            if (!faulted.IsTerminal)
                return Task.CompletedTask;

            _handle.Disconnect();
            _registration.Dispose();

            _ready.TrySetException(faulted.Exception);

            return Task.CompletedTask;
        }

        public Task SetStoppedAsync()
        {
            _handle.Disconnect();
            _registration.Dispose();

            _ready.TrySetCanceled();

            return Task.CompletedTask;
        }

        void CancelReady()
        {
            _handle.Disconnect();
            _ready.TrySetCanceled(_cancellationToken);
        }
    }
}
