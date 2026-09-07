using System;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Transformation;

/// <summary>Projects an inbound consume context and the message being transformed.</summary>
/// <typeparam name="TInput">The source message type.</typeparam>
internal sealed class ConsumeTransformContext<TInput> :
    ProxyPipeContext,
    TransformContext<TInput>
    where TInput : class
{
    readonly ConsumeContext _context;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="input">The input.</param>
    public ConsumeTransformContext(ConsumeContext context, TInput input)
        : base(context ?? throw new ArgumentNullException(nameof(context)))
    {
        _context = context;
        Input = input ?? throw new ArgumentNullException(nameof(input));
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

    /// <summary>Gets whether the source message is available.</summary>
    public bool HasInput => true;

    /// <summary>Gets the source message.</summary>
    public TInput Input { get; }
}
