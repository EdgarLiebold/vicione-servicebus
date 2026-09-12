using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.ExceptionServices;
using MessagePack;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Serialization;

namespace ViciOne.ServiceBus.MessagePack.Serialization;

/// <summary>Owns one serialized MessagePack message or transport envelope.</summary>
/// <typeparam name="TMessage">The message contract contained by the body.</typeparam>
internal sealed class MessagePackMessageBody<TMessage> :
    MessageBody
    where TMessage : class
{
    readonly byte[] _content;

    /// <summary>Creates an owned MessagePack transport-envelope snapshot.</summary>
    /// <param name="context">The send context that supplies message content and transport metadata.</param>
    /// <param name="envelope">An optional prebuilt envelope whose message body is replaced after payload admission.</param>
    public MessagePackMessageBody(SendContext<TMessage> context, MessagePackEnvelope? envelope = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        _content = Serialize(context, envelope);
    }

    /// <summary>Creates an owned MessagePack snapshot containing only the supplied message.</summary>
    /// <param name="message">The message to serialize.</param>
    public MessagePackMessageBody(TMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        _content = MessagePackSerializationRuntime.Serialize(message);
    }

    /// <summary>Gets the serialized byte length.</summary>
    public long Length => _content.LongLength;

    /// <summary>Copies the serialized MessagePack content into a new array.</summary>
    /// <returns>An independently mutable copy of the serialized content.</returns>
    public byte[] ToArray() => (byte[])_content.Clone();

    /// <summary>Opens a new read-only stream over the serialized MessagePack content.</summary>
    /// <returns>An independently disposable stream positioned at the beginning of the body.</returns>
    public Stream OpenReadStream() => new MemoryStream(_content, false);

    /// <summary>Tries to get the Base64 carrier used by text-only transports.</summary>
    /// <param name="text">The lossless Base64 representation of the serialized content.</param>
    /// <returns>Always <see langword="true" />.</returns>
    public bool TryGetTransportText([NotNullWhen(true)] out string? text)
    {
        text = Convert.ToBase64String(_content);
        return true;
    }

    static byte[] Serialize(SendContext<TMessage> context, MessagePackEnvelope? envelope)
    {
        if (!context.TryGetPayload(out PayloadAdmissionSerializationContext? admission))
        {
            var unboundedEnvelope = envelope ?? new MessagePackEnvelope(context, context.Message);
            return MessagePackSerializationRuntime.Serialize(unboundedEnvelope);
        }

        IPayloadSerializationBuffer bodyBuffer = admission.Runtime.CreateSerializedBodyBuffer();
        if (envelope?.Message is { } serializedMessage)
        {
            serializedMessage.AsSpan().CopyTo(bodyBuffer.GetSpan(serializedMessage.Length));
            bodyBuffer.Advance(serializedMessage.Length);
        }
        else
            SerializeBounded(() =>
                MessagePackSerializationRuntime.Serialize(context.Message.GetType(), bodyBuffer, context.Message));

        _ = admission.Runtime.EvaluateSerializedBody(bodyBuffer.WrittenMemory, admission.MessageDataOffloadObserved);

        byte[] boundedBody = bodyBuffer.WrittenMemory.ToArray();
        var envelopeToSerialize = envelope == null
            ? new MessagePackEnvelope(context, boundedBody)
            : new MessagePackEnvelope(
                envelope,
                boundedBody,
                envelope.IsNativeMessagePackPayload);

        IPayloadSerializationBuffer envelopeBuffer = admission.Runtime.CreateTransportEnvelopeBuffer();
        SerializeBounded(() => MessagePackSerializationRuntime.Serialize(envelopeBuffer, envelopeToSerialize));
        admission.Runtime.ValidateTransportEnvelope(envelopeBuffer.WrittenMemory);

        return envelopeBuffer.WrittenMemory.ToArray();
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
        }
    }
}
