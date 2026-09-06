using System;
using Azure.Core;
using Azure.Storage;
using Azure.Storage.Blobs;

namespace ViciOne.ServiceBus.EventHubs.Configuration;

/// <summary>Provides Blob Storage settings used for Event Hubs partition ownership and checkpoints.</summary>
public interface IStorageSettings
{
    /// <summary>Gets the Azure Storage connection string, when connection-string authentication is configured.</summary>
    string? ConnectionString { get; }
    /// <summary>Gets the Blob Storage URI configured for checkpoint storage.</summary>
    Uri? ContainerUri { get; }
    /// <summary>Gets the storage account shared-key credential, when configured.</summary>
    StorageSharedKeyCredential? SharedKeyCredential { get; }
    /// <summary>Gets the Azure token credential, when configured.</summary>
    TokenCredential? TokenCredential { get; }
    /// <summary>Gets the optional callback applied to Blob client options.</summary>
    Action<BlobClientOptions>? Configure { get; }
}
