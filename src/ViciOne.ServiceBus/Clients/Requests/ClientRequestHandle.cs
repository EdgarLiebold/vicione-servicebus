using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Clients.Requests;

/// <summary>Coordinates sending one request and awaiting one of its registered response contracts.</summary>
/// <typeparam name="TRequest">The request message contract.</typeparam>
internal sealed partial class ClientRequestHandle<TRequest> :
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
    readonly DateTimeOffset? _deadline;
    readonly object _handlerLock;
    readonly TaskCompletionSource<TRequest> _message;
    readonly IBuildPipeConfigurator<SendContext<TRequest>> _pipeConfigurator;
    readonly TaskCompletionSource<bool> _readyToSend;
    readonly CancellationTokenRegistration _registration;
    readonly Dictionary<Type, HandlerConnectHandle> _responseHandlers;
    readonly Task _send;
    readonly TaskCompletionSource<SendContext<TRequest>> _sendContext;
    readonly SendRequestCallback _sendRequestCallback;
    readonly TaskCompletionSource _terminalCleanupCompleted;
    readonly TaskCompletionSource _terminalRequestFailure;
    readonly RequestTimeout _timeout;
    readonly bool _useDeadlineAsTimeToLive;
    int _faultedOrCanceled;
    Exception? _responseFailure;
    bool _responseCompleted;
    ConnectHandle? _faultHandler;
    ITimer? _timeoutTimer;
    RequestTimeout _timeToLive;

    /// <summary>Creates a request handle and starts its send pipeline behind the response-registration gate.</summary>
    /// <param name="context">The provider context used for response connections, addressing, and time.</param>
    /// <param name="sendRequestCallback">The operation that sends the request after handlers are registered.</param>
    /// <param name="cancellationToken">Cancels sending and response waiting.</param>
    /// <param name="timeout">The maximum response-wait duration.</param>
    /// <param name="requestId">An optional request identifier; a new identifier is generated when omitted.</param>
    /// <param name="deadline">The optional absolute response deadline.</param>
    /// <param name="useDeadlineAsTimeToLive">Whether the remaining deadline also limits transport lifetime.</param>
    public ClientRequestHandle(ClientFactoryContext context, SendRequestCallback sendRequestCallback, CancellationToken cancellationToken = default,
        RequestTimeout timeout = default, Guid? requestId = null, DateTimeOffset? deadline = null, bool useDeadlineAsTimeToLive = false)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(sendRequestCallback);
        if (useDeadlineAsTimeToLive && deadline is null)
            throw new ArgumentException("A transport lifetime derived from a deadline requires an absolute deadline.", nameof(useDeadlineAsTimeToLive));

        _context = context;
        _sendRequestCallback = sendRequestCallback;
        _cancellationToken = cancellationToken;
        _deadline = deadline;
        _useDeadlineAsTimeToLive = useDeadlineAsTimeToLive;
        _timeout = timeout.HasValue ? timeout : _context.DefaultTimeout.HasValue ? _context.DefaultTimeout : RequestTimeout.Default;
        _timeToLive = _timeout;

        RequestId = requestId ?? NewId.NextGuid();

        _message = TaskCompletionSources.Create<TRequest>();
        _pipeConfigurator = new PipeConfigurator<SendContext<TRequest>>();
        _sendContext = TaskCompletionSources.Create<SendContext<TRequest>>();
        _readyToSend = TaskCompletionSources.Create<bool>();
        _cancellationTokenSource = new CancellationTokenSource();
        _requestSendCancellationToken = _cancellationTokenSource.Token;
        _responseHandlers = new Dictionary<Type, HandlerConnectHandle>();
        _handlerLock = new object();
        _terminalCleanupCompleted = TaskCompletionSources.Create();
        _terminalRequestFailure = TaskCompletionSources.Create();
        _terminalRequestFailure.Task.IgnoreUnobservedExceptions();
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

        try
        {
            if (_deadline is { } absoluteDeadline)
                StartTimeoutTimer(absoluteDeadline - _context.TimeProvider.GetUtcNow());

            ConnectFaultHandler();
        }
        catch
        {
            DisposeTimer();
            _registration.Dispose();
            _cancellationTokenSource.Dispose();
            throw;
        }

        _send = Volatile.Read(ref _faultedOrCanceled) == 0
            ? SendRequestAsync()
            : _readyToSend.Task;
        _send.IgnoreUnobservedExceptions();
        DisposeCancellationTokenSourceAfterTerminalCleanupAsync().IgnoreUnobservedExceptions();
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

        if (!_useDeadlineAsTimeToLive && _timeToLive.HasValue)
            context.TimeToLive ??= _timeToLive.Value;

        IPipe<SendContext<TRequest>> pipe = _pipeConfigurator.Build();

        if (pipe.IsNotEmpty())
            await pipe.SendAsync(context).ConfigureAwait(false);

        if (_deadline is { } deadline)
        {
            TimeSpan responseTimeout = deadline - _context.TimeProvider.GetUtcNow();
            if (responseTimeout <= TimeSpan.Zero)
                throw new RequestTimeoutException(RequestId);

            if (_useDeadlineAsTimeToLive)
                context.TimeToLive = responseTimeout;
        }
        else
            StartTimeoutTimer(_timeout.Value);

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
        lock (_handlerLock)
        {
            if (_faultedOrCanceled != 0)
                return;

            _faultedOrCanceled = 1;
        }

        CancelRequestSend();
        CancellationToken cancellationToken = CancellationTokenForCanceledRequest();
        CompleteCancellationSignals(cancellationToken);

        Task.Factory.StartNew(CancelAndDispose, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
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
        lock (_handlerLock)
        {
            if (_faultedOrCanceled != 0)
                return;

            _faultedOrCanceled = 1;
        }

        CancelAndDispose();
    }

    /// <summary>Gets the request message produced by the send callback.</summary>
    public Task<TRequest> Message => _message.Task;

    void StartTimeoutTimer(TimeSpan timeout)
    {
        ITimer timeoutTimer = _context.TimeProvider.CreateTimer(
            TimeoutExpired,
            this,
            timeout <= TimeSpan.Zero ? TimeSpan.Zero : timeout,
            Timeout.InfiniteTimeSpan)
            ?? throw new InvalidOperationException("The request time provider returned no timeout timer.");

        if (Interlocked.CompareExchange(ref _timeoutTimer, timeoutTimer, null) is not null)
        {
            DisposeTimerSafely(timeoutTimer);
            throw new InvalidOperationException("The request timeout timer was initialized more than once.");
        }

        if (Volatile.Read(ref _faultedOrCanceled) != 0)
            DisposeTimer();
    }
}
