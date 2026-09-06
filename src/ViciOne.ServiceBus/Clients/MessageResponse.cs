using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Clients;

/// <summary>A result from a request.</summary>
/// <typeparam name="TResult">The result produced by the operation.</typeparam>
public class MessageResponse<TResult> :
    Response<TResult>
    where TResult : class
{
    readonly ConsumeContext<TResult> _context;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public MessageResponse(ConsumeContext<TResult> context)
    {
        _context = context;

        Message = context.Message;
    }

    Guid? MessageContext.MessageId => _context.MessageId;
    Guid? MessageContext.RequestId => _context.RequestId;
    Guid? MessageContext.CorrelationId => _context.CorrelationId;
    Guid? MessageContext.ConversationId => _context.ConversationId;
    Guid? MessageContext.InitiatorId => _context.InitiatorId;
    DateTimeOffset? MessageContext.ExpirationTime => _context.ExpirationTime;
    Uri? MessageContext.SourceAddress => _context.SourceAddress;
    Uri? MessageContext.DestinationAddress => _context.DestinationAddress;
    Uri? MessageContext.ResponseAddress => _context.ResponseAddress;
    Uri? MessageContext.FaultAddress => _context.FaultAddress;
    DateTimeOffset? MessageContext.SentTime => _context.SentTime;
    Headers MessageContext.Headers => _context.Headers;
    HostInfo MessageContext.Host => _context.Host;

    /// <summary>Gets the message.</summary>
    public TResult Message { get; }
    object Response.Message => Message;

    /// <summary>Deserializes object.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="dictionary">The dictionary.</param>
    /// <returns>The deserialized object.</returns>
    public T? DeserializeObject<T>(Dictionary<string, object> dictionary)
        where T : class
    {
        return _context.Advanced().SerializerContext.DeserializeObject<T>(dictionary);
    }
}
