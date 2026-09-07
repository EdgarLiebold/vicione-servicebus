using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Events;
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
        /// <summary>The transport has started and is awaiting readiness.</summary>
        Started,
        /// <summary>The endpoint is ready to consume messages.</summary>
        Ready,
        /// <summary>The endpoint completed its most recent run.</summary>
        Completed,
        /// <summary>The endpoint encountered a transport failure.</summary>
        Faulted,
        /// <summary>The endpoint has reached its terminal lifecycle state.</summary>
        Final
    }


    readonly ReceiveEndpointContext _context;
    readonly SemaphoreSlim _lifecycleGate;
    readonly TaskCompletionSource<ReceiveEndpointReady> _started;
    readonly StartObserver _startObserver;
    readonly IReceiveTransport _transport;
    EndpointHandle? _handle;
    bool _paused;

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
        _paused = false;

        try
        {
            handle.Start();
        }
        catch (Exception exception)
        {
            _handle = null;
            Message = $"start faulted ({exception.Message})";
            HealthResult = EndpointHealthResult.Unhealthy(this, Message, exception);
            CurrentState = State.Faulted;
            throw;
        }

        switch (CurrentState)
        {
            case State.Initial:
            case State.Completed:
            case State.Faulted:
                Message = "starting";
                CurrentState = State.Started;
                break;
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
        _transport.Probe(context);

        _context.ReceivePipe.Probe(context);
    }

    /// <summary>Subscribes an observer to consume pipeline notifications.</summary>
    /// <param name="observer">The observer that receives consume notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumeObserver(IConsumeObserver observer)
    {
        return _context.ReceivePipe.ConnectConsumeObserver(observer);
    }

    /// <summary>Connects a pipeline that receives every consumed message of a contract type.</summary>
    /// <typeparam name="T">The consumed message contract.</typeparam>
    /// <param name="pipe">The pipeline invoked for matching messages.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe)
        where T : class
    {
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
        return _context.ReceivePipe.ConnectRequestPipe(requestId, pipe);
    }

    /// <summary>Subscribes an observer to messages published from this endpoint context.</summary>
    /// <param name="observer">The observer that receives publish notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
    {
        return _context.ConnectPublishObserver(observer);
    }

    /// <summary>Subscribes an observer to messages sent from this endpoint context.</summary>
    /// <param name="observer">The observer that receives send notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        return _context.ConnectSendObserver(observer);
    }

    /// <summary>Resolves an endpoint that sends messages to a destination.</summary>
    /// <param name="address">The destination address.</param>
    /// <param name="cancellationToken">The token that cancels endpoint resolution.</param>
    /// <returns>A task that produces the send endpoint.</returns>
    public Task<ISendEndpoint> GetSendEndpointAsync(Uri address, CancellationToken cancellationToken = default)
    {
        return _context.SendEndpointProvider.GetSendEndpointAsync(address, cancellationToken: cancellationToken);
    }

    /// <summary>Resolves the publish destination for a message contract.</summary>
    /// <typeparam name="T">The published message contract.</typeparam>
    /// <param name="cancellationToken">The token that cancels endpoint resolution.</param>
    /// <returns>A task that produces the publish send endpoint.</returns>
    public Task<ISendEndpoint> GetPublishSendEndpointAsync<T>(CancellationToken cancellationToken = default)
        where T : class
    {
        return _context.PublishEndpointProvider.GetPublishSendEndpointAsync<T>(cancellationToken: cancellationToken);
    }

    /// <summary>Subscribes an observer to receive transport notifications.</summary>
    /// <param name="observer">The observer that receives transport notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectReceiveObserver(IReceiveObserver observer)
    {
        return _context.ConnectReceiveObserver(observer);
    }

    /// <summary>Subscribes a typed observer to consume pipeline notifications.</summary>
    /// <typeparam name="T">The observed message contract.</typeparam>
    /// <param name="observer">The observer that receives typed consume notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumeMessageObserver<T>(IConsumeMessageObserver<T> observer)
        where T : class
    {
        return _context.ReceivePipe.ConnectConsumeMessageObserver(observer);
    }

    /// <summary>Subscribes an observer to this endpoint's lifecycle notifications.</summary>
    /// <param name="observer">The observer that receives endpoint lifecycle notifications.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectReceiveEndpointObserver(IReceiveEndpointObserver observer)
    {
        return _context.ConnectReceiveEndpointObserver(observer);
    }

    /// <summary>Determines whether the endpoint owns or is recovering a started transport.</summary>
    /// <returns><see langword="true" /> while the endpoint is started, ready, or faulted; otherwise, <see langword="false" />.</returns>
    internal bool IsStarted()
    {
        return State.Started.Equals(CurrentState) || State.Ready.Equals(CurrentState) || State.Faulted.Equals(CurrentState);
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
                await StopTransportAsync(removed, cancellationToken).ConfigureAwait(false);
            else if (_paused)
            {
                // A policy pause has no active transport handle, but a later external stop is still a
                // terminal lifecycle event. Publishing it lets the policy cancel a pending restart.
                await _context.EndpointObservers.StoppingAsync(new ReceiveEndpointStoppingEvent(_context.InputAddress, this, removed)).ConfigureAwait(false);
            }

            _paused = false;
            await _context.ResetAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
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
                return;

            // Mark the endpoint before stopping the transport so a concurrent host stop cannot omit it.
            _paused = true;
            await StopTransportAsync(false, cancellationToken).ConfigureAwait(false);
            await _context.ResetAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
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
            _endpoint = endpoint;
            _observer = observer;
        }

        public Task ReadyAsync(ReceiveTransportReady ready)
        {
            var endpointReadyEvent = new ReceiveEndpointReadyEvent(ready.InputAddress, _endpoint, ready.IsStarted);
            if (ready.IsStarted)
                _endpoint._started.TrySetResult(endpointReadyEvent);

            return _observer.ReadyAsync(endpointReadyEvent);
        }

        public Task CompletedAsync(ReceiveTransportCompleted completed)
        {
            return _observer.CompletedAsync(new ReceiveEndpointCompletedEvent(completed, _endpoint));
        }

        public Task FaultedAsync(ReceiveTransportFaulted faulted)
        {
            return _observer.FaultedAsync(new ReceiveEndpointFaultedEvent(faulted, _endpoint));
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
            return _handles.ForEachAsync(x => x.SetReadyAsync(ready));
        }

        public Task StoppingAsync(ReceiveEndpointStopping stopping)
        {
            return _handles.ForEachAsync(static x => x.SetStoppedAsync());
        }

        Task IReceiveEndpointObserver.CompletedAsync(ReceiveEndpointCompleted completed)
        {
            return Task.CompletedTask;
        }

        Task IReceiveEndpointObserver.FaultedAsync(ReceiveEndpointFaulted faulted)
        {
            return _handles.ForEachAsync(x => x.SetFaultedAsync(faulted));
        }

        public ConnectHandle ConnectEndpointHandle(EndpointHandle handle)
        {
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
            _endpoint = endpoint;
            _transport = transport;

            _cancellationToken = cancellationToken;
            _ready = new TaskCompletionSource<ReceiveEndpointReady>(TaskCreationOptions.RunContinuationsAsynchronously);
            _handle = startObserver.ConnectEndpointHandle(this);

            if (cancellationToken.CanBeCanceled)
                _registration = cancellationToken.UnsafeRegister(static state => ((EndpointHandle)state!).CancelReady(), this);
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
                _transportHandle = _transport.Start();
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
            _handle.Disconnect();
            _registration.Dispose();

            _ready.TrySetResult(ready);

            return Task.CompletedTask;
        }

        public Task SetFaultedAsync(ReceiveEndpointFaulted faulted)
        {
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
