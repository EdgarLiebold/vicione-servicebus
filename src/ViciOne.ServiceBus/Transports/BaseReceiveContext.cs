using System;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Transports;

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

    protected abstract IHeaderProvider HeaderProvider { get; }

    public virtual void Dispose()
    {
        _cancellationTokenSource.Dispose();
    }

    public override CancellationToken CancellationToken => _cancellationTokenSource.Token;

    public bool IsDelivered { get; private set; }
    public bool IsFaulted { get; private set; }

    public bool PublishFaults => _receiveEndpointContext.PublishFaults;
    public abstract MessageBody Body { get; }

    public ISendEndpointProvider SendEndpointProvider => _sendEndpointProvider.Value;
    public IPublishEndpointProvider PublishEndpointProvider => _publishEndpointProvider.Value;

    public Task ReceiveCompleted => _receiveTasks.CompletedAsync(CancellationToken);

    public void AddReceiveTask(Task task)
    {
        _receiveTasks.Add(task);
    }

    public bool Redelivered { get; }
    public Headers TransportHeaders => _headers.Value;

    public virtual Task NotifyConsumedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); IsDelivered = true;

        context.LogConsumed(duration, consumerType);

        return _receiveEndpointContext.ReceiveObservers.PostConsumeAsync(context, duration, consumerType);
    }

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

    public virtual Task NotifyFaultedAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); IsFaulted = true;

        this.LogFaulted(exception);

        return _receiveEndpointContext.ReceiveObservers.ReceiveFaultAsync(this, exception);
    }

    public TimeSpan ElapsedTime => _timeProvider.GetElapsedTime(_receiveStartedAt);
    public Uri InputAddress { get; protected set; }
    public ContentType ContentType => _contentType.Value;

    protected virtual ISendEndpointProvider GetSendEndpointProvider()
    {
        return _receiveEndpointContext.SendEndpointProvider;
    }

    protected virtual IPublishEndpointProvider GetPublishEndpointProvider()
    {
        return _receiveEndpointContext.PublishEndpointProvider;
    }

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

    public void Cancel()
    {
        _cancellationTokenSource.Cancel();
    }

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
