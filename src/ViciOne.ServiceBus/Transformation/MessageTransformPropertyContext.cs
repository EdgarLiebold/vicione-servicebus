using System;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Transformation;

/// <summary>Carries state for message transform property operations.</summary>
/// <typeparam name="TProperty">The property type.</typeparam>
/// <typeparam name="TInput">The input type.</typeparam>
public class MessageTransformPropertyContext<TProperty, TInput> :
    ProxyPipeContext,
    TransformPropertyContext<TProperty, TInput>
    where TInput : class
{
    readonly TransformContext<TInput> _context;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="value">The value to process.</param>
    public MessageTransformPropertyContext(TransformContext<TInput> context, TProperty? value)
        : base(context)
    {
        _context = context;

        Value = value;
        HasValue = true;
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
    public DateTimeOffset? SentTime => _context.SentTime;
    /// <summary>Gets the headers.</summary>
    public Headers Headers => _context.Headers;
    /// <summary>Gets the host.</summary>
    public HostInfo Host => _context.Host;

    /// <summary>Gets a value indicating whether this instance has input.</summary>
    public bool HasInput => _context.HasInput;
    /// <summary>Gets the input.</summary>
    public TInput Input => _context.Input;

    /// <summary>Gets whether this instance contains a value.</summary>
    public bool HasValue { get; }
    /// <summary>Gets the value.</summary>
    public TProperty? Value { get; }
}
