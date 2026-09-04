using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Context;

/// <summary>
/// Provides a mediator consume context implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class MediatorConsumeContext<TMessage> :
    DeserializerConsumeContext,
    ConsumeContext<TMessage>
    where TMessage : class
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="receiveContext">The receive context value.</param>
    /// <param name="serializerContext">The serializer context value.</param>
    /// <param name="message">The message value.</param>
    public MediatorConsumeContext(ReceiveContext receiveContext, SerializerContext serializerContext, TMessage message)
        : base(receiveContext, serializerContext)
    {
        Message = message;
    }

    /// <summary>
    /// Determines whether the current value has message type.
    /// </summary>
    /// <param name="messageType">The message type value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public override bool HasMessageType(Type messageType)
    {
        return messageType.IsAssignableFrom(typeof(TMessage));
    }

    /// <summary>
    /// Attempts to get message.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="consumeContext">The consume context value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
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

    /// <summary>
    /// Gets the message id value.
    /// </summary>
    public override Guid? MessageId => SerializerContext.MessageId;
    /// <summary>
    /// Gets the request id value.
    /// </summary>
    public override Guid? RequestId => SerializerContext.RequestId;
    /// <summary>
    /// Gets the correlation id value.
    /// </summary>
    public override Guid? CorrelationId => SerializerContext.CorrelationId;
    /// <summary>
    /// Gets the conversation id value.
    /// </summary>
    public override Guid? ConversationId => SerializerContext.ConversationId;
    /// <summary>
    /// Gets the initiator id value.
    /// </summary>
    public override Guid? InitiatorId => SerializerContext.InitiatorId;
    /// <summary>
    /// Gets the expiration time value.
    /// </summary>
    public override DateTimeOffset? ExpirationTime => SerializerContext.ExpirationTime;
    /// <summary>
    /// Gets the source address value.
    /// </summary>
    public override Uri? SourceAddress => SerializerContext.SourceAddress;
    /// <summary>
    /// Gets the destination address value.
    /// </summary>
    public override Uri? DestinationAddress => SerializerContext.DestinationAddress;
    /// <summary>
    /// Gets the response address value.
    /// </summary>
    public override Uri? ResponseAddress => SerializerContext.ResponseAddress;
    /// <summary>
    /// Gets the fault address value.
    /// </summary>
    public override Uri? FaultAddress => SerializerContext.FaultAddress;
    /// <summary>
    /// Gets the sent time value.
    /// </summary>
    public override DateTimeOffset? SentTime => SerializerContext.SentTime;
    /// <summary>
    /// Gets the headers value.
    /// </summary>
    public override Headers Headers => SerializerContext.Headers;
    /// <summary>
    /// Gets the host value.
    /// </summary>
    public override HostInfo Host => SerializerContext.Host;
    /// <summary>
    /// Gets the supported message types value.
    /// </summary>
    public override IEnumerable<string> SupportedMessageTypes => SerializerContext.SupportedMessageTypes;

    /// <summary>
    /// Gets the message value.
    /// </summary>
    public TMessage Message { get; }

    /// <summary>
    /// Performs the notify consumed operation.
    /// </summary>
    /// <param name="duration">The duration value.</param>
    /// <param name="consumerType">The consumer type value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task NotifyConsumedAsync(TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
    {
        return ReceiveContext.NotifyConsumedAsync(this, duration, consumerType, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs the notify faulted operation.
    /// </summary>
    /// <param name="duration">The duration value.</param>
    /// <param name="consumerType">The consumer type value.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task NotifyFaultedAsync(TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
    {
        return ReceiveContext.NotifyFaultedAsync(this, duration, consumerType, exception, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs the generate fault operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>The result of the operation.</returns>
    protected override Task GenerateFaultAsync<T>(ConsumeContext<T> context, Exception exception)
    {
        return Task.CompletedTask;
    }
}
