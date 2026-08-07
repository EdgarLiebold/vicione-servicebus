// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using Azure.Storage.Blobs;
    using AzureStorage.MessageData;


    public static class AzureStorageConfigurationExtensions
    {
        public static AzureStorageMessageDataRepository CreateMessageDataRepository(this BlobServiceClient client, string containerName, bool compress = false)
        {
            return new AzureStorageMessageDataRepository(client, containerName, compress);
        }
    }
}
