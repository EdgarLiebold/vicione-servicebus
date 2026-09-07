using System;
using Azure.Storage.Blobs;
using ViciOne.ServiceBus.Azure.Storage.MessageData;

namespace ViciOne.ServiceBus.Azure.Storage;

/// <summary>Creates message-data repositories from caller-owned Azure Blob Storage service clients.</summary>
public static class BlobServiceClientExtensions
{
    /// <summary>Creates a repository that stores message payloads in the named blob container.</summary>
    /// <param name="client">The caller-owned blob service client.</param>
    /// <param name="containerName">The non-empty container name that stores message payloads.</param>
    /// <param name="compress">Whether new payloads are GZip-compressed before upload.</param>
    /// <param name="timeProvider">The clock used to calculate expiration metadata, or <see langword="null"/> to use system time.</param>
    /// <returns>A repository backed by the selected Azure Blob Storage container.</returns>
    public static AzureBlobMessageDataRepository CreateMessageDataRepository(
        this BlobServiceClient client,
        string containerName,
        bool compress = false,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(containerName);

        return new AzureBlobMessageDataRepository(
            client.GetBlobContainerClient(containerName),
            compress,
            timeProvider);
    }
}
