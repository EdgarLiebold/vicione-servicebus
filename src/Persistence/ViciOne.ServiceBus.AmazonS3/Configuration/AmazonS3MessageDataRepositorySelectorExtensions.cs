using System;
using Amazon.S3;
using ViciOne.ServiceBus.AmazonS3.MessageData;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.AmazonS3;

/// <summary>
/// Provides extension methods for amazon s3 message data repository selector.
/// </summary>
public static class AmazonS3MessageDataRepositorySelectorExtensions
{
    /// <summary>Uses a caller-owned Amazon S3 client for message-data storage.</summary>
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
