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
/// <typeparam name="TRequest">The request message contract.</typeparam>
internal sealed class ClientRequestHandle<TRequest> :
    RequestHandle<TRequest>,
    IPipe<SendContext<TRequest>>
    where TRequest : class
{
    /// <summary>Sends the request after its response contracts have been registered.</summary>
    /// <param name="requestId">The identifier used to correlate the response.</param>
    /// <param name="pipe">The request send-context pipeline.</param>
    /// <param name="cancellationToken">Cancels the request send.</param>
    /// <returns>A task containing the request message accepted by the send path.</returns>
    public delegate Task<TRequest> SendRequestCallback(Guid requestId, IPipe<SendContext<TRequest>> pipe, CancellationToken cancellationToken);


    readonly List<string> _accept;
    readonly CancellationToken _cancellationToken;
    readonly CancellationTokenSource _cancellationTokenSource;
    readonly CancellationToken _requestSendCancellationToken;
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
    readonly TaskCompletionSource _terminalCleanupCompleted;
    readonly RequestTimeout _timeout;
    int _faultedOrCanceled;
    ConnectHandle? _faultHandler;
    ITimer? _timeoutTimer;
    RequestTimeout _timeToLive;

    /// <summary>Creates a request handle and starts its send pipeline behind the response-registration gate.</summary>
    /// <param name="context">The provider context used for response connections, addressing, and time.</param>
    /// <param name="sendRequestCallback">The operation that sends the request after handlers are registered.</param>
    /// <param name="cancellationToken">Cancels sending and response waiting.</param>
    /// <param name="timeout">The maximum response-wait duration.</param>
    /// <param name="requestId">An optional request identifier; a new identifier is generated when omitted.</param>
    /// <param name="taskScheduler">The scheduler used for terminal cleanup callbacks.</param>
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

        _message = TaskCompletionSources.Create<TRequest>();
        _pipeConfigurator = new PipeConfigurator<SendContext<TRequest>>();
        _sendContext = TaskCompletionSources.Create<SendContext<TRequest>>();
        _readyToSend = TaskCompletionSources.Create<bool>();
        _cancellationTokenSource = new CancellationTokenSource();
        _requestSendCancellationToken = _cancellationTokenSource.Token;
        _responseHandlers = new Dictionary<Type, HandlerConnectHandle>();
        _handlerLock = new object();
        _terminalCleanupCompleted = TaskCompletionSources.Create();
        _accept = [];

        if (cancellationToken.IsCancellationRequested)
        {
            _faultedOrCanceled = 1;
            _cancellationTokenSource.Cancel();
            _readyToSend.TrySetCanceled(cancellationToken);
            _sendContext.TrySetCanceled(cancellationToken);
            _message.TrySetCanceled(cancellationToken);
            _cancellationTokenSource.Dispose();
            _send = Task.FromCanceled(cancellationToken);
            return;
        }

        if (cancellationToken.CanBeCanceled)
            _registration = cancellationToken.Register(Cancel);

        _send = SendRequestAsync();
        _send.IgnoreUnobservedExceptions();
        DisposeCancellationTokenSourceAfterTerminalCleanupAsync().IgnoreUnobservedExceptions();

        HandleFault();
    }

    /// <summary>Applies request metadata, the configured send pipeline, and the response timeout to an outgoing context.</summary>
    /// <param name="context">The outgoing request context.</param>
    /// <returns>A task that completes when the request context is ready for transport.</returns>
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

        ITimer timeoutTimer = _context.TimeProvider.CreateTimer(
            TimeoutExpired,
            this,
            _timeout.Value,
            Timeout.InfiniteTimeSpan)
            ?? throw new InvalidOperationException("The request time provider returned no timeout timer.");

        if (Interlocked.CompareExchange(ref _timeoutTimer, timeoutTimer, null) is not null)
        {
            DisposeTimerSafely(timeoutTimer);
            throw new InvalidOperationException("The request timeout timer was initialized more than once.");
        }

        if (Volatile.Read(ref _faultedOrCanceled) != 0)
            DisposeTimer();

        _sendContext.TrySetResult(context);
    }

    /// <summary>Adds request identity and contract information to a diagnostic probe.</summary>
    /// <param name="context">The probe to populate.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        ProbeContext scope = context.CreateFilterScope("request");
        scope.Add("requestId", RequestId);
        scope.Add("requestType", TypeCache<TRequest>.ShortName);
    }

    /// <summary>Gets the identifier used to correlate responses.</summary>
    public Guid RequestId { get; }

    /// <summary>Sets the transport time-to-live independently of the client response deadline.</summary>
    public RequestTimeout TimeToLive
    {
        set => _timeToLive = value;
    }

    /// <summary>Cancels the request and disconnects its response handlers.</summary>
    public void Cancel()
    {
        if (Interlocked.CompareExchange(ref _faultedOrCanceled, 1, 0) != 0)
            return;

        CancelRequestSend();
        CancellationToken cancellationToken = CancellationTokenForCanceledRequest();
        CompleteCancellationSignals(cancellationToken);

        Task.Factory.StartNew(CancelAndDispose, CancellationToken.None, TaskCreationOptions.None, _taskScheduler);
    }

    /// <summary>Adds a send-context specification applied before the request reaches the transport.</summary>
    /// <param name="specification">The pipeline specification to add.</param>
    public void AddPipeSpecification(IPipeSpecification<SendContext<TRequest>> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);
        _pipeConfigurator.AddPipeSpecification(specification);
    }

    /// <summary>Registers one response contract and optionally releases the request for sending.</summary>
    /// <typeparam name="T">The response message contract.</typeparam>
    /// <param name="readyToSend">Whether this registration completes the response set and releases the send gate.</param>
    /// <param name="cancellationToken">Cancels waiting for this response.</param>
    /// <returns>A task containing the matching response.</returns>
    public Task<Response<T>> GetResponseAsync<T>(bool readyToSend, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<Response<T>>(cancellationToken);

        Task<Response<T>> response = ResponseAsync<T>(readyToSend);
        return cancellationToken.CanBeCanceled
            ? response.WaitAsync(cancellationToken)
            : response;
    }

    /// <summary>Cancels the request and releases all response-handler connections.</summary>
    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _faultedOrCanceled, 1, 0) == 0)
            CancelAndDispose();
    }

    /// <summary>Gets the request message produced by the send callback.</summary>
    public Task<TRequest> Message => _message.Task;

    async Task SendRequestAsync()
    {
        try
        {
            var message = await _sendRequestCallback(RequestId, this, _requestSendCancellationToken).ConfigureAwait(false);

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
            var requestException = new RequestException(
                $"An exception occurred while processing the {typeof(TRequest).Name} request",
                exception);

            Fail(requestException, exception);

            throw requestException;
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

    void Fail(Exception responseException, Exception? messageException = null)
    {
        if (Interlocked.CompareExchange(ref _faultedOrCanceled, 1, 0) != 0)
            return;

        void HandleFail()
        {
            try
            {
                DisposeRegistration();

                DisposeTimer();

                _readyToSend.TrySetException(responseException);

                var wasSet = _sendContext.TrySetException(responseException);

                _message.TrySetException(messageException ?? responseException);
                _message.Task.IgnoreUnobservedExceptions();

                DisconnectHandlers(handle => handle.TrySetException(responseException));

                if (wasSet)
                    CancelRequestSend();
            }
            finally
            {
                _terminalCleanupCompleted.TrySetResult();
            }
        }

        Task.Factory.StartNew(HandleFail, CancellationToken.None, TaskCreationOptions.None, _taskScheduler);
    }

    void CancelAndDispose()
    {
        try
        {
            DisposeRegistration();

            DisposeTimer();

            CancelRequestSend();

            var cancellationToken = _cancellationToken.IsCancellationRequested ? _cancellationToken : _requestSendCancellationToken;

            CompleteCancellationSignals(cancellationToken);

            DisconnectHandlers(handle => handle.TrySetCanceled(cancellationToken));
        }
        finally
        {
            _terminalCleanupCompleted.TrySetResult();
        }
    }

    void CompleteCancellationSignals(CancellationToken cancellationToken)
    {
        _readyToSend.TrySetCanceled(cancellationToken);
        _sendContext.TrySetCanceled(cancellationToken);
        _message.TrySetCanceled(cancellationToken);

        HandlerConnectHandle[] responseHandlers;
        lock (_handlerLock)
            responseHandlers = _responseHandlers.Values.ToArray();

        foreach (HandlerConnectHandle handle in responseHandlers)
            TryCleanup(() => handle.TrySetCanceled(cancellationToken), "Completing a canceled request response handler faulted");
    }

    void TimeoutExpired(object? state)
    {
        var timeoutException = new RequestTimeoutException(RequestId.ToString());

        Fail(timeoutException);
    }

    void DisposeTimer()
    {
        ITimer? timer = Interlocked.Exchange(ref _timeoutTimer, null);
        DisposeTimerSafely(timer);
    }

    CancellationToken CancellationTokenForCanceledRequest()
    {
        if (_cancellationToken.IsCancellationRequested)
            return _cancellationToken;
        if (_requestSendCancellationToken.IsCancellationRequested)
            return _requestSendCancellationToken;

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
            TryCleanup(() => complete(handle), "Completing a request response handler faulted");
            TryCleanup(handle.Disconnect, "Disconnecting a request response handler faulted");
        }

        if (faultHandler is not null)
            TryCleanup(faultHandler.Disconnect, "Disconnecting the request fault handler faulted");
    }

    void CancelRequestSend()
    {
        TryCleanup(_cancellationTokenSource.Cancel, "Canceling the request send faulted");
    }

    async Task DisposeCancellationTokenSourceAfterTerminalCleanupAsync()
    {
        await _terminalCleanupCompleted.Task.ConfigureAwait(false);

        try
        {
            await _send.ConfigureAwait(false);
        }
        catch
        {
            // Send failures are propagated through the request completion tasks.
        }

        _cancellationTokenSource.Dispose();
    }

    void DisposeRegistration()
    {
        TryCleanup(_registration.Dispose, "Disposing the request cancellation registration faulted");
    }

    static void DisposeTimerSafely(ITimer? timer)
    {
        if (timer is null)
            return;

        TryCleanup(timer.Dispose, "Disposing the request timeout timer faulted");
    }

    static void TryCleanup(Action cleanup, string message)
    {
        try
        {
            cleanup();
        }
        catch (Exception exception)
        {
            try
            {
                LogContext.Warning?.Log(exception, message);
            }
            catch
            {
                // Diagnostic logging cannot change request completion or cleanup outcomes.
            }
        }
    }
}
