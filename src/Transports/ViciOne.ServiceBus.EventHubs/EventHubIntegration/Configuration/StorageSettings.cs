using System;
using Azure.Core;
using Azure.Storage;
using Azure.Storage.Blobs;

namespace ViciOne.ServiceBus.EventHubs.Configuration;

/// <summary>Stores Blob Storage connection, credential, and client settings for Event Hubs checkpoints.</summary>
public class StorageSettings :
    IStorageSettings
{
    /// <summary>Gets or sets the Azure Storage connection string.</summary>
    public string? ConnectionString { get; set; }
    /// <summary>Gets or sets the Blob Storage URI used to construct the checkpoint client.</summary>
    public Uri? ContainerUri { get; set; }
    /// <summary>Gets or sets the storage account shared-key credential.</summary>
    public StorageSharedKeyCredential? SharedKeyCredential { get; set; }
    /// <summary>Gets or sets the Azure token credential.</summary>
    public TokenCredential? TokenCredential { get; set; }
    /// <summary>Gets or sets the callback applied to Blob client options.</summary>
    public Action<BlobClientOptions>? Configure { get; set; }
}
