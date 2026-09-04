using Azure.Storage.Blobs;
using ViciOne.ServiceBus.AzureStorage.MessageData;

namespace ViciOne.ServiceBus.Azure.Storage;

/// <summary>
/// Provides extension methods for azure storage configuration.
/// </summary>
public static class AzureStorageConfigurationExtensions
{
    /// <summary>
    /// Creates message data repository.
    /// </summary>
    /// <param name="client">The client value.</param>
    /// <param name="containerName">The container name value.</param>
    /// <param name="compress">The compress value.</param>
    /// <returns>The result of the operation.</returns>
    public static AzureStorageMessageDataRepository CreateMessageDataRepository(this BlobServiceClient client, string containerName, bool compress = false)
    {
        return new AzureStorageMessageDataRepository(client, containerName, compress);
    }
}
