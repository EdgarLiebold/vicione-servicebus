using System;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.Context;

/// <summary>Carries state for mediator send message operations.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class MediatorSendMessageContext<T> :
    MessageContext
    where T : class
{
    readonly SendContext<T> _context;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public MediatorSendMessageContext(SendContext<T> context)
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
    public DateTimeOffset? ExpirationTime => _context.SentTime + _context.TimeToLive;
    /// <summary>Gets the source address.</summary>
    public Uri? SourceAddress => _context.SourceAddress;
    /// <summary>Gets the destination address.</summary>
    public Uri? DestinationAddress => _context.DestinationAddress;
    /// <summary>Gets the response address.</summary>
    public Uri? ResponseAddress => _context.ResponseAddress;
    /// <summary>Gets the fault address.</summary>
    public Uri? FaultAddress => _context.FaultAddress;
    /// <summary>Gets the sent time.</summary>
    public DateTimeOffset? SentTime => _context.SentTime;
    /// <summary>Gets the headers.</summary>
    public Headers Headers => _context.Headers;
    /// <summary>Gets the host.</summary>
    public HostInfo Host => HostMetadataCache.Host;
}
