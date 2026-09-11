using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Mediator.Contexts;

/// <summary>Represents a mediator dispatch as a typed consume context.</summary>
/// <typeparam name="TMessage">The dispatched message contract.</typeparam>
internal sealed class MediatorConsumeContext<TMessage> :
    DeserializerConsumeContext,
    ConsumeContext<TMessage>
    where TMessage : class
{
    /// <summary>Creates a typed consume context for one mediator dispatch.</summary>
    /// <param name="receiveContext">The in-process receive context.</param>
    /// <param name="serializerContext">The serializer context that owns message metadata.</param>
    /// <param name="message">The dispatched message.</param>
    public MediatorConsumeContext(ReceiveContext receiveContext, SerializerContext serializerContext, TMessage message)
        : base(receiveContext, serializerContext)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
    }

    /// <inheritdoc />
    public override bool HasMessageType(Type messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        return messageType.IsAssignableFrom(typeof(TMessage));
    }

    /// <inheritdoc />
    public override bool TryGetMessage<T>([NotNullWhen(true)] out ConsumeContext<T>? consumeContext)
    {
        if (Message is T message)
        {
            consumeContext = new MessageConsumeContext<T>(this, message);
            return true;
        }

        consumeContext = default;
        return false;
    }

    /// <inheritdoc />
    public override Guid? MessageId => SerializerContext.MessageId;
    /// <inheritdoc />
    public override Guid? RequestId => SerializerContext.RequestId;
    /// <inheritdoc />
    public override Guid? CorrelationId => SerializerContext.CorrelationId;
    /// <inheritdoc />
    public override Guid? ConversationId => SerializerContext.ConversationId;
    /// <inheritdoc />
    public override Guid? InitiatorId => SerializerContext.InitiatorId;
    /// <inheritdoc />
    public override DateTimeOffset? ExpirationTime => SerializerContext.ExpirationTime;
    /// <inheritdoc />
    public override Uri? SourceAddress => SerializerContext.SourceAddress;
    /// <inheritdoc />
    public override Uri? DestinationAddress => SerializerContext.DestinationAddress;
    /// <inheritdoc />
    public override Uri? ResponseAddress => SerializerContext.ResponseAddress;
    /// <inheritdoc />
    public override Uri? FaultAddress => SerializerContext.FaultAddress;
    /// <inheritdoc />
    public override DateTimeOffset? SentTime => SerializerContext.SentTime;
    /// <inheritdoc />
    public override Headers Headers => SerializerContext.Headers;
    /// <inheritdoc />
    public override HostInfo Host => SerializerContext.Host;
    /// <inheritdoc />
    public override IEnumerable<string> SupportedMessageTypes => SerializerContext.SupportedMessageTypes;

    /// <inheritdoc />
    public TMessage Message { get; }

    /// <inheritdoc />
    public Task NotifyConsumedAsync(TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
    {
        return ReceiveContext.NotifyConsumedAsync(this, duration, consumerType, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public Task NotifyFaultedAsync(TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
    {
        return ReceiveContext.NotifyFaultedAsync(this, duration, consumerType, exception, cancellationToken: cancellationToken);
    }

    /// <summary>Suppresses transport fault publication because mediator dispatch propagates failures directly.</summary>
    /// <typeparam name="T">The failed message contract.</typeparam>
    /// <param name="context">The mediator consume context that faulted.</param>
    /// <param name="exception">The dispatch failure.</param>
    /// <returns>A completed task because no fault message is published.</returns>
    protected override Task GenerateFaultAsync<T>(ConsumeContext<T> context, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(exception);
        return Task.CompletedTask;
    }
}
