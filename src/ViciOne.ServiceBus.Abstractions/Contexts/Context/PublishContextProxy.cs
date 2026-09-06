using System;
using System.Net.Mime;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Context;

/// <summary>Forwards publish context operations to an underlying context.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class PublishContextProxy<TMessage> :
    PublishContextProxy,
    PublishContext<TMessage>
    where TMessage : class
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="message">The message to process.</param>
    public PublishContextProxy(PublishContext context, TMessage message)
        : base(context)
    {
        Message = message;
    }

    /// <summary>Gets the message.</summary>
    public TMessage Message { get; }
}


/// <summary>Forwards publish context operations to an underlying context.</summary>
public class PublishContextProxy :
    ProxyPipeContext,
    PublishContext
{
    readonly PublishContext _context;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    protected PublishContextProxy(PublishContext context)
        : base(context)
    {
        _context = context;
    }

    /// <summary>Gets or sets the source address.</summary>
    public Uri? SourceAddress
    {
        get => _context.SourceAddress;
        set => _context.SourceAddress = value;
    }

    /// <summary>Gets or sets the destination address.</summary>
    public Uri? DestinationAddress
    {
        get => _context.DestinationAddress;
        set => _context.DestinationAddress = value;
    }

    /// <summary>Gets or sets the response address.</summary>
    public Uri? ResponseAddress
    {
        get => _context.ResponseAddress;
        set => _context.ResponseAddress = value;
    }

    /// <summary>Gets or sets the fault address.</summary>
    public Uri? FaultAddress
    {
        get => _context.FaultAddress;
        set => _context.FaultAddress = value;
    }

    /// <summary>Gets or sets the request id.</summary>
    public Guid? RequestId
    {
        get => _context.RequestId;
        set => _context.RequestId = value;
    }

    /// <summary>Gets or sets the message id.</summary>
    public Guid? MessageId
    {
        get => _context.MessageId;
        set => _context.MessageId = value;
    }

    /// <summary>Gets or sets the correlation id.</summary>
    public Guid? CorrelationId
    {
        get => _context.CorrelationId;
        set => _context.CorrelationId = value;
    }

    /// <summary>Gets or sets the conversation id.</summary>
    public Guid? ConversationId
    {
        get => _context.ConversationId;
        set => _context.ConversationId = value;
    }

    /// <summary>Gets or sets the initiator id.</summary>
    public Guid? InitiatorId
    {
        get => _context.InitiatorId;
        set => _context.InitiatorId = value;
    }

    /// <summary>Gets or sets the scheduled message id.</summary>
    public Guid? ScheduledMessageId
    {
        get => _context.ScheduledMessageId;
        set => _context.ScheduledMessageId = value;
    }

    /// <summary>Gets the headers.</summary>
    public SendHeaders Headers => _context.Headers;

    /// <summary>Gets or sets the time to live.</summary>
    public TimeSpan? TimeToLive
    {
        get => _context.TimeToLive;
        set => _context.TimeToLive = value;
    }

    /// <summary>Gets the sent time.</summary>
    public DateTimeOffset? SentTime => _context.SentTime;

    /// <summary>Gets or sets the content type.</summary>
    public ContentType? ContentType
    {
        get => _context.ContentType;
        set => _context.ContentType = value;
    }

    /// <summary>Gets or sets the durable.</summary>
    public bool Durable
    {
        get => _context.Durable;
        set => _context.Durable = value;
    }

    /// <summary>Gets or sets the delay.</summary>
    public TimeSpan? Delay
    {
        get => _context.Delay;
        set => _context.Delay = value;
    }

    /// <summary>Gets or sets the serializer.</summary>
    public IMessageSerializer Serializer
    {
        get => _context.Serializer;
        set => _context.Serializer = value;
    }

    /// <summary>Gets or sets the serialization.</summary>
    public ISerialization Serialization
    {
        get => _context.Serialization;
        set => _context.Serialization = value;
    }

    /// <summary>Gets or sets the supported message types.</summary>
    public string[] SupportedMessageTypes
    {
        get => _context.SupportedMessageTypes;
        set => _context.SupportedMessageTypes = value;
    }

    /// <summary>Gets the body length.</summary>
    public long? BodyLength => _context.BodyLength;

    /// <summary>Creates proxy.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <returns>The created proxy.</returns>
    public SendContext<T> CreateProxy<T>(T message)
        where T : class
    {
        return new PublishContextProxy<T>(this, message);
    }

    /// <summary>Gets or sets the mandatory.</summary>
    public bool Mandatory
    {
        get => _context.Mandatory;
        set => _context.Mandatory = value;
    }
}
