using Azure.Storage.Blobs;
using ViciOne.ServiceBus.AzureStorage.MessageData;

namespace ViciOne.ServiceBus;

public static class AzureStorageConfigurationExtensions
{
    public static AzureStorageMessageDataRepository CreateMessageDataRepository(this BlobServiceClient client, string containerName, bool compress = false)
    {
        return new AzureStorageMessageDataRepository(client, containerName, compress);
    }
}
