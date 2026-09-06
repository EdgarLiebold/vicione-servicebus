using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Context;

/// <summary>
/// A consume context proxy creates a payload scope, such that anything added to the payload
/// of the context is only added at the scope level and below.
/// </summary>
public abstract class ConsumeContextProxy :
    BaseConsumeContext
{
    readonly ConsumeContext _context;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    protected ConsumeContextProxy(ConsumeContext context)
        : base(context.ReceiveContext, context.SerializerContext)
    {
        _context = context;
    }

    /// <summary>Returns the CancellationToken for the context (implicit interface).</summary>
    public override CancellationToken CancellationToken => _context.CancellationToken;

    /// <summary>Gets the message id.</summary>
    public override Guid? MessageId => _context.MessageId;
    /// <summary>Gets the request id.</summary>
    public override Guid? RequestId => _context.RequestId;
    /// <summary>Gets the correlation id.</summary>
    public override Guid? CorrelationId => _context.CorrelationId;
    /// <summary>Gets the conversation id.</summary>
    public override Guid? ConversationId => _context.ConversationId;
    /// <summary>Gets the initiator id.</summary>
    public override Guid? InitiatorId => _context.InitiatorId;
    /// <summary>Gets the expiration time.</summary>
    public override DateTimeOffset? ExpirationTime => _context.ExpirationTime;
    /// <summary>Gets the source address.</summary>
    public override Uri? SourceAddress => _context.SourceAddress;
    /// <summary>Gets the destination address.</summary>
    public override Uri? DestinationAddress => _context.DestinationAddress;
    /// <summary>Gets the response address.</summary>
    public override Uri? ResponseAddress => _context.ResponseAddress;
    /// <summary>Gets the fault address.</summary>
    public override Uri? FaultAddress => _context.FaultAddress;
    /// <summary>Gets the sent time.</summary>
    public override DateTimeOffset? SentTime => _context.SentTime;
    /// <summary>Gets the headers.</summary>
    public override Headers Headers => _context.Headers;
    /// <summary>Gets the host.</summary>
    public override HostInfo Host => _context.Host;

    /// <summary>Gets the consume completed.</summary>
    public override Task ConsumeCompleted => _context.ConsumeCompleted;

    /// <summary>Gets the supported message types.</summary>
    public override IEnumerable<string> SupportedMessageTypes => _context.SupportedMessageTypes;

    /// <summary>Determines whether the current value has message type.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public override bool HasMessageType(Type messageType)
    {
        return _context.HasMessageType(messageType);
    }

    /// <summary>Attempts to get message.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="consumeContext">Receives the consume context produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public override bool TryGetMessage<T>([NotNullWhen(true)] out ConsumeContext<T>? consumeContext)
    {
        if (_context.TryGetMessage(out ConsumeContext<T>? messageContext))
        {
            consumeContext = new MessageConsumeContext<T>(this, messageContext.Message);
            return true;
        }

        consumeContext = null;
        return false;
    }

    /// <summary>Adds consume task to the configuration.</summary>
    /// <param name="task">The task.</param>
    public override void AddConsumeTask(Task task)
    {
        _context.AddConsumeTask(task);
    }

    /// <summary>Returns true if the payload type is included with or supported by the context type.</summary>
    /// <param name="payloadType">The runtime payload type used by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public override bool HasPayloadType(Type payloadType)
    {
        return payloadType.IsInstanceOfType(this) || _context.HasPayloadType(payloadType);
    }

    /// <summary>Attempts to get the specified payload type.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="payload">Receives the payload produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public override bool TryGetPayload<T>([NotNullWhen(true)] out T? payload)
        where T : class
    {
        if (this is T context)
        {
            payload = context;
            return true;
        }

        return _context.TryGetPayload(out payload);
    }

    /// <summary>Get or add a payload to the context, using the provided payload factory.</summary>
    /// <typeparam name="T">The payload type.</typeparam>
    /// <param name="payloadFactory">The payload factory, which is only invoked if the payload is not present.</param>
    /// <returns>The or add payload.</returns>
    public override T GetOrAddPayload<T>(PayloadFactory<T> payloadFactory)
    {
        if (this is T context)
            return context;

        return _context.GetOrAddPayload(payloadFactory);
    }

    /// <summary>Either adds a new payload, or updates an existing payload.</summary>
    /// <typeparam name="T">The payload type.</typeparam>
    /// <param name="addFactory">The payload factory called if the payload is not present.</param>
    /// <param name="updateFactory">The payload factory called if the payload already exists.</param>
    /// <returns>The t produced by the operation.</returns>
    public override T AddOrUpdatePayload<T>(PayloadFactory<T> addFactory, UpdatePayloadFactory<T> updateFactory)
    {
        if (this is T context)
            return context;

        return _context.AddOrUpdatePayload(addFactory, updateFactory);
    }

    /// <summary>Reports that notify has been consumed.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public override Task NotifyConsumedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
    {
        return _context.NotifyConsumedAsync(context, duration, consumerType, cancellationToken: cancellationToken);
    }

    /// <summary>Reports that notify has faulted.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="duration">The duration.</param>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public override Task NotifyFaultedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
    {
        return _context.NotifyFaultedAsync(context, duration, consumerType, exception, cancellationToken: cancellationToken);
    }
}


/// <summary>
/// A consume context proxy creates a payload scope, such that anything added to the payload
/// of the context is only added at the scope level and below.
/// </summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class ConsumeContextProxy<TMessage> :
    ConsumeContextProxy,
    ConsumeContext<TMessage>
    where TMessage : class
{
    readonly ConsumeContext<TMessage> _context;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public ConsumeContextProxy(ConsumeContext<TMessage> context)
        : base(context.Advanced())
    {
        _context = context;
    }

    /// <summary>Gets the message.</summary>
    public TMessage Message => _context.Message;

    /// <summary>Reports that notify has been consumed.</summary>
    /// <param name="duration">The duration.</param>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual Task NotifyConsumedAsync(TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
    {
        return NotifyConsumedAsync(this, duration, consumerType, cancellationToken: cancellationToken);
    }

    /// <summary>Reports that notify has faulted.</summary>
    /// <param name="duration">The duration.</param>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public virtual Task NotifyFaultedAsync(TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
    {
        return NotifyFaultedAsync(this, duration, consumerType, exception, cancellationToken: cancellationToken);
    }
}
