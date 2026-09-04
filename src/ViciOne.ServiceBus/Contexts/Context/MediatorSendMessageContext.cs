using System;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.Context;

/// <summary>
/// Provides a mediator send message context implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class MediatorSendMessageContext<T> :
    MessageContext
    where T : class
{
    readonly SendContext<T> _context;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public MediatorSendMessageContext(SendContext<T> context)
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
    public DateTimeOffset? ExpirationTime => _context.SentTime + _context.TimeToLive;
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
    public DateTimeOffset? SentTime => _context.SentTime;
    /// <summary>
    /// Gets the headers value.
    /// </summary>
    public Headers Headers => _context.Headers;
    /// <summary>
    /// Gets the host value.
    /// </summary>
    public HostInfo Host => HostMetadataCache.Host;
}
