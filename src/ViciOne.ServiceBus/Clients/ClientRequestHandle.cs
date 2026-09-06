using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Clients;

/// <summary>
/// Provides a client request handle implementation.
/// </summary>
/// <typeparam name="TRequest">The t request type.</typeparam>
public class ClientRequestHandle<TRequest> :
    RequestHandle<TRequest>,
    IPipe<SendContext<TRequest>>
    where TRequest : class
{
    /// <summary>
    /// Represents the method that handles send request callback.
    /// </summary>
    /// <param name="requestId">The request id value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public delegate Task<TRequest> SendRequestCallback(Guid requestId, IPipe<SendContext<TRequest>> pipe, CancellationToken cancellationToken);


    readonly List<string> _accept;
    readonly CancellationToken _cancellationToken;
    readonly CancellationTokenSource _cancellationTokenSource;
    readonly ClientFactoryContext _context;
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
    ITimer? _timeoutTimer;
    RequestTimeout _timeToLive;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="sendRequestCallback">The send request callback value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="requestId">The request id value.</param>
    /// <param name="taskScheduler">The task scheduler value.</param>
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
        _accept = [];

        if (cancellationToken.CanBeCanceled)
            _registration = cancellationToken.Register(Cancel);

        _send = SendRequestAsync();

        HandleFault();
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public async Task SendAsync(SendContext<TRequest> context)
    {
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

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
    }

    /// <summary>
    /// Gets the request id value.
    /// </summary>
    public Guid RequestId { get; }

    /// <summary>
    /// Gets or sets the time to live value.
    /// </summary>
    public RequestTimeout TimeToLive
    {
        set => _timeToLive = value;
    }

    /// <summary>
    /// Determines whether the current value can cel.
    /// </summary>
    public void Cancel()
    {
        if (Interlocked.CompareExchange(ref _faultedOrCanceled, 1, 0) != 0)
            return;

        Task.Factory.StartNew(CancelAndDispose, CancellationToken.None, TaskCreationOptions.None, _taskScheduler);
    }

    /// <summary>
    /// Adds pipe specification to the configuration.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    public void AddPipeSpecification(IPipeSpecification<SendContext<TRequest>> specification)
    {
        _pipeConfigurator.AddPipeSpecification(specification);
    }

    /// <summary>
    /// Gets response.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="readyToSend">The ready to send value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<Response<T>> GetResponseAsync<T>(bool readyToSend, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.Response<T>>(cancellationToken); Task<Response<T>> response = ResponseAsync<T>();

        AcceptResponse<T>();

        if (readyToSend)
            _readyToSend.TrySetResult(true);

        return response;
    }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _faultedOrCanceled, 1, 0) == 0)
            CancelAndDispose();
    }

    /// <summary>
    /// Gets the message value.
    /// </summary>
    public Task<TRequest> Message => _message.Task;

    void AcceptResponse<T>()
        where T : class
    {
        _accept.Add(MessageUrn.ForTypeString<T>());
    }

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

    Task<Response<T>> ResponseAsync<T>(MessageHandler<T>? handler = null, Action<IHandlerConfigurator<T>>? configure = null)
        where T : class
    {
        if (_responseHandlers.ContainsKey(typeof(T)))
            throw new RequestException($"Only one handler of type {TypeCache<T>.ShortName} can be registered");

        var configurator = new ResponseHandlerConfigurator<T>(_taskScheduler, handler, _send);

        configure?.Invoke(configurator);

        if (_cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<Response<T>>(_cancellationToken);

        HandlerConnectHandle<T> handle = configurator.Connect(_context, RequestId);

        _responseHandlers.Add(typeof(T), handle);

        return handle.Task;
    }

    void HandleFault()
    {
        if (_cancellationToken.IsCancellationRequested)
            return;

        Task MessageHandlerAsync(ConsumeContext<Fault<TRequest>> context)
        {
            return FaultHandlerAsync(context);
        }

        var connectHandle = _context.ConnectRequestHandler(RequestId, MessageHandlerAsync, new PipeConfigurator<ConsumeContext<Fault<TRequest>>>());

        var handle = new FaultHandlerConnectHandle(connectHandle);

        _responseHandlers.Add(typeof(Fault<TRequest>), handle);
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

            foreach (var handle in _responseHandlers.Values)
            {
                handle.TrySetException(exception);
                handle.Disconnect();
            }

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

        foreach (var handle in _responseHandlers.Values)
        {
            handle.TrySetCanceled(cancellationToken);
            handle.Disconnect();
        }
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
}
