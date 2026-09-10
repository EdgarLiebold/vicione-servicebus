using System;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Transformation;

/// <summary>Projects an outbound send context and the message being transformed.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
internal sealed class SendTransformContext<TMessage> :
    ProxyPipeContext,
    TransformContext<TMessage>
    where TMessage : class
{
    readonly SendContext<TMessage> _context;

    /// <summary>Creates a transform view over an outbound send operation.</summary>
    /// <param name="context">The send context whose message and envelope metadata are exposed.</param>
    public SendTransformContext(SendContext<TMessage> context)
        : base(context ?? throw new ArgumentNullException(nameof(context)))
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
    /// <summary>Gets the expiration time calculated from the current clock and time to live.</summary>
    public DateTimeOffset? ExpirationTime => _context.TimeToLive.HasValue
        ? _context.GetTimeProvider().GetUtcNow() + _context.TimeToLive.Value
        : null;
    /// <summary>Gets the source address.</summary>
    public Uri? SourceAddress => _context.SourceAddress;
    /// <summary>Gets the destination address.</summary>
    public Uri? DestinationAddress => _context.DestinationAddress;
    /// <summary>Gets the response address.</summary>
    public Uri? ResponseAddress => _context.ResponseAddress;
    /// <summary>Gets the fault address.</summary>
    public Uri? FaultAddress => _context.FaultAddress;
    /// <summary>Gets no sent timestamp because the outbound message has not reached the transport.</summary>
    public DateTimeOffset? SentTime => default;
    /// <summary>Gets the headers.</summary>
    public Headers Headers => _context.Headers;
    /// <summary>Gets the host.</summary>
    public HostInfo Host => HostMetadataCache.Host;

    /// <summary>Gets whether the outbound message is available.</summary>
    public bool HasInput => true;

    /// <summary>Gets the outbound message.</summary>
    public TMessage Input => _context.Message;
}
