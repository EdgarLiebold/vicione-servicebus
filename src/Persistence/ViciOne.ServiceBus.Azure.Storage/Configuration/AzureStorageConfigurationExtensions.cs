using Azure.Storage.Blobs;
using ViciOne.ServiceBus.AzureStorage.MessageData;

namespace ViciOne.ServiceBus.Azure.Storage;

/// <summary>Creates Azure Blob Storage message-data repositories from caller-owned clients.</summary>
public static class AzureStorageConfigurationExtensions
{
    /// <summary>Creates a repository that stores message payloads in the named blob container.</summary>
    /// <param name="client">The caller-owned blob service client.</param>
    /// <param name="containerName">The container that stores message payloads.</param>
    /// <param name="compress">Whether payloads are GZip-compressed before upload.</param>
    /// <returns>A repository backed by the supplied Azure Blob Storage client.</returns>
    public static AzureStorageMessageDataRepository CreateMessageDataRepository(this BlobServiceClient client, string containerName, bool compress = false)
    {
        return new AzureStorageMessageDataRepository(client, containerName, compress);
    }
}
