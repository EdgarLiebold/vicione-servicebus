// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.EventHubIntegration.Configuration
{
    using System;
    using Azure.Core;
    using Azure.Storage;
    using Azure.Storage.Blobs;


    public interface IStorageSettings
    {
        string ConnectionString { get; }
        Uri ContainerUri { get; }
        StorageSharedKeyCredential SharedKeyCredential { get; }
        TokenCredential TokenCredential { get; }
        Action<BlobClientOptions> Configure { get; }
    }
}
