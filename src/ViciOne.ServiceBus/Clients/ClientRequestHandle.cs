using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Clients;

/// <summary>Coordinates sending one request and awaiting one of its registered response contracts.</summary>
/// <typeparam name="TRequest">The request type.</typeparam>
internal sealed class ClientRequestHandle<TRequest> :
    RequestHandle<TRequest>,
    IPipe<SendContext<TRequest>>
    where TRequest : class
{
    /// <summary>Sends the request after its response contracts have been registered.</summary>
    /// <param name="requestId">The request id.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public delegate Task<TRequest> SendRequestCallback(Guid requestId, IPipe<SendContext<TRequest>> pipe, CancellationToken cancellationToken);


    readonly List<string> _accept;
    readonly CancellationToken _cancellationToken;
    readonly CancellationTokenSource _cancellationTokenSource;
    readonly ClientFactoryContext _context;
    readonly object _handlerLock;
    readonly TaskCompletionSource<TRequest> _message;
    readonly IBuildPipeConfigurator<SendContext<TRequest>> _pipeConfigurator;
    readonly TaskCompletionSource<bool> _readyToSend;
    readonly CancellationTokenRegistration _registration;
    readonly Dictionary<Type, HandlerConnectHandle> _responseHandlers;
    readonly Task _send;
    readonly TaskCompletionSource<SendContext<TRequest>> _sendContext;
    readonly SendRequestCallback _sendRequestCallback;
    readonly TaskScheduler _taskScheduler;
    readonly RequestTimeout _timeout;
    int _faultedOrCanceled;
    ConnectHandle? _faultHandler;
    ITimer? _timeoutTimer;
    RequestTimeout _timeToLive;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="sendRequestCallback">The send request callback.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="requestId">The request id.</param>
    /// <param name="taskScheduler">The task scheduler.</param>
    public ClientRequestHandle(ClientFactoryContext context, SendRequestCallback sendRequestCallback, CancellationToken cancellationToken = default,
        RequestTimeout timeout = default, Guid? requestId = null, TaskScheduler? taskScheduler = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(sendRequestCallback);

        _context = context;
        _sendRequestCallback = sendRequestCallback;
        _cancellationToken = cancellationToken;

        _timeout = timeout.HasValue ? timeout : _context.DefaultTimeout.HasValue ? _context.DefaultTimeout.Value : RequestTimeout.Default;
        _timeToLive = _timeout;

        RequestId = requestId ?? NewId.NextGuid();

        _taskScheduler = taskScheduler ??
            (SynchronizationContext.Current == null
                ? TaskScheduler.Default
                : TaskScheduler.FromCurrentSynchronizationContext());

        _message = new TaskCompletionSource<TRequest>();
        _pipeConfigurator = new PipeConfigurator<SendContext<TRequest>>();
        _sendContext = TaskCompletionSources.Create<SendContext<TRequest>>();
        _readyToSend = TaskCompletionSources.Create<bool>();
        _cancellationTokenSource = new CancellationTokenSource();
        _responseHandlers = new Dictionary<Type, HandlerConnectHandle>();
        _handlerLock = new object();
        _accept = [];

        if (cancellationToken.CanBeCanceled)
            _registration = cancellationToken.Register(Cancel);

        _send = SendRequestAsync();

        HandleFault();
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendAsync(SendContext<TRequest> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        await _readyToSend.Task.ConfigureAwait(false);

        context.RequestId = RequestId;
        context.ResponseAddress = _context.ResponseAddress;

        context.Headers.Set(MessageHeaders.Request.Accept, _accept);

        if (_timeToLive.HasValue)
            context.TimeToLive ??= _timeToLive.Value;

        IPipe<SendContext<TRequest>> pipe = _pipeConfigurator.Build();

        if (pipe.IsNotEmpty())
            await pipe.SendAsync(context).ConfigureAwait(false);

        _timeoutTimer = _context.TimeProvider.CreateTimer(
            TimeoutExpired,
            this,
            _timeout.Value,
            Timeout.InfiniteTimeSpan);

        _sendContext.TrySetResult(context);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        ProbeContext scope = context.CreateFilterScope("request");
        scope.Add("requestId", RequestId);
        scope.Add("requestType", TypeCache<TRequest>.ShortName);
    }

    /// <summary>Gets the request id.</summary>
    public Guid RequestId { get; }

    /// <summary>Gets or sets the time to live.</summary>
    public RequestTimeout TimeToLive
    {
        set => _timeToLive = value;
    }

    /// <summary>Cancels the request and disconnects its response handlers.</summary>
    public void Cancel()
    {
        if (Interlocked.CompareExchange(ref _faultedOrCanceled, 1, 0) != 0)
            return;

        Task.Factory.StartNew(CancelAndDispose, CancellationToken.None, TaskCreationOptions.None, _taskScheduler);
    }

    /// <summary>Adds pipe specification to the configuration.</summary>
    /// <param name="specification">The specification.</param>
    public void AddPipeSpecification(IPipeSpecification<SendContext<TRequest>> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);
        _pipeConfigurator.AddPipeSpecification(specification);
    }

    /// <summary>Gets response.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="readyToSend">The ready to send.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public Task<Response<T>> GetResponseAsync<T>(bool readyToSend, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<Response<T>>(cancellationToken);

        return ResponseAsync<T>(readyToSend);
    }

    /// <summary>Releases the resources owned by this instance.</summary>
    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _faultedOrCanceled, 1, 0) == 0)
            CancelAndDispose();
    }

    /// <summary>Gets the message.</summary>
    public Task<TRequest> Message => _message.Task;

    async Task SendRequestAsync()
    {
        try
        {
            var message = await _sendRequestCallback(RequestId, this, _cancellationTokenSource.Token).ConfigureAwait(false);

            _message.TrySetResult(message);
        }
        catch (RequestException exception)
        {
            Fail(exception);

            throw;
        }
        catch (OperationCanceledException exception)
        {
            if (_sendContext.Task.IsFaulted)
                await _sendContext.Task.ConfigureAwait(false);

            var requestException = new RequestCanceledException(RequestId.ToString("D"), exception, exception.CancellationToken);

            Fail(requestException);

            throw requestException;
        }
        catch (Exception exception)
        {
            Fail(exception);

            throw new RequestException($"An exception occurred while processing the {typeof(TRequest).Name} request", exception);
        }
    }

    Task<Response<T>> ResponseAsync<T>(bool readyToSend)
        where T : class
    {
        lock (_handlerLock)
        {
            if (_faultedOrCanceled != 0 || _cancellationToken.IsCancellationRequested)
                return Task.FromCanceled<Response<T>>(CancellationTokenForCanceledRequest());

            if (_readyToSend.Task.IsCompleted)
                throw new RequestException("Response handlers cannot be registered after the request is ready to send");

            if (typeof(T) == typeof(Fault<TRequest>) || _responseHandlers.ContainsKey(typeof(T)))
                throw new RequestException($"Only one handler of type {TypeCache<T>.ShortName} can be registered");

            var completed = TaskCompletionSources.Create<ConsumeContext<T>>();
            var pipeConfigurator = new PipeConfigurator<ConsumeContext<T>>();

            Task MessageHandlerAsync(ConsumeContext<T> context)
            {
                completed.TrySetResult(context);
                return Task.CompletedTask;
            }

            ConnectHandle connectHandle = _context.ConnectRequestHandler(RequestId, MessageHandlerAsync, pipeConfigurator);
            var handle = new ResponseHandlerConnectHandle<T>(connectHandle, completed, _send);

            _responseHandlers.Add(typeof(T), handle);
            _accept.Add(MessageUrn.ForTypeString<T>());

            if (readyToSend)
                _readyToSend.TrySetResult(true);

            return handle.Task;
        }
    }

    void HandleFault()
    {
        Task MessageHandlerAsync(ConsumeContext<Fault<TRequest>> context)
        {
            return FaultHandlerAsync(context);
        }

        lock (_handlerLock)
        {
            if (_faultedOrCanceled != 0 || _cancellationToken.IsCancellationRequested)
                return;

            _faultHandler = _context.ConnectRequestHandler(
                RequestId,
                MessageHandlerAsync,
                new PipeConfigurator<ConsumeContext<Fault<TRequest>>>());
        }
    }

    Task FaultHandlerAsync(ConsumeContext<Fault<TRequest>> context)
    {
        Fail(context.Message);

        return Task.CompletedTask;
    }

    void Fail(Fault message)
    {
        Fail(new RequestFaultException(TypeCache<TRequest>.ShortName, message));
    }

    void Fail(Exception exception)
    {
        if (Interlocked.CompareExchange(ref _faultedOrCanceled, 1, 0) != 0)
            return;

        void HandleFail()
        {
            _registration.Dispose();

            DisposeTimer();

            _readyToSend.TrySetException(exception);

            var wasSet = _sendContext.TrySetException(exception);

            _message.TrySetException(exception);
            _message.Task.IgnoreUnobservedExceptions();

            DisconnectHandlers(handle => handle.TrySetException(exception));

            if (wasSet)
                _cancellationTokenSource.Cancel();
        }

        Task.Factory.StartNew(HandleFail, CancellationToken.None, TaskCreationOptions.None, _taskScheduler);
    }

    void CancelAndDispose()
    {
        _registration.Dispose();

        DisposeTimer();

        _cancellationTokenSource.Cancel();

        var cancellationToken = _cancellationToken.IsCancellationRequested ? _cancellationToken : _cancellationTokenSource.Token;

        _readyToSend.TrySetCanceled(cancellationToken);

        _sendContext.TrySetCanceled(cancellationToken);

        _message.TrySetCanceled();

        DisconnectHandlers(handle => handle.TrySetCanceled(cancellationToken));
    }

    void TimeoutExpired(object? state)
    {
        var timeoutException = new RequestTimeoutException(RequestId.ToString());

        Fail(timeoutException);
    }

    void DisposeTimer()
    {
        try
        {
            _timeoutTimer?.Dispose();
        }
        catch (ObjectDisposedException)
        {
        }

        _timeoutTimer = null;
    }

    CancellationToken CancellationTokenForCanceledRequest()
    {
        if (_cancellationToken.IsCancellationRequested)
            return _cancellationToken;
        if (_cancellationTokenSource.IsCancellationRequested)
            return _cancellationTokenSource.Token;

        return new CancellationToken(canceled: true);
    }

    void DisconnectHandlers(Action<HandlerConnectHandle> complete)
    {
        HandlerConnectHandle[] responseHandlers;
        ConnectHandle? faultHandler;
        lock (_handlerLock)
        {
            responseHandlers = _responseHandlers.Values.ToArray();
            _responseHandlers.Clear();
            faultHandler = _faultHandler;
            _faultHandler = null;
        }

        foreach (HandlerConnectHandle handle in responseHandlers)
        {
            complete(handle);
            handle.Disconnect();
        }

        faultHandler?.Disconnect();
    }
}
