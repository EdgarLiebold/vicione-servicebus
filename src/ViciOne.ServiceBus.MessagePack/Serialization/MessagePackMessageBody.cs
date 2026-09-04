using System;
using System.IO;
using System.Runtime.ExceptionServices;
using MessagePack;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>
/// Provides a message pack message body implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class MessagePackMessageBody<TMessage> :
    MessageBody
    where TMessage : class
{
    /// <summary>
    /// Gets the length value.
    /// </summary>
    public long? Length => _lazyMessagePackSerializedObject.Value.Length;

    readonly Lazy<byte[]> _lazyMessagePackSerializedObject;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="envelope">The envelope value.</param>
    public MessagePackMessageBody(SendContext<TMessage> context, MessagePackEnvelope? envelope = null)
    {
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

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    public MessagePackMessageBody(TMessage message)
    {
        _lazyMessagePackSerializedObject = new Lazy<byte[]>(() => InternalMessagePackResolver.Serialize(message));
    }

    /// <summary>
    /// Gets stream.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public Stream GetStream()
    {
        return new MemoryStream(_lazyMessagePackSerializedObject.Value, false);
    }

    /// <summary>
    /// Gets bytes.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public byte[] GetBytes()
    {
        return _lazyMessagePackSerializedObject.Value;
    }

    /// <summary>
    /// Gets string.
    /// </summary>
    /// <returns>The result of the operation.</returns>
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
            // MessagePack wraps formatter failures. Payload admission is a transport policy result,
            // so preserve the original instance and stage instead of changing its public contract.
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }
}
