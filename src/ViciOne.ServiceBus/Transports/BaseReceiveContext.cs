using System;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Provides a base receive context implementation.
/// </summary>
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

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="redelivered">The redelivered value.</param>
    /// <param name="receiveEndpointContext">The receive endpoint context value.</param>
    /// <param name="payloads">The payloads value.</param>
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

    /// <summary>
    /// Gets the header provider value.
    /// </summary>
    protected abstract IHeaderProvider HeaderProvider { get; }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    public virtual void Dispose()
    {
        _cancellationTokenSource.Dispose();
    }

    /// <summary>
    /// Gets the cancellation token value.
    /// </summary>
    public override CancellationToken CancellationToken => _cancellationTokenSource.Token;

    /// <summary>
    /// Gets or sets the is delivered value.
    /// </summary>
    public bool IsDelivered { get; private set; }
    /// <summary>
    /// Gets or sets the is faulted value.
    /// </summary>
    public bool IsFaulted { get; private set; }

    /// <summary>
    /// Gets the publish faults value.
    /// </summary>
    public bool PublishFaults => _receiveEndpointContext.PublishFaults;
    /// <summary>
    /// Gets the body value.
    /// </summary>
    public abstract MessageBody Body { get; }

    /// <summary>
    /// Gets the send endpoint provider value.
    /// </summary>
    public ISendEndpointProvider SendEndpointProvider => _sendEndpointProvider.Value;
    /// <summary>
    /// Gets the publish endpoint provider value.
    /// </summary>
    public IPublishEndpointProvider PublishEndpointProvider => _publishEndpointProvider.Value;

    /// <summary>
    /// Gets the receive completed value.
    /// </summary>
    public Task ReceiveCompleted => _receiveTasks.CompletedAsync(CancellationToken);

    /// <summary>
    /// Adds receive task to the configuration.
    /// </summary>
    /// <param name="task">The task value.</param>
    public void AddReceiveTask(Task task)
    {
        _receiveTasks.Add(task);
    }

    /// <summary>
    /// Gets the redelivered value.
    /// </summary>
    public bool Redelivered { get; }
    /// <summary>
    /// Gets the transport headers value.
    /// </summary>
    public Headers TransportHeaders => _headers.Value;

    /// <summary>
    /// Performs the notify consumed operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="duration">The duration value.</param>
    /// <param name="consumerType">The consumer type value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public virtual Task NotifyConsumedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); IsDelivered = true;

        context.LogConsumed(duration, consumerType);

        return _receiveEndpointContext.ReceiveObservers.PostConsumeAsync(context, duration, consumerType);
    }

    /// <summary>
    /// Performs the notify faulted operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="duration">The duration value.</param>
    /// <param name="consumerType">The consumer type value.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the notify faulted operation.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public virtual Task NotifyFaultedAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); IsFaulted = true;

        this.LogFaulted(exception);

        return _receiveEndpointContext.ReceiveObservers.ReceiveFaultAsync(this, exception);
    }

    /// <summary>
    /// Gets the elapsed time value.
    /// </summary>
    public TimeSpan ElapsedTime => _timeProvider.GetElapsedTime(_receiveStartedAt);
    /// <summary>
    /// Gets or sets the input address value.
    /// </summary>
    public Uri InputAddress { get; protected set; }
    /// <summary>
    /// Gets the content type value.
    /// </summary>
    public ContentType ContentType => _contentType.Value;

    /// <summary>
    /// Gets send endpoint provider.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    protected virtual ISendEndpointProvider GetSendEndpointProvider()
    {
        return _receiveEndpointContext.SendEndpointProvider;
    }

    /// <summary>
    /// Gets publish endpoint provider.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    protected virtual IPublishEndpointProvider GetPublishEndpointProvider()
    {
        return _receiveEndpointContext.PublishEndpointProvider;
    }

    /// <summary>
    /// Gets content type.
    /// </summary>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Determines whether the current value can cel.
    /// </summary>
    public void Cancel()
    {
        _cancellationTokenSource.Cancel();
    }

    /// <summary>
    /// Performs the convert to content type operation.
    /// </summary>
    /// <param name="text">The text value.</param>
    /// <returns>The result of the operation.</returns>
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
