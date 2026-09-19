using System;
using System.Buffers;
using System.IO;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Writes an application body and its transport envelope to separately bounded, bus-owned buffers.</summary>
/// <remarks>
/// This optional serializer contract is used when a bus enforces payload admission. The bus invokes
/// <see cref="WriteSerializedBody{T}"/> once, admits those exact bytes, and only then invokes
/// <see cref="WriteTransportEnvelope{T}"/>. Serializers must not retain either writer or the body stream.
/// The admitted body must occur as one unchanged, contiguous byte range in the final envelope.
/// Formats that transform or encrypt the body while enclosing it require a different admission contract.
/// </remarks>
public interface IBoundedMessageSerializer : IMessageSerializer
{
    /// <summary>Gets the lossless text representation required by text-only transports.</summary>
    SerializedTransportTextFormat TransportTextFormat { get; }

    /// <summary>Writes the serialized application message, without transport-envelope metadata.</summary>
    /// <typeparam name="T">The outgoing message contract.</typeparam>
    /// <param name="context">The outgoing message and metadata.</param>
    /// <param name="writer">The bus-owned writer bounded by the application-body limit.</param>
    void WriteSerializedBody<T>(SendContext<T> context, IBufferWriter<byte> writer)
        where T : class;

    /// <summary>Writes the final transport envelope from the already admitted application body.</summary>
    /// <typeparam name="T">The outgoing message contract.</typeparam>
    /// <param name="context">The outgoing message and metadata.</param>
    /// <param name="serializedBody">A read-only, seekable stream over the admitted body, positioned at zero.</param>
    /// <param name="writer">The bus-owned writer bounded by the transport-envelope limit.</param>
    void WriteTransportEnvelope<T>(SendContext<T> context, Stream serializedBody, IBufferWriter<byte> writer)
        where T : class;

    /// <summary>Locates the unchanged application-body bytes in the final transport envelope.</summary>
    /// <param name="serializedEnvelope">The exact final envelope bytes owned by the bus.</param>
    /// <param name="offset">The byte offset of the contiguous application body.</param>
    /// <param name="length">The length of the contiguous application body.</param>
    /// <returns><see langword="true"/> only when one unambiguous body range was found.</returns>
    bool TryLocateSerializedBody(ReadOnlySpan<byte> serializedEnvelope, out int offset, out int length);
}
