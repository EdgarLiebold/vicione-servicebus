using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Context;

/// <summary>Represents the absence of an active consume scope with a consistent diagnostic failure.</summary>
public sealed class MissingConsumeContext :
    ConsumeContext
{
    private MissingConsumeContext()
    {
    }

    /// <summary>Gets the shared unavailable consume context.</summary>
    public static ConsumeContext Instance { get; } = new MissingConsumeContext();

    /// <inheritdoc />
    public CancellationToken CancellationToken => throw Unavailable();

    /// <inheritdoc />
    public Guid? MessageId => throw Unavailable();

    /// <inheritdoc />
    public Guid? RequestId => throw Unavailable();

    /// <inheritdoc />
    public Guid? CorrelationId => throw Unavailable();

    /// <inheritdoc />
    public Guid? ConversationId => throw Unavailable();

    /// <inheritdoc />
    public Guid? InitiatorId => throw Unavailable();

    /// <inheritdoc />
    public DateTimeOffset? ExpirationTime => throw Unavailable();

    /// <inheritdoc />
    public Uri? SourceAddress => throw Unavailable();

    /// <inheritdoc />
    public Uri? DestinationAddress => throw Unavailable();

    /// <inheritdoc />
    public Uri? ResponseAddress => throw Unavailable();

    /// <inheritdoc />
    public Uri? FaultAddress => throw Unavailable();

    /// <inheritdoc />
    public DateTimeOffset? SentTime => throw Unavailable();

    /// <inheritdoc />
    public Headers Headers => throw Unavailable();

    /// <inheritdoc />
    public HostInfo Host => throw Unavailable();

    /// <inheritdoc />
    public ReceiveContext ReceiveContext => throw Unavailable();

    /// <inheritdoc />
    public SerializerContext SerializerContext => throw Unavailable();

    /// <inheritdoc />
    public Task ConsumeCompleted => throw Unavailable();

    /// <inheritdoc />
    public IOutgoingMessages Outgoing => throw Unavailable();

    /// <inheritdoc />
    public IEnumerable<string> SupportedMessageTypes => throw Unavailable();

    /// <inheritdoc />
    public bool HasPayloadType(Type payloadType) => throw Unavailable();

    /// <inheritdoc />
    public bool TryGetPayload<TPayload>([NotNullWhen(true)] out TPayload? payload)
        where TPayload : class => throw Unavailable();

    /// <inheritdoc />
    public TPayload GetOrAddPayload<TPayload>(PayloadFactory<TPayload> payloadFactory)
        where TPayload : class => throw Unavailable();

    /// <inheritdoc />
    public TPayload AddOrUpdatePayload<TPayload>(PayloadFactory<TPayload> addFactory,
        UpdatePayloadFactory<TPayload> updateFactory)
        where TPayload : class => throw Unavailable();

    /// <inheritdoc />
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer) => throw Unavailable();

    /// <inheritdoc />
    public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken)
        where TMessage : class => FailUnavailableAsync(cancellationToken);

    /// <inheritdoc />
    public Task PublishAsync<TMessage>(TMessage message, IPipe<PublishContext<TMessage>> publishPipe,
        CancellationToken cancellationToken)
        where TMessage : class => FailUnavailableAsync(cancellationToken);

    /// <inheritdoc />
    public Task PublishAsync<TMessage>(TMessage message, IPipe<PublishContext> publishPipe,
        CancellationToken cancellationToken)
        where TMessage : class => FailUnavailableAsync(cancellationToken);

    /// <inheritdoc />
    public Task PublishAsync(object message, CancellationToken cancellationToken) => FailUnavailableAsync(cancellationToken);

    /// <inheritdoc />
    public Task PublishAsync(object message, IPipe<PublishContext> publishPipe, CancellationToken cancellationToken) =>
        FailUnavailableAsync(cancellationToken);

    /// <inheritdoc />
    public Task PublishAsync(object message, Type messageType, CancellationToken cancellationToken) =>
        FailUnavailableAsync(cancellationToken);

    /// <inheritdoc />
    public Task PublishAsync(object message, Type messageType, IPipe<PublishContext> publishPipe,
        CancellationToken cancellationToken) => FailUnavailableAsync(cancellationToken);

    /// <inheritdoc />
    public Task PublishAsync<TMessage>(object values, CancellationToken cancellationToken)
        where TMessage : class => FailUnavailableAsync(cancellationToken);

    /// <inheritdoc />
    public Task PublishAsync<TMessage>(object values, IPipe<PublishContext<TMessage>> publishPipe,
        CancellationToken cancellationToken)
        where TMessage : class => FailUnavailableAsync(cancellationToken);

    /// <inheritdoc />
    public Task PublishAsync<TMessage>(object values, IPipe<PublishContext> publishPipe,
        CancellationToken cancellationToken)
        where TMessage : class => FailUnavailableAsync(cancellationToken);

    /// <inheritdoc />
    public ConnectHandle ConnectSendObserver(ISendObserver observer) => throw Unavailable();

    /// <inheritdoc />
    public Task<ISendEndpoint> GetSendEndpointAsync(Uri address, CancellationToken cancellationToken = default) =>
        FailUnavailableAsync<ISendEndpoint>(cancellationToken);

    /// <inheritdoc />
    public bool HasMessageType(Type messageType) => throw Unavailable();

    /// <inheritdoc />
    public bool TryGetMessage<TMessage>([NotNullWhen(true)] out ConsumeContext<TMessage>? consumeContext)
        where TMessage : class => throw Unavailable();

    /// <inheritdoc />
    public void AddConsumeTask(Task task) => throw Unavailable();

    /// <inheritdoc />
    public Task RespondAsync<TResponse>(TResponse message)
        where TResponse : class => throw Unavailable();

    /// <inheritdoc />
    public Task RespondAsync<TResponse>(TResponse message, SendOptions options)
        where TResponse : class => throw Unavailable();

    /// <inheritdoc />
    public Task RespondAsync<TResponse>(TResponse message, IPipe<SendContext<TResponse>> sendPipe)
        where TResponse : class => throw Unavailable();

    /// <inheritdoc />
    public Task RespondAsync<TResponse>(TResponse message, IPipe<SendContext> sendPipe)
        where TResponse : class => throw Unavailable();

    /// <inheritdoc />
    public Task RespondAsync(object message) => throw Unavailable();

    /// <inheritdoc />
    public Task RespondAsync(object message, Type messageType) => throw Unavailable();

    /// <inheritdoc />
    public Task RespondAsync(object message, IPipe<SendContext> sendPipe) => throw Unavailable();

    /// <inheritdoc />
    public Task RespondAsync(object message, Type messageType, IPipe<SendContext> sendPipe) => throw Unavailable();

    /// <inheritdoc />
    public Task RespondAsync<TResponse>(object values)
        where TResponse : class => throw Unavailable();

    /// <inheritdoc />
    public Task RespondAsync<TResponse>(object values, IPipe<SendContext<TResponse>> sendPipe)
        where TResponse : class => throw Unavailable();

    /// <inheritdoc />
    public Task RespondAsync<TResponse>(object values, IPipe<SendContext> sendPipe)
        where TResponse : class => throw Unavailable();

    /// <inheritdoc />
    public void DeferResponse<TResponse>(TResponse message)
        where TResponse : class => throw Unavailable();

    /// <inheritdoc />
    public Task NotifyConsumedAsync<TMessage>(ConsumeContext<TMessage> context, TimeSpan duration,
        string consumerType, CancellationToken cancellationToken = default)
        where TMessage : class => FailUnavailableAsync(cancellationToken);

    /// <inheritdoc />
    public Task NotifyFaultedAsync<TMessage>(ConsumeContext<TMessage> context, TimeSpan duration,
        string consumerType, Exception exception, CancellationToken cancellationToken = default)
        where TMessage : class => FailUnavailableAsync(cancellationToken);

    private static ConsumeContextNotAvailableException Unavailable() => new();

    private static Task FailUnavailableAsync(CancellationToken cancellationToken)
    {
        return cancellationToken.IsCancellationRequested
            ? Task.FromCanceled(cancellationToken)
            : throw Unavailable();
    }

    private static Task<TResult> FailUnavailableAsync<TResult>(CancellationToken cancellationToken)
    {
        return cancellationToken.IsCancellationRequested
            ? Task.FromCanceled<TResult>(cancellationToken)
            : throw Unavailable();
    }
}
