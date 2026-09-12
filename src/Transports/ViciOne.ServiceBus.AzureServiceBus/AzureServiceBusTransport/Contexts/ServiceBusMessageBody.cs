using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Owns a stable snapshot of an Azure <see cref="BinaryData"/> message body.</summary>
internal sealed class ServiceBusMessageBody :
    MessageBody
{
    readonly byte[] _content;

    /// <summary>Creates an owned body snapshot from Azure binary data.</summary>
    /// <param name="data">The received message body to snapshot.</param>
    public ServiceBusMessageBody(BinaryData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        _content = data.ToArray();
    }

    /// <summary>Gets the body length in bytes.</summary>
    public long Length => _content.LongLength;

    /// <summary>Copies the body snapshot into a new array.</summary>
    /// <returns>An independently mutable copy of the body.</returns>
    public byte[] ToArray() => (byte[])_content.Clone();

    /// <summary>Creates a read-only stream over the body snapshot.</summary>
    /// <returns>An independently disposable stream positioned at the beginning of the body.</returns>
    public Stream OpenReadStream() => new MemoryStream(_content, false);

    /// <summary>Reports that native binary content has no serializer-defined text representation.</summary>
    /// <param name="text">Always <see langword="null" />.</param>
    /// <returns>Always <see langword="false" />.</returns>
    public bool TryGetTransportText([NotNullWhen(true)] out string? text)
    {
        text = null;
        return false;
    }
}
