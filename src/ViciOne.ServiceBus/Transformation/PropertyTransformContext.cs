using System;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Transformation;

/// <summary>
/// For nested types transformed.
/// </summary>
/// <typeparam name="TMessage">The input message transform context type</typeparam>
/// <typeparam name="TProperty"></typeparam>
public class PropertyTransformContext<TMessage, TProperty> :
    ProxyPipeContext,
    TransformContext<TProperty>
    where TMessage : class
    where TProperty : class
{
    readonly TransformContext<TMessage> _context;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="property">The property value.</param>
    public PropertyTransformContext(TransformContext<TMessage> context, TProperty property)
        : base(context)
    {
        _context = context;
        Input = property;
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
    public DateTimeOffset? ExpirationTime => _context.ExpirationTime;
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
    public TProperty Input { get; }
}
