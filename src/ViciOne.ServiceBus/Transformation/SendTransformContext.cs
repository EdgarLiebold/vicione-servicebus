using System;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Transformation;

/// <summary>
/// Sits in front of the consume context and allows the inbound message to be
/// transformed.
/// </summary>
/// <typeparam name="TMessage"></typeparam>
public class SendTransformContext<TMessage> :
    ProxyPipeContext,
    TransformContext<TMessage>
    where TMessage : class
{
    readonly SendContext<TMessage> _context;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public SendTransformContext(SendContext<TMessage> context)
        : base(context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets the message id value.
    /// </summary>
    public Guid? MessageId => _context.MessageId;
    /// <summary>
    /// Gets the request id value.
    /// </summary>
    public Guid? RequestId => _context.RequestId;
    /// <summary>
    /// Gets the correlation id value.
    /// </summary>
    public Guid? CorrelationId => _context.CorrelationId;
    /// <summary>
    /// Gets the conversation id value.
    /// </summary>
    public Guid? ConversationId => _context.ConversationId;
    /// <summary>
    /// Gets the initiator id value.
    /// </summary>
    public Guid? InitiatorId => _context.InitiatorId;
    /// <summary>
    /// Gets the expiration time value.
    /// </summary>
    public DateTimeOffset? ExpirationTime => _context.TimeToLive.HasValue
        ? _context.GetTimeProvider().GetUtcNow().UtcDateTime + _context.TimeToLive.Value
        : null;
    /// <summary>
    /// Gets the source address value.
    /// </summary>
    public Uri? SourceAddress => _context.SourceAddress;
    /// <summary>
    /// Gets the destination address value.
    /// </summary>
    public Uri? DestinationAddress => _context.DestinationAddress;
    /// <summary>
    /// Gets the response address value.
    /// </summary>
    public Uri? ResponseAddress => _context.ResponseAddress;
    /// <summary>
    /// Gets the fault address value.
    /// </summary>
    public Uri? FaultAddress => _context.FaultAddress;
    /// <summary>
    /// Gets the sent time value.
    /// </summary>
    public DateTimeOffset? SentTime => default;
    /// <summary>
    /// Gets the headers value.
    /// </summary>
    public Headers Headers => _context.Headers;
    /// <summary>
    /// Gets the host value.
    /// </summary>
    public HostInfo Host => HostMetadataCache.Host;

    /// <summary>
    /// Gets the has input value.
    /// </summary>
    public bool HasInput => true;

    /// <summary>
    /// Gets the input value.
    /// </summary>
    public TMessage Input => _context.Message;
}
