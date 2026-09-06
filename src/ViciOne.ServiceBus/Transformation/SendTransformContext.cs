using System;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Transformation;

/// <summary>
/// Sits in front of the consume context and allows the inbound message to be
/// transformed.
/// </summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class SendTransformContext<TMessage> :
    ProxyPipeContext,
    TransformContext<TMessage>
    where TMessage : class
{
    readonly SendContext<TMessage> _context;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public SendTransformContext(SendContext<TMessage> context)
        : base(context)
    {
        _context = context;
    }

    /// <summary>Gets the message id.</summary>
    public Guid? MessageId => _context.MessageId;
    /// <summary>Gets the request id.</summary>
    public Guid? RequestId => _context.RequestId;
    /// <summary>Gets the correlation id.</summary>
    public Guid? CorrelationId => _context.CorrelationId;
    /// <summary>Gets the conversation id.</summary>
    public Guid? ConversationId => _context.ConversationId;
    /// <summary>Gets the initiator id.</summary>
    public Guid? InitiatorId => _context.InitiatorId;
    /// <summary>Gets the expiration time.</summary>
    public DateTimeOffset? ExpirationTime => _context.TimeToLive.HasValue
        ? _context.GetTimeProvider().GetUtcNow().UtcDateTime + _context.TimeToLive.Value
        : null;
    /// <summary>Gets the source address.</summary>
    public Uri? SourceAddress => _context.SourceAddress;
    /// <summary>Gets the destination address.</summary>
    public Uri? DestinationAddress => _context.DestinationAddress;
    /// <summary>Gets the response address.</summary>
    public Uri? ResponseAddress => _context.ResponseAddress;
    /// <summary>Gets the fault address.</summary>
    public Uri? FaultAddress => _context.FaultAddress;
    /// <summary>Gets the sent time.</summary>
    public DateTimeOffset? SentTime => default;
    /// <summary>Gets the headers.</summary>
    public Headers Headers => _context.Headers;
    /// <summary>Gets the host.</summary>
    public HostInfo Host => HostMetadataCache.Host;

    /// <summary>Gets a value indicating whether this instance has input.</summary>
    public bool HasInput => true;

    /// <summary>Gets the input.</summary>
    public TMessage Input => _context.Message;
}
