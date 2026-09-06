using System;
using Amazon.S3;
using ViciOne.ServiceBus.AmazonS3.MessageData;

namespace ViciOne.ServiceBus.AmazonS3;

/// <summary>Creates message-data repositories from caller-owned Amazon S3 clients.</summary>
public static class AmazonS3ClientExtensions
{
    /// <summary>Creates a message-data repository that uses the supplied client and bucket settings.</summary>
    /// <param name="client">The caller-owned Amazon S3 client used by the repository.</param>
    /// <param name="options">The validated bucket and lifecycle settings.</param>
    /// <returns>A repository backed by the supplied Amazon S3 client.</returns>
    public static AmazonS3MessageDataRepository CreateMessageDataRepository(
        this IAmazonS3 client,
        AmazonS3MessageDataRepositoryOptions options)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(options);

        return new AmazonS3MessageDataRepository(client, options);
    }
}
