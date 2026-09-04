using System;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Transformation;

/// <summary>
/// Provides a message transform property context implementation.
/// </summary>
/// <typeparam name="TProperty">The t property type.</typeparam>
/// <typeparam name="TInput">The t input type.</typeparam>
public class MessageTransformPropertyContext<TProperty, TInput> :
    ProxyPipeContext,
    TransformPropertyContext<TProperty, TInput>
    where TInput : class
{
    readonly TransformContext<TInput> _context;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="value">The value.</param>
    public MessageTransformPropertyContext(TransformContext<TInput> context, TProperty? value)
        : base(context)
    {
        _context = context;

        Value = value;
        HasValue = true;
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
    public DateTimeOffset? SentTime => _context.SentTime;
    /// <summary>
    /// Gets the headers value.
    /// </summary>
    public Headers Headers => _context.Headers;
    /// <summary>
    /// Gets the host value.
    /// </summary>
    public HostInfo Host => _context.Host;

    /// <summary>
    /// Gets the has input value.
    /// </summary>
    public bool HasInput => _context.HasInput;
    /// <summary>
    /// Gets the input value.
    /// </summary>
    public TInput Input => _context.Input;

    /// <summary>
    /// Gets the has value value.
    /// </summary>
    public bool HasValue { get; }
    /// <summary>
    /// Gets the underlying value.
    /// </summary>
    public TProperty? Value { get; }
}
