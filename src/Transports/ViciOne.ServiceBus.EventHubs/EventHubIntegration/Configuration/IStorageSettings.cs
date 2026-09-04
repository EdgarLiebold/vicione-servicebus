using System;
using Azure.Core;
using Azure.Storage;
using Azure.Storage.Blobs;

namespace ViciOne.ServiceBus.EventHubs.Configuration;

/// <summary>
/// Defines the contract for storage settings.
/// </summary>
public interface IStorageSettings
{
    /// <summary>
    /// Gets the connection string value.
    /// </summary>
    string? ConnectionString { get; }
    /// <summary>
    /// Gets the container uri value.
    /// </summary>
    Uri? ContainerUri { get; }
    /// <summary>
    /// Gets the shared key credential value.
    /// </summary>
    StorageSharedKeyCredential? SharedKeyCredential { get; }
    /// <summary>
    /// Gets the token credential value.
    /// </summary>
    TokenCredential? TokenCredential { get; }
    /// <summary>
    /// Gets the configure value.
    /// </summary>
    Action<BlobClientOptions>? Configure { get; }
}
