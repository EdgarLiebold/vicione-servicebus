using System;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.Context;

public class MediatorSendMessageContext<T> :
    MessageContext
    where T : class
{
    readonly SendContext<T> _context;

    public MediatorSendMessageContext(SendContext<T> context)
    {
        _context = context;
    }

    public Guid? MessageId => _context.MessageId;
    public Guid? RequestId => _context.RequestId;
    public Guid? CorrelationId => _context.CorrelationId;
    public Guid? ConversationId => _context.ConversationId;
    public Guid? InitiatorId => _context.InitiatorId;
    public DateTimeOffset? ExpirationTime => _context.SentTime + _context.TimeToLive;
    public Uri? SourceAddress => _context.SourceAddress;
    public Uri? DestinationAddress => _context.DestinationAddress;
    public Uri? ResponseAddress => _context.ResponseAddress;
    public Uri? FaultAddress => _context.FaultAddress;
    public DateTimeOffset? SentTime => _context.SentTime;
    public Headers Headers => _context.Headers;
    public HostInfo Host => HostMetadataCache.Host;
}
