using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Events;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// A receive endpoint is called by the receive transport to push messages to consumers.
/// The receive endpoint is where the initial deserialization occurs, as well as any additional
/// filters on the receive context.
/// </summary>
public class ReceiveEndpoint :
    IReceiveEndpoint,
    IRestartableReceiveEndpoint,
    IMessageRouteProvider
{
    /// <summary>Specifies the available state values.</summary>
    public enum State
    {
        /// <summary>Indicates initial.</summary>
        Initial,
        /// <summary>Indicates started.</summary>
        Started,
        /// <summary>Indicates ready.</summary>
        Ready,
        /// <summary>Indicates completed.</summary>
        Completed,
        /// <summary>Indicates faulted.</summary>
        Faulted,
        /// <summary>Indicates final.</summary>
        Final
    }


    readonly ReceiveEndpointContext _context;
    readonly SemaphoreSlim _lifecycleGate;
    readonly TaskCompletionSource<ReceiveEndpointReady> _started;
    readonly StartObserver _startObserver;
    readonly IReceiveTransport _transport;
    EndpointHandle? _handle;
    bool _paused;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="transport">The transport.</param>
    /// <param name="context">The context associated with the operation.</param>
    public ReceiveEndpoint(IReceiveTransport transport, ReceiveEndpointContext context)
    {
        _context = context;
        _transport = transport;
        _lifecycleGate = new SemaphoreSlim(1, 1);

        _started = new TaskCompletionSource<ReceiveEndpointReady>(TaskCreationOptions.RunContinuationsAsynchronously);

        InputAddress = context.InputAddress;

        _startObserver = new StartObserver();

        ConnectReceiveEndpointObserver(_startObserver);

        transport.ConnectReceiveTransportObserver(new Observer(this, context.EndpointObservers));
    }

    /// <summary>Gets or sets the current state.</summary>
    public State CurrentState { get; set; }

    /// <summary>Gets or sets the message.</summary>
    public string Message { get; set; } = null!;
    /// <summary>Gets or sets the health result.</summary>
    public EndpointHealthResult HealthResult { get; set; }

    /// <summary>Gets a value indicating whether bus endpoint.</summary>
    public bool IsBusEndpoint => _context.IsBusEndpoint;

    /// <summary>Gets or sets the input address.</summary>
    public Uri InputAddress { get; set; }

    IMessageRouteTable IMessageRouteProvider.MessageRoutes => _context.MessageRoutes;

    /// <summary>Gets the started.</summary>
    public Task<ReceiveEndpointReady> Started => _started.Task;
    /// <summary>Gets or sets the observer handle.</summary>
    public ConnectHandle ObserverHandle { get; set; } = null!; Logging.ILogContext IRestartableReceiveEndpoint.LogContext => _context.LogContext;

    /// <summary>Starts the configured component.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The receive endpoint handle produced by the operation.</returns>
    public ReceiveEndpointHandle Start(CancellationToken cancellationToken)
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

    ReceiveEndpointHandle StartTransport(CancellationToken cancellationToken)
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

    /// <summary>Stops the configured component.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return StopAsync(false, cancellationToken);
    }

    internal bool IsPaused => Volatile.Read(ref _paused);

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        _transport.Probe(context);

        _context.ReceivePipe.Probe(context);
    }

    /// <summary>Connects consume observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumeObserver(IConsumeObserver observer)
    {
        return _context.ReceivePipe.ConnectConsumeObserver(observer);
    }

    /// <summary>Connects consume pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe)
        where T : class
    {
        return _context.ReceivePipe.ConnectConsumePipe(pipe);
    }

    /// <summary>Connects consume pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options)
        where T : class
    {
        return _context.ReceivePipe.ConnectConsumePipe(pipe, options);
    }

    /// <summary>Connects request pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="requestId">The request id.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectRequestPipe<T>(Guid requestId, IPipe<ConsumeContext<T>> pipe)
        where T : class
    {
        return _context.ReceivePipe.ConnectRequestPipe(requestId, pipe);
    }

    /// <summary>Connects publish observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
    {
        return _context.ConnectPublishObserver(observer);
    }

    /// <summary>Connects send observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectSendObserver(ISendObserver observer)
    {
        return _context.ConnectSendObserver(observer);
    }

    /// <summary>Gets send endpoint.</summary>
    /// <param name="address">The address.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public Task<ISendEndpoint> GetSendEndpointAsync(Uri address, CancellationToken cancellationToken = default)
    {
        return _context.SendEndpointProvider.GetSendEndpointAsync(address, cancellationToken: cancellationToken);
    }

    /// <summary>Gets publish send endpoint.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public Task<ISendEndpoint> GetPublishSendEndpointAsync<T>(CancellationToken cancellationToken = default)
        where T : class
    {
        return _context.PublishEndpointProvider.GetPublishSendEndpointAsync<T>(cancellationToken: cancellationToken);
    }

    /// <summary>Connects receive observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectReceiveObserver(IReceiveObserver observer)
    {
        return _context.ConnectReceiveObserver(observer);
    }

    /// <summary>Connects consume message observer.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumeMessageObserver<T>(IConsumeMessageObserver<T> observer)
        where T : class
    {
        return _context.ReceivePipe.ConnectConsumeMessageObserver(observer);
    }

    /// <summary>Connects receive endpoint observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectReceiveEndpointObserver(IReceiveEndpointObserver observer)
    {
        return _context.ConnectReceiveEndpointObserver(observer);
    }

    /// <summary>Determines whether started.</summary>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool IsStarted()
    {
        return State.Started.Equals(CurrentState) || State.Ready.Equals(CurrentState) || State.Faulted.Equals(CurrentState);
    }

    /// <summary>Stops the configured component.</summary>
    /// <param name="removed">The removed.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task StopAsync(bool removed, CancellationToken cancellationToken)
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

    async Task<ReceiveEndpointHandle> IRestartableReceiveEndpoint.RestartAsync(CancellationToken cancellationToken)
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


    class Observer :
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


    class StartObserver :
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
            return Task.CompletedTask;
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


    class EndpointHandle :
        ReceiveEndpointHandle
    {
        readonly CancellationToken _cancellationToken;
        readonly ReceiveEndpoint _endpoint;
        readonly ConnectHandle _handle = null!;
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

        public ReceiveTransportHandle TransportHandle { get; private set; } = null!;
        public Task<ReceiveEndpointReady> Ready => _ready.Task;

        Task ReceiveEndpointHandle.StopAsync(CancellationToken cancellationToken)
        {
            return _endpoint.StopAsync(cancellationToken);
        }

        public void Start()
        {
            _cancellationToken.ThrowIfCancellationRequested();

            try
            {
                TransportHandle = _transport.Start();
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

        void CancelReady()
        {
            _handle.Disconnect();
            _ready.TrySetCanceled(_cancellationToken);
        }
    }
}
