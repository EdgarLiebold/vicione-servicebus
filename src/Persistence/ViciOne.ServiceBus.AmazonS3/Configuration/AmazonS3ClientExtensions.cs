using System;
using Amazon.S3;
using ViciOne.ServiceBus.AmazonS3.MessageData;

namespace ViciOne.ServiceBus.AmazonS3;

/// <summary>
/// Provides extension methods for amazon s3 client.
/// </summary>
public static class AmazonS3ClientExtensions
{
    /// <summary>
    /// Creates message data repository.
    /// </summary>
    /// <param name="client">The client value.</param>
    /// <param name="options">The options value.</param>
    /// <returns>The result of the operation.</returns>
    public static AmazonS3MessageDataRepository CreateMessageDataRepository(
        this IAmazonS3 client,
        AmazonS3MessageDataRepositoryOptions options)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(options);

        return new AmazonS3MessageDataRepository(client, options);
    }
}
