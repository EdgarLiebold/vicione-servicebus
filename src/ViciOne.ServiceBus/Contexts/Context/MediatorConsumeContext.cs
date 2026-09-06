using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Context;

/// <summary>Carries state for mediator consume operations.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class MediatorConsumeContext<TMessage> :
    DeserializerConsumeContext,
    ConsumeContext<TMessage>
    where TMessage : class
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="receiveContext">The receive context.</param>
    /// <param name="serializerContext">The serializer context.</param>
    /// <param name="message">The message to process.</param>
    public MediatorConsumeContext(ReceiveContext receiveContext, SerializerContext serializerContext, TMessage message)
        : base(receiveContext, serializerContext)
    {
        Message = message;
    }

    /// <summary>Determines whether the current value has message type.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public override bool HasMessageType(Type messageType)
    {
        return messageType.IsAssignableFrom(typeof(TMessage));
    }

    /// <summary>Attempts to get message.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="consumeContext">Receives the consume context produced by the operation.</param>
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

    /// <summary>Gets the message id.</summary>
    public override Guid? MessageId => SerializerContext.MessageId;
    /// <summary>Gets the request id.</summary>
    public override Guid? RequestId => SerializerContext.RequestId;
    /// <summary>Gets the correlation id.</summary>
    public override Guid? CorrelationId => SerializerContext.CorrelationId;
    /// <summary>Gets the conversation id.</summary>
    public override Guid? ConversationId => SerializerContext.ConversationId;
    /// <summary>Gets the initiator id.</summary>
    public override Guid? InitiatorId => SerializerContext.InitiatorId;
    /// <summary>Gets the expiration time.</summary>
    public override DateTimeOffset? ExpirationTime => SerializerContext.ExpirationTime;
    /// <summary>Gets the source address.</summary>
    public override Uri? SourceAddress => SerializerContext.SourceAddress;
    /// <summary>Gets the destination address.</summary>
    public override Uri? DestinationAddress => SerializerContext.DestinationAddress;
    /// <summary>Gets the response address.</summary>
    public override Uri? ResponseAddress => SerializerContext.ResponseAddress;
    /// <summary>Gets the fault address.</summary>
    public override Uri? FaultAddress => SerializerContext.FaultAddress;
    /// <summary>Gets the sent time.</summary>
    public override DateTimeOffset? SentTime => SerializerContext.SentTime;
    /// <summary>Gets the headers.</summary>
    public override Headers Headers => SerializerContext.Headers;
    /// <summary>Gets the host.</summary>
    public override HostInfo Host => SerializerContext.Host;
    /// <summary>Gets the supported message types.</summary>
    public override IEnumerable<string> SupportedMessageTypes => SerializerContext.SupportedMessageTypes;

    /// <summary>Gets the message.</summary>
    public TMessage Message { get; }

    /// <summary>Reports that notify has been consumed.</summary>
    /// <param name="duration">The duration.</param>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task NotifyConsumedAsync(TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
    {
        return ReceiveContext.NotifyConsumedAsync(this, duration, consumerType, cancellationToken: cancellationToken);
    }

    /// <summary>Reports that notify has faulted.</summary>
    /// <param name="duration">The duration.</param>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task NotifyFaultedAsync(TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
    {
        return ReceiveContext.NotifyFaultedAsync(this, duration, consumerType, exception, cancellationToken: cancellationToken);
    }

    /// <summary>Generates fault.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected override Task GenerateFaultAsync<T>(ConsumeContext<T> context, Exception exception)
    {
        return Task.CompletedTask;
    }
}
