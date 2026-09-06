using System;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Transformation;

/// <summary>For nested types transformed.</summary>
/// <typeparam name="TMessage">The input message transform context type.</typeparam>
/// <typeparam name="TProperty">The property type.</typeparam>
public class PropertyTransformContext<TMessage, TProperty> :
    ProxyPipeContext,
    TransformContext<TProperty>
    where TMessage : class
    where TProperty : class
{
    readonly TransformContext<TMessage> _context;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="property">The property.</param>
    public PropertyTransformContext(TransformContext<TMessage> context, TProperty property)
        : base(context)
    {
        _context = context;
        Input = property;
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
    public DateTimeOffset? ExpirationTime => _context.ExpirationTime;
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
    public TProperty Input { get; }
}
