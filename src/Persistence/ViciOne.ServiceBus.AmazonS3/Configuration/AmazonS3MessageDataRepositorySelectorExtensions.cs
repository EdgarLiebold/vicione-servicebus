using System;
using Amazon.S3;
using ViciOne.ServiceBus.AmazonS3.MessageData;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.AmazonS3;

/// <summary>Adds Amazon S3 message-data storage to the repository selection API.</summary>
public static class AmazonS3MessageDataRepositorySelectorExtensions
{
    /// <summary>Uses a caller-owned Amazon S3 client for message-data storage.</summary>
    /// <param name="selector">The repository selector being configured.</param>
    /// <param name="client">The caller-owned Amazon S3 client used by the repository.</param>
    /// <param name="options">The validated bucket and lifecycle settings.</param>
    /// <returns>A repository backed by the supplied Amazon S3 client.</returns>
    public static IMessageDataRepository AmazonS3(
        this IMessageDataRepositorySelector selector,
        IAmazonS3 client,
        AmazonS3MessageDataRepositoryOptions options)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(options);

        return new AmazonS3MessageDataRepository(client, options);
    }
}
