namespace ViciOne.ServiceBus.Serialization;

using System.Buffers;
using System;

/// <summary>
/// A serialization target whose owned-memory growth is bounded by a configured hard maximum.
/// </summary>
public interface IPayloadSerializationBuffer : IBufferWriter<byte>
{
    /// <summary>Gets the number of bytes committed by the serializer.</summary>
    int WrittenCount { get; }

    /// <summary>Gets only the bytes committed by the serializer.</summary>
    ReadOnlyMemory<byte> WrittenMemory { get; }
}
