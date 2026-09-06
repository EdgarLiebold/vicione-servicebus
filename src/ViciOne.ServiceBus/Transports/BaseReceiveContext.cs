using System;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Carries state for base receive operations.</summary>
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
    MessageBody? _validatedBody;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="redelivered">The redelivered.</param>
    /// <param name="receiveEndpointContext">The receive endpoint context.</param>
    /// <param name="payloads">The payloads.</param>
    protected BaseReceiveContext(bool redelivered, ReceiveEndpointContext receiveEndpointContext, params object[] payloads)
        : base(receiveEndpointContext, payloads)
    {
        _timeProvider = receiveEndpointContext.GetTimeProvider();
        _receiveStartedAt = _timeProvider.GetTimestamp();

        _cancellationTokenSource = new CancellationTokenSource();
        _receiveEndpointContext = receiveEndpointContext;

        InputAddress = receiveEndpointContext.InputAddress;
        Redelivered = redelivered;

        _headers = new Lazy<Headers>(() => new JsonTransportHeaders(HeaderProvider));

        _contentType = new Lazy<ContentType>(GetContentType);
        _receiveTasks = new PendingTaskCollection(4);

        _sendEndpointProvider = new Lazy<ISendEndpointProvider>(GetSendEndpointProvider);
        _publishEndpointProvider = new Lazy<IPublishEndpointProvider>(GetPublishEndpointProvider);
    }

    /// <summary>Gets the header provider.</summary>
    protected abstract IHeaderProvider HeaderProvider { get; }

    /// <summary>Applies the bus-owned receive limit before a transport body can be read by a deserializer.</summary>
    /// <param name="body">The body.</param>
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

    /// <summary>Releases the resources owned by this instance.</summary>
    public virtual void Dispose()
    {
        _cancellationTokenSource.Dispose();
    }

    /// <summary>Gets the cancellation token.</summary>
    public override CancellationToken CancellationToken => _cancellationTokenSource.Token;

    /// <summary>Gets or sets a value indicating whether delivered.</summary>
    public bool IsDelivered { get; private set; }
    /// <summary>Gets or sets a value indicating whether faulted.</summary>
    public bool IsFaulted { get; private set; }

    /// <summary>Gets the publish faults.</summary>
    public bool PublishFaults => _receiveEndpointContext.PublishFaults;
    /// <summary>Gets the body.</summary>
    public abstract MessageBody Body { get; }

    /// <summary>Gets the send endpoint provider.</summary>
    public ISendEndpointProvider SendEndpointProvider => _sendEndpointProvider.Value;
    /// <summary>Gets the publish endpoint provider.</summary>
    public IPublishEndpointProvider PublishEndpointProvider => _publishEndpointProvider.Value;

    /// <summary>Gets the receive completed.</summary>
    public Task ReceiveCompleted => _receiveTasks.CompletedAsync(CancellationToken);

    /// <summary>Adds receive task to the configuration.</summary>
    /// <param name="task">The task.</param>
    public void AddReceiveTask(Task task)
    {
        _receiveTasks.Add(task);
    }

    /// <summary>Gets the redelivered.</summary>
    public bool Redelivered { get; }
    /// <summary>Gets the transport headers.</summary>
    public Headers TransportHeaders => _headers.Value;

    /// <summary>Reports that notify has been consumed.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual Task NotifyConsumedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); IsDelivered = true;

        context.LogConsumed(duration, consumerType);

        return _receiveEndpointContext.ReceiveObservers.PostConsumeAsync(context, duration, consumerType);
    }

    /// <summary>Reports that notify has faulted.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual Task NotifyFaultedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); IsFaulted = true;

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

        return _receiveEndpointContext.ReceiveObservers.ConsumeFaultAsync(context, duration, consumerType, exception);
    }

    /// <summary>Reports that notify has faulted.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual Task NotifyFaultedAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); IsFaulted = true;

        this.LogFaulted(exception);

        return _receiveEndpointContext.ReceiveObservers.ReceiveFaultAsync(this, exception);
    }

    /// <summary>Gets the elapsed time.</summary>
    public TimeSpan ElapsedTime => _timeProvider.GetElapsedTime(_receiveStartedAt);
    /// <summary>Gets or sets the input address.</summary>
    public Uri InputAddress { get; protected set; }
    /// <summary>Gets the content type.</summary>
    public ContentType ContentType => _contentType.Value;

    /// <summary>Gets send endpoint provider.</summary>
    /// <returns>The send endpoint provider.</returns>
    protected virtual ISendEndpointProvider GetSendEndpointProvider()
    {
        return _receiveEndpointContext.SendEndpointProvider;
    }

    /// <summary>Gets publish endpoint provider.</summary>
    /// <returns>The publish endpoint provider.</returns>
    protected virtual IPublishEndpointProvider GetPublishEndpointProvider()
    {
        return _receiveEndpointContext.PublishEndpointProvider;
    }

    /// <summary>Gets content type.</summary>
    /// <returns>The content type.</returns>
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

    /// <summary>Determines whether the current value can cel.</summary>
    public void Cancel()
    {
        _cancellationTokenSource.Cancel();
    }

    /// <summary>Converts to content type.</summary>
    /// <param name="text">The text.</param>
    /// <returns>The converted to content type.</returns>
    protected static ContentType? ConvertToContentType(string text)
    {
        try
        {
            return new ContentType(text);
        }
        catch (FormatException)
        {
            return default;
        }
    }


    class FaultContext :
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
