using System;
using Azure.Core;
using Azure.Storage;
using Azure.Storage.Blobs;

namespace ViciOne.ServiceBus.EventHubs.Configuration;

/// <summary>
/// Provides a storage settings implementation.
/// </summary>
public class StorageSettings :
    IStorageSettings
{
    /// <summary>
    /// Gets or sets the connection string value.
    /// </summary>
    public string? ConnectionString { get; set; }
    /// <summary>
    /// Gets or sets the container uri value.
    /// </summary>
    public Uri? ContainerUri { get; set; }
    /// <summary>
    /// Gets or sets the shared key credential value.
    /// </summary>
    public StorageSharedKeyCredential? SharedKeyCredential { get; set; }
    /// <summary>
    /// Gets or sets the token credential value.
    /// </summary>
    public TokenCredential? TokenCredential { get; set; }
    /// <summary>
    /// Gets or sets the configure value.
    /// </summary>
    public Action<BlobClientOptions>? Configure { get; set; }
}
