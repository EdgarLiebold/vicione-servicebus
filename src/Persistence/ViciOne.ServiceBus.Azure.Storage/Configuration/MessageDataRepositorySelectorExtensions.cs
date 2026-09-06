using System;
using Azure.Storage.Blobs;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Azure.Storage;

/// <summary>Adds Azure Blob Storage to the message-data repository selection API.</summary>
public static class MessageDataRepositorySelectorExtensions
{
    /// <summary>Use Azure Blob Storage for message data storage.</summary>
    /// <param name="selector">The repository selector being configured.</param>
    /// <param name="connectionString">The connection string used to create the blob service client.</param>
    /// <param name="containerName">The container name, or <see langword="null"/> to use <c>message-data</c>.</param>
    /// <param name="compress">Whether payloads are GZip-compressed before upload.</param>
    /// <returns>A repository backed by the configured Azure Blob Storage container.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <see langword="null" />.</exception>
    public static IMessageDataRepository AzureStorage(this IMessageDataRepositorySelector selector, string connectionString, string? containerName = default, bool compress = false)
    {
        if (selector is null)
            throw new ArgumentNullException(nameof(selector));

        if (string.IsNullOrEmpty(connectionString))
            throw new ArgumentNullException(nameof(connectionString));

        var client = new BlobServiceClient(connectionString);

        var repository = client.CreateMessageDataRepository(containerName ?? "message-data", compress);

        return repository;
    }
}
