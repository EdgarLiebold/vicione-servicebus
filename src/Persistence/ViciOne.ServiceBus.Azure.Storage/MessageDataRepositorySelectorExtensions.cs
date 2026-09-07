using System;
using Azure.Storage.Blobs;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Azure.Storage;

/// <summary>Adds Azure Blob Storage to the message-data repository selection API.</summary>
public static class MessageDataRepositorySelectorExtensions
{
    /// <summary>Uses Azure Blob Storage for message-data storage.</summary>
    /// <param name="selector">The repository selector being configured.</param>
    /// <param name="connectionString">The connection string used to create the blob service client.</param>
    /// <param name="containerName">The non-empty container name.</param>
    /// <param name="compress">Whether new payloads are GZip-compressed before upload.</param>
    /// <returns>A repository backed by the configured Azure Blob Storage container.</returns>
    public static IMessageDataRepository UseAzureBlobStorage(
        this IMessageDataRepositorySelector selector,
        string connectionString,
        string containerName = "message-data",
        bool compress = false)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(containerName);

        var client = new BlobServiceClient(connectionString);
        return client.CreateMessageDataRepository(containerName, compress);
    }
}
