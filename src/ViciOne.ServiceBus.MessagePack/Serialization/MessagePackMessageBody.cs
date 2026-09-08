using System;
using System.IO;
using System.Runtime.ExceptionServices;
using MessagePack;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Serialization;

namespace ViciOne.ServiceBus.MessagePack.Serialization;

/// <summary>Lazily serializes a message or transport envelope into MessagePack bytes.</summary>
/// <typeparam name="TMessage">The message contract contained by the body.</typeparam>
internal sealed class MessagePackMessageBody<TMessage> :
    MessageBody
    where TMessage : class
{
    /// <summary>Gets the serialized byte length, materializing the lazy body when first accessed.</summary>
    public long? Length => _lazyMessagePackSerializedObject.Value.Length;

    readonly Lazy<byte[]> _lazyMessagePackSerializedObject;

    /// <summary>Creates a lazy MessagePack transport envelope for a send context.</summary>
    /// <param name="context">The send context that supplies message content and transport metadata.</param>
    /// <param name="envelope">An optional prebuilt envelope whose message body is replaced after payload admission.</param>
    public MessagePackMessageBody(SendContext<TMessage> context, MessagePackEnvelope? envelope = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        _lazyMessagePackSerializedObject = new Lazy<byte[]>(() =>
        {
            if (!context.TryGetPayload(out PayloadAdmissionSerializationContext? admission))
            {
                var unboundedEnvelope = envelope ?? new MessagePackEnvelope(context, context.Message);
                return InternalMessagePackResolver.Serialize(unboundedEnvelope);
            }

            IPayloadSerializationBuffer bodyBuffer = admission.Runtime.CreateSerializedBodyBuffer();
            if (envelope is { IsMessageNativeMessagePackSerialized: true, Message: byte[] serializedMessage })
            {
                serializedMessage.AsSpan().CopyTo(bodyBuffer.GetSpan(serializedMessage.Length));
                bodyBuffer.Advance(serializedMessage.Length);
            }
            else
            {
                object? message = envelope?.Message ?? context.Message;
                SerializeBounded(() =>
                    InternalMessagePackResolver.Serialize(message?.GetType() ?? typeof(object), bodyBuffer, message));
            }

            _ = admission.Runtime.EvaluateSerializedBody(bodyBuffer.WrittenMemory, admission.MessageDataOffloadObserved);

            byte[] boundedBody = bodyBuffer.WrittenMemory.ToArray();
            var envelopeToSerialize = envelope == null
                ? new MessagePackEnvelope(context, boundedBody)
                : new MessagePackEnvelope(envelope, boundedBody);

            IPayloadSerializationBuffer envelopeBuffer = admission.Runtime.CreateTransportEnvelopeBuffer();
            SerializeBounded(() => InternalMessagePackResolver.Serialize(envelopeBuffer, envelopeToSerialize));
            admission.Runtime.ValidateTransportEnvelope(envelopeBuffer.WrittenMemory);

            return envelopeBuffer.WrittenMemory.ToArray();
        });
    }

    /// <summary>Creates a lazy MessagePack body containing only the supplied message.</summary>
    /// <param name="message">The message serialized when the body is first accessed.</param>
    public MessagePackMessageBody(TMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        _lazyMessagePackSerializedObject = new Lazy<byte[]>(() => InternalMessagePackResolver.Serialize(message));
    }

    /// <summary>Opens a non-writable stream over the serialized MessagePack bytes.</summary>
    /// <returns>A readable stream positioned at the beginning of the serialized body.</returns>
    public Stream GetStream()
    {
        return new MemoryStream(_lazyMessagePackSerializedObject.Value, false);
    }

    /// <summary>Gets the lazily serialized MessagePack byte array.</summary>
    /// <returns>The serialized body bytes retained by this instance.</returns>
    public byte[] GetBytes()
    {
        return _lazyMessagePackSerializedObject.Value;
    }

    /// <summary>Gets the serialized MessagePack bytes encoded as Base64 text.</summary>
    /// <returns>The Base64 representation of the serialized body.</returns>
    public string GetString()
    {
        return Convert.ToBase64String(_lazyMessagePackSerializedObject.Value);
    }

    static void SerializeBounded(Action serialize)
    {
        try
        {
            serialize();
        }
        catch (MessagePackSerializationException exception)
            when (exception.InnerException is PayloadAdmissionException)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }
}
