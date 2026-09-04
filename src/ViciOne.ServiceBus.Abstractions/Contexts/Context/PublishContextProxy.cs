using System;
using System.Net.Mime;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Context;

/// <summary>
/// Provides a publish context proxy implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class PublishContextProxy<TMessage> :
    PublishContextProxy,
    PublishContext<TMessage>
    where TMessage : class
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="message">The message value.</param>
    public PublishContextProxy(PublishContext context, TMessage message)
        : base(context)
    {
        Message = message;
    }

    /// <summary>
    /// Gets the message value.
    /// </summary>
    public TMessage Message { get; }
}


/// <summary>
/// Provides a publish context proxy implementation.
/// </summary>
public class PublishContextProxy :
    ProxyPipeContext,
    PublishContext
{
    readonly PublishContext _context;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    protected PublishContextProxy(PublishContext context)
        : base(context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets or sets the source address value.
    /// </summary>
    public Uri? SourceAddress
    {
        get => _context.SourceAddress;
        set => _context.SourceAddress = value;
    }

    /// <summary>
    /// Gets or sets the destination address value.
    /// </summary>
    public Uri? DestinationAddress
    {
        get => _context.DestinationAddress;
        set => _context.DestinationAddress = value;
    }

    /// <summary>
    /// Gets or sets the response address value.
    /// </summary>
    public Uri? ResponseAddress
    {
        get => _context.ResponseAddress;
        set => _context.ResponseAddress = value;
    }

    /// <summary>
    /// Gets or sets the fault address value.
    /// </summary>
    public Uri? FaultAddress
    {
        get => _context.FaultAddress;
        set => _context.FaultAddress = value;
    }

    /// <summary>
    /// Gets or sets the request id value.
    /// </summary>
    public Guid? RequestId
    {
        get => _context.RequestId;
        set => _context.RequestId = value;
    }

    /// <summary>
    /// Gets or sets the message id value.
    /// </summary>
    public Guid? MessageId
    {
        get => _context.MessageId;
        set => _context.MessageId = value;
    }

    /// <summary>
    /// Gets or sets the correlation id value.
    /// </summary>
    public Guid? CorrelationId
    {
        get => _context.CorrelationId;
        set => _context.CorrelationId = value;
    }

    /// <summary>
    /// Gets or sets the conversation id value.
    /// </summary>
    public Guid? ConversationId
    {
        get => _context.ConversationId;
        set => _context.ConversationId = value;
    }

    /// <summary>
    /// Gets or sets the initiator id value.
    /// </summary>
    public Guid? InitiatorId
    {
        get => _context.InitiatorId;
        set => _context.InitiatorId = value;
    }

    /// <summary>
    /// Gets or sets the scheduled message id value.
    /// </summary>
    public Guid? ScheduledMessageId
    {
        get => _context.ScheduledMessageId;
        set => _context.ScheduledMessageId = value;
    }

    /// <summary>
    /// Gets the headers value.
    /// </summary>
    public SendHeaders Headers => _context.Headers;

    /// <summary>
    /// Gets or sets the time to live value.
    /// </summary>
    public TimeSpan? TimeToLive
    {
        get => _context.TimeToLive;
        set => _context.TimeToLive = value;
    }

    /// <summary>
    /// Gets the sent time value.
    /// </summary>
    public DateTimeOffset? SentTime => _context.SentTime;

    /// <summary>
    /// Gets or sets the content type value.
    /// </summary>
    public ContentType? ContentType
    {
        get => _context.ContentType;
        set => _context.ContentType = value;
    }

    /// <summary>
    /// Gets or sets the durable value.
    /// </summary>
    public bool Durable
    {
        get => _context.Durable;
        set => _context.Durable = value;
    }

    /// <summary>
    /// Gets or sets the delay value.
    /// </summary>
    public TimeSpan? Delay
    {
        get => _context.Delay;
        set => _context.Delay = value;
    }

    /// <summary>
    /// Gets or sets the serializer value.
    /// </summary>
    public IMessageSerializer Serializer
    {
        get => _context.Serializer;
        set => _context.Serializer = value;
    }

    /// <summary>
    /// Gets or sets the serialization value.
    /// </summary>
    public ISerialization Serialization
    {
        get => _context.Serialization;
        set => _context.Serialization = value;
    }

    /// <summary>
    /// Gets or sets the supported message types value.
    /// </summary>
    public string[] SupportedMessageTypes
    {
        get => _context.SupportedMessageTypes;
        set => _context.SupportedMessageTypes = value;
    }

    /// <summary>
    /// Gets the body length value.
    /// </summary>
    public long? BodyLength => _context.BodyLength;

    /// <summary>
    /// Creates proxy.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <returns>The result of the operation.</returns>
    public SendContext<T> CreateProxy<T>(T message)
        where T : class
    {
        return new PublishContextProxy<T>(this, message);
    }

    /// <summary>
    /// Gets or sets the mandatory value.
    /// </summary>
    public bool Mandatory
    {
        get => _context.Mandatory;
        set => _context.Mandatory = value;
    }
}
