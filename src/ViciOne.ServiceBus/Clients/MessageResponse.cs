using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Clients;

/// <summary>Provides a request response together with its transport metadata and serializer context.</summary>
/// <typeparam name="TResult">The response contract type.</typeparam>
internal sealed class MessageResponse<TResult> :
    Response<TResult>
    where TResult : class
{
    readonly ConsumeContext<TResult> _context;

    /// <summary>Creates a response backed by its receive context.</summary>
    /// <param name="context">The received response and its transport metadata.</param>
    public MessageResponse(ConsumeContext<TResult> context)
    {
        ArgumentNullException.ThrowIfNull(context);

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

    /// <summary>Gets the received response message.</summary>
    public TResult Message { get; }
    object Response.Message => Message;

    /// <summary>Deserializes a dictionary payload with the serializer that received the response.</summary>
    /// <typeparam name="TValue">The expected object type.</typeparam>
    /// <param name="dictionary">The serialized object properties.</param>
    /// <returns>The deserialized object, or <see langword="null" /> when the payload represents no value.</returns>
    public TValue? DeserializeObject<TValue>(IReadOnlyDictionary<string, object> dictionary)
        where TValue : class
    {
        ArgumentNullException.ThrowIfNull(dictionary);

        return _context.Advanced().SerializerContext.DeserializeObject<TValue>(dictionary);
    }
}
