using System;
using System.Net.Mime;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Context;

/// <summary>Exposes a message-specific view over an existing publish context.</summary>
/// <typeparam name="TMessage">The published message type.</typeparam>
public sealed class PublishContextProxy<TMessage> :
    PublishContextProxy,
    PublishContext<TMessage>
    where TMessage : class
{
    /// <summary>Initializes a message-specific view over <paramref name="context" />.</summary>
    /// <param name="context">The publish context to forward.</param>
    /// <param name="message">The message exposed by the typed view.</param>
    /// <exception cref="ArgumentNullException"><paramref name="context" /> or <paramref name="message" /> is <see langword="null" />.</exception>
    public PublishContextProxy(PublishContext context, TMessage message)
        : base(context)
    {
        ArgumentNullException.ThrowIfNull(message);

        Message = message;
    }

    /// <inheritdoc />
    public TMessage Message { get; }
}


/// <summary>Forwards publish operations and metadata to an underlying context.</summary>
public abstract class PublishContextProxy :
    ProxyPipeContext,
    PublishContext
{
    readonly PublishContext _context;

    /// <summary>Initializes a forwarding view over <paramref name="context" />.</summary>
    /// <param name="context">The publish context to forward.</param>
    /// <exception cref="ArgumentNullException"><paramref name="context" /> is <see langword="null" />.</exception>
    protected PublishContextProxy(PublishContext context)
        : base(context ?? throw new ArgumentNullException(nameof(context)))
    {
        _context = context;
    }

    /// <inheritdoc />
    public Uri? SourceAddress
    {
        get => _context.SourceAddress;
        set => _context.SourceAddress = value;
    }

    /// <inheritdoc />
    public Uri? DestinationAddress
    {
        get => _context.DestinationAddress;
        set => _context.DestinationAddress = value;
    }

    /// <inheritdoc />
    public Uri? ResponseAddress
    {
        get => _context.ResponseAddress;
        set => _context.ResponseAddress = value;
    }

    /// <inheritdoc />
    public Uri? FaultAddress
    {
        get => _context.FaultAddress;
        set => _context.FaultAddress = value;
    }

    /// <inheritdoc />
    public Guid? RequestId
    {
        get => _context.RequestId;
        set => _context.RequestId = value;
    }

    /// <inheritdoc />
    public Guid? MessageId
    {
        get => _context.MessageId;
        set => _context.MessageId = value;
    }

    /// <inheritdoc />
    public Guid? CorrelationId
    {
        get => _context.CorrelationId;
        set => _context.CorrelationId = value;
    }

    /// <inheritdoc />
    public Guid? ConversationId
    {
        get => _context.ConversationId;
        set => _context.ConversationId = value;
    }

    /// <inheritdoc />
    public Guid? InitiatorId
    {
        get => _context.InitiatorId;
        set => _context.InitiatorId = value;
    }

    /// <inheritdoc />
    public Guid? ScheduledMessageId
    {
        get => _context.ScheduledMessageId;
        set => _context.ScheduledMessageId = value;
    }

    /// <inheritdoc />
    public SendHeaders Headers => _context.Headers;

    /// <inheritdoc />
    public TimeSpan? TimeToLive
    {
        get => _context.TimeToLive;
        set => _context.TimeToLive = value;
    }

    /// <inheritdoc />
    public DateTimeOffset? SentTime => _context.SentTime;

    /// <inheritdoc />
    public ContentType? ContentType
    {
        get => _context.ContentType;
        set => _context.ContentType = value;
    }

    /// <inheritdoc />
    public bool Durable
    {
        get => _context.Durable;
        set => _context.Durable = value;
    }

    /// <inheritdoc />
    public TimeSpan? Delay
    {
        get => _context.Delay;
        set => _context.Delay = value;
    }

    /// <inheritdoc />
    public IMessageSerializer Serializer
    {
        get => _context.Serializer;
        set => _context.Serializer = value;
    }

    /// <inheritdoc />
    public ISerialization Serialization
    {
        get => _context.Serialization;
        set => _context.Serialization = value;
    }

    /// <inheritdoc />
    public string[] SupportedMessageTypes
    {
        get => _context.SupportedMessageTypes;
        set => _context.SupportedMessageTypes = value;
    }

    /// <inheritdoc />
    public long? BodyLength => _context.BodyLength;

    /// <inheritdoc />
    public SendContext<TMessageContract> CreateProxy<TMessageContract>(TMessageContract message)
        where TMessageContract : class
    {
        ArgumentNullException.ThrowIfNull(message);

        return new PublishContextProxy<TMessageContract>(this, message);
    }

    /// <inheritdoc />
    public bool Mandatory
    {
        get => _context.Mandatory;
        set => _context.Mandatory = value;
    }
}
