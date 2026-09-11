using System;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Owns message metadata, cancellation, serialization, and completion tasks for one delivery.</summary>
public abstract class BaseReceiveContext :
    ScopePipeContext,
    ReceiveContext,
    IDisposable
{
    readonly CancellationTokenSource _cancellationTokenSource;
    readonly Lazy<ContentType> _contentType;
    readonly Lazy<Headers> _headers;
    readonly Lazy<IPublishEndpointProvider> _publishEndpointProvider;
    readonly ReceiveEndpointContext _receiveEndpointContext;
    readonly PendingTaskCollection _receiveTasks;
    readonly long _receiveStartedAt;
    readonly Lazy<ISendEndpointProvider> _sendEndpointProvider;
    readonly TimeProvider _timeProvider;
    int _isDelivered;
    int _isFaulted;
    MessageBody? _validatedBody;

    /// <summary>Initializes a delivery from its endpoint context and transport payloads.</summary>
    /// <param name="redelivered">Whether the transport identifies this as a redelivery.</param>
    /// <param name="receiveEndpointContext">The endpoint that owns the delivery.</param>
    /// <param name="payloads">Transport-specific payloads available to the receive pipeline.</param>
    protected BaseReceiveContext(bool redelivered, ReceiveEndpointContext receiveEndpointContext, params object[] payloads)
        : base(receiveEndpointContext ?? throw new ArgumentNullException(nameof(receiveEndpointContext)),
            payloads ?? throw new ArgumentNullException(nameof(payloads)))
    {
        _timeProvider = receiveEndpointContext.GetTimeProvider();
        _receiveStartedAt = _timeProvider.GetTimestamp();

        _cancellationTokenSource = new CancellationTokenSource();
        _receiveEndpointContext = receiveEndpointContext;

        InputAddress = receiveEndpointContext.InputAddress
            ?? throw new InvalidOperationException("The receive endpoint context returned no input address.");
        Redelivered = redelivered;

        _headers = new Lazy<Headers>(() => new JsonTransportHeaders(HeaderProvider
            ?? throw new InvalidOperationException("The receive context returned no header provider.")));

        _contentType = new Lazy<ContentType>(GetContentType);
        _receiveTasks = new PendingTaskCollection(4);

        _sendEndpointProvider = new Lazy<ISendEndpointProvider>(() => GetSendEndpointProvider()
            ?? throw new InvalidOperationException("The receive context returned no send endpoint provider."));
        _publishEndpointProvider = new Lazy<IPublishEndpointProvider>(() => GetPublishEndpointProvider()
            ?? throw new InvalidOperationException("The receive context returned no publish endpoint provider."));
    }

    /// <summary>Gets the transport-specific source of message headers.</summary>
    protected abstract IHeaderProvider HeaderProvider { get; }

    /// <summary>Applies the bus-owned receive limit before a transport body can be read by a deserializer.</summary>
    /// <param name="body">The transport body being exposed to the receive pipeline.</param>
    /// <returns>The same body after successful admission.</returns>
    protected MessageBody EnforceMessageLimits(MessageBody body)
    {
        ArgumentNullException.ThrowIfNull(body);
        if (ReferenceEquals(Volatile.Read(ref _validatedBody), body))
            return body;

        if (TryGetPayload(out MessageLimits? limits)
            && body.Length is { } actualBytes
            && actualBytes > limits.MaxEnvelopeBytes)
        {
            throw new MessageTooLargeException(actualBytes, limits.MaxEnvelopeBytes, InputAddress);
        }

        Interlocked.CompareExchange(ref _validatedBody, body, null);
        return body;
    }

    /// <summary>Releases the delivery cancellation source.</summary>
    public virtual void Dispose()
    {
        _cancellationTokenSource.Dispose();
    }

    /// <summary>Gets the token canceled when processing of this delivery must stop.</summary>
    public override CancellationToken CancellationToken => _cancellationTokenSource.Token;

    /// <summary>Gets whether a consumer completed this delivery successfully.</summary>
    public bool IsDelivered => Volatile.Read(ref _isDelivered) != 0;
    /// <summary>Gets whether receive or consume processing faulted.</summary>
    public bool IsFaulted => Volatile.Read(ref _isFaulted) != 0;

    /// <summary>Gets whether unaddressed consumer faults are published.</summary>
    public bool PublishFaults => _receiveEndpointContext.PublishFaults;
    /// <summary>Gets the serialized transport body.</summary>
    public abstract MessageBody Body { get; }

    /// <summary>Gets the endpoint provider used for sends initiated by this delivery.</summary>
    public ISendEndpointProvider SendEndpointProvider => _sendEndpointProvider.Value;
    /// <summary>Gets the endpoint provider used for publishes initiated by this delivery.</summary>
    public IPublishEndpointProvider PublishEndpointProvider => _publishEndpointProvider.Value;

    /// <summary>Gets a task that completes after all delivery-owned asynchronous work has completed.</summary>
    public Task ReceiveCompleted => _receiveTasks.CompletedAsync(CancellationToken);

    /// <summary>Adds asynchronous work that must complete before the delivery can be settled.</summary>
    /// <param name="task">The delivery-owned task to await.</param>
    public void AddReceiveTask(Task task)
    {
        ArgumentNullException.ThrowIfNull(task);
        _receiveTasks.Add(task);
    }

    /// <summary>Gets whether the transport identified this delivery as a redelivery.</summary>
    public bool Redelivered { get; }
    /// <summary>Gets the transport headers.</summary>
    public Headers TransportHeaders => _headers.Value;

    /// <summary>Records a successful consume operation and notifies receive observers.</summary>
    /// <typeparam name="T">The consumed message contract.</typeparam>
    /// <param name="context">The completed consume context.</param>
    /// <param name="duration">The elapsed consumer execution time.</param>
    /// <param name="consumerType">The runtime consumer type that handled the message.</param>
    /// <param name="cancellationToken">The token that cancels notification.</param>
    /// <returns>A task that completes after receive observers have been notified.</returns>
    public virtual Task NotifyConsumedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerType);

        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        Interlocked.Exchange(ref _isDelivered, 1);

        context.LogConsumed(duration, consumerType);

        return _receiveEndpointContext.ReceiveObservers.PostConsumeAsync(context, duration, consumerType)
            ?? throw new InvalidOperationException("The receive observer returned no post-consume task.");
    }

    /// <summary>Records a failed consume operation and notifies receive observers.</summary>
    /// <typeparam name="T">The consumed message contract.</typeparam>
    /// <param name="context">The failed consume context.</param>
    /// <param name="duration">The elapsed consumer execution time.</param>
    /// <param name="consumerType">The runtime consumer type whose delivery failed.</param>
    /// <param name="exception">The consumer failure.</param>
    /// <param name="cancellationToken">The token that cancels notification.</param>
    /// <returns>A task that completes after receive observers have been notified.</returns>
    public virtual Task NotifyFaultedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerType);
        ArgumentNullException.ThrowIfNull(exception);

        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        Interlocked.Exchange(ref _isFaulted, 1);

        switch (exception)
        {
            case OperationCanceledException canceled when canceled.CancellationToken == context.CancellationToken:
                context.LogCanceled(duration, consumerType);
                break;

            default:
                context.LogFaulted(duration, consumerType, exception);
                break;
        }

        GetOrAddPayload<ConsumerFaultContext>(() => new FaultContext(TypeCache<T>.ShortName, consumerType));

        return _receiveEndpointContext.ReceiveObservers.ConsumeFaultAsync(context, duration, consumerType, exception)
            ?? throw new InvalidOperationException("The receive observer returned no consume-fault task.");
    }

    /// <summary>Records a receive-pipeline failure and notifies receive observers.</summary>
    /// <param name="exception">The receive-pipeline failure.</param>
    /// <param name="cancellationToken">The token that cancels notification.</param>
    /// <returns>A task that completes after receive observers have been notified.</returns>
    public virtual Task NotifyFaultedAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        Interlocked.Exchange(ref _isFaulted, 1);

        this.LogFaulted(exception);

        return _receiveEndpointContext.ReceiveObservers.ReceiveFaultAsync(this, exception)
            ?? throw new InvalidOperationException("The receive observer returned no receive-fault task.");
    }

    /// <summary>Gets the elapsed time since the delivery context was created.</summary>
    public TimeSpan ElapsedTime => _timeProvider.GetElapsedTime(_receiveStartedAt);
    /// <summary>Gets the address from which the message was received.</summary>
    public Uri InputAddress { get; protected set; }
    /// <summary>Gets the parsed content type supplied by transport headers or endpoint serialization.</summary>
    public ContentType ContentType => _contentType.Value;

    /// <summary>Resolves the endpoint provider used for sends initiated by this delivery.</summary>
    /// <returns>The receive endpoint's send endpoint provider.</returns>
    protected virtual ISendEndpointProvider GetSendEndpointProvider()
    {
        return _receiveEndpointContext.SendEndpointProvider;
    }

    /// <summary>Resolves the endpoint provider used for publishes initiated by this delivery.</summary>
    /// <returns>The receive endpoint's publish endpoint provider.</returns>
    protected virtual IPublishEndpointProvider GetPublishEndpointProvider()
    {
        return _receiveEndpointContext.PublishEndpointProvider;
    }

    /// <summary>Resolves the message content type from transport headers and serialization defaults.</summary>
    /// <returns>The effective message content type.</returns>
    protected virtual ContentType GetContentType()
    {
        if (_headers.Value.TryGetHeader("Content-Type", out var contentTypeHeader) || _headers.Value.TryGetHeader("ContentType", out contentTypeHeader))
        {
            if (contentTypeHeader is ContentType contentType)
                return contentType;

            if (contentTypeHeader is string contentTypeString && ConvertToContentType(contentTypeString) is { } parsedContentType)
                return parsedContentType;
        }

        return _receiveEndpointContext.Serialization.DefaultContentType;
    }

    /// <summary>Cancels processing associated with this delivery.</summary>
    public void Cancel()
    {
        _cancellationTokenSource.Cancel();
    }

    /// <summary>Parses a MIME content type without propagating invalid transport metadata.</summary>
    /// <param name="text">The content-type header value.</param>
    /// <returns>The parsed content type, or <see langword="null" /> when the value is invalid.</returns>
    protected static ContentType? ConvertToContentType(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        try
        {
            return new ContentType(text);
        }
        catch (FormatException)
        {
            return default;
        }
    }


    sealed class FaultContext :
        ConsumerFaultContext
    {
        public FaultContext(string messageType, string consumerType)
        {
            MessageType = messageType;
            ConsumerType = consumerType;
        }

        public string MessageType { get; }
        public string ConsumerType { get; }
    }
}
