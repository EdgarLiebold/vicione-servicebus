namespace ViciOne.ServiceBus;

using System;
using Amazon.S3;
using AmazonS3.MessageData;


public static class AmazonS3ClientExtensions
{
    public static AmazonS3MessageDataRepository CreateMessageDataRepository(
        this IAmazonS3 client,
        AmazonS3MessageDataRepositoryOptions options)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(options);

        return new AmazonS3MessageDataRepository(client, options);
    }
}
