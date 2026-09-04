using System;
using Azure.Core;
using Azure.Storage;
using Azure.Storage.Blobs;

namespace ViciOne.ServiceBus.EventHubIntegration.Configuration;

public interface IStorageSettings
{
    string ConnectionString { get; }
    Uri ContainerUri { get; }
    StorageSharedKeyCredential SharedKeyCredential { get; }
    TokenCredential TokenCredential { get; }
    Action<BlobClientOptions> Configure { get; }
}
