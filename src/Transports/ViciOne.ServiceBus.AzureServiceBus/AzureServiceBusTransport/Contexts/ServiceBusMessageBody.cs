using System;
using System.IO;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Exposes an Azure <see cref="BinaryData"/> message body through the transport body abstraction.</summary>
public class ServiceBusMessageBody :
    MessageBody
{
    readonly BinaryData _data;

    /// <summary>Initializes the body from Azure binary data.</summary>
    /// <param name="data">The received message body.</param>
    public ServiceBusMessageBody(BinaryData data)
    {
        _data = data;
    }

    /// <summary>Gets the body length in bytes.</summary>
    public long? Length => _data.ToMemory().Length;

    /// <summary>Creates a readable stream over the body.</summary>
    /// <returns>A stream containing the body bytes.</returns>
    public Stream GetStream()
    {
        return _data.ToStream();
    }

    /// <summary>Copies the body to a byte array.</summary>
    /// <returns>The body bytes.</returns>
    public byte[] GetBytes()
    {
        return _data.ToArray();
    }

    /// <summary>Decodes the body using <see cref="BinaryData"/>'s string representation.</summary>
    /// <returns>The decoded body text.</returns>
    public string GetString()
    {
        return _data.ToString();
    }
}
