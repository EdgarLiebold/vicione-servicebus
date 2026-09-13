using System;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.Mediator.Contexts;

/// <summary>Projects mediator send metadata through the transport-independent message-context contract.</summary>
/// <typeparam name="TMessage">The message contract.</typeparam>
internal sealed class MediatorSendMessageContext<TMessage> :
    MessageContext
    where TMessage : class
{
    readonly SendContext<TMessage> _context;

    /// <summary>Creates a message-context view over a mediator send context.</summary>
    /// <param name="context">The mediator send context to project.</param>
    public MediatorSendMessageContext(SendContext<TMessage> context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public Guid? MessageId => _context.MessageId;
    /// <inheritdoc />
    public Guid? RequestId => _context.RequestId;
    /// <inheritdoc />
    public Guid? CorrelationId => _context.CorrelationId;
    /// <inheritdoc />
    public Guid? ConversationId => _context.ConversationId;
    /// <inheritdoc />
    public Guid? InitiatorId => _context.InitiatorId;
    /// <inheritdoc />
    public DateTimeOffset? ExpirationTime => _context.SentTime + _context.TimeToLive;
    /// <inheritdoc />
    public Uri? SourceAddress => _context.SourceAddress;
    /// <inheritdoc />
    public Uri? DestinationAddress => _context.DestinationAddress;
    /// <inheritdoc />
    public Uri? ResponseAddress => _context.ResponseAddress;
    /// <inheritdoc />
    public Uri? FaultAddress => _context.FaultAddress;
    /// <inheritdoc />
    public DateTimeOffset? SentTime => _context.SentTime;
    /// <inheritdoc />
    public Headers Headers => _context.Headers;
    /// <inheritdoc />
    public HostInfo Host => HostMetadataCache.Host;
}
