using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Owns an isolated snapshot of serialized binary content.</summary>
public sealed class BinaryMessageBody :
    MessageBody
{
    readonly byte[] _content;

    /// <summary>Creates a body by copying the selected content.</summary>
    /// <param name="content">The complete serialized content.</param>
    public BinaryMessageBody(ReadOnlyMemory<byte> content)
    {
        _content = content.ToArray();
    }

    /// <summary>Gets the serialized content length in bytes.</summary>
    public long Length => _content.LongLength;

    /// <summary>Copies the complete serialized content into a new array.</summary>
    /// <returns>An independently mutable copy of the serialized content.</returns>
    public byte[] ToArray() => (byte[])_content.Clone();

    /// <summary>Opens a new read-only stream over the owned content.</summary>
    /// <returns>An independently disposable stream positioned at the beginning of the body.</returns>
    public Stream OpenReadStream() => new MemoryStream(_content, false);

    /// <summary>Reports that opaque binary content has no serializer-defined text representation.</summary>
    /// <param name="text">Always <see langword="null" />.</param>
    /// <returns>Always <see langword="false" />.</returns>
    public bool TryGetTransportText([NotNullWhen(true)] out string? text)
    {
        text = null;
        return false;
    }

}
