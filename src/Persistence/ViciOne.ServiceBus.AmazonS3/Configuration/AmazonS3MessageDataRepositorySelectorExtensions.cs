namespace ViciOne.ServiceBus;

using System;
using Amazon.S3;
using AmazonS3.MessageData;
using Configuration;


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
