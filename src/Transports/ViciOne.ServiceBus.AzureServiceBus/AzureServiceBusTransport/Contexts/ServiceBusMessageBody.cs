using System;
using System.IO;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Provides a service bus message body implementation.
/// </summary>
public class ServiceBusMessageBody :
    MessageBody
{
    readonly BinaryData _data;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="data">The data value.</param>
    public ServiceBusMessageBody(BinaryData data)
    {
        _data = data;
    }

    /// <summary>
    /// Gets the length value.
    /// </summary>
    public long? Length => _data.ToMemory().Length;

    /// <summary>
    /// Gets stream.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public Stream GetStream()
    {
        return _data.ToStream();
    }

    /// <summary>
    /// Gets bytes.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public byte[] GetBytes()
    {
        return _data.ToArray();
    }

    /// <summary>
    /// Gets string.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public string GetString()
    {
        return _data.ToString();
    }
}
