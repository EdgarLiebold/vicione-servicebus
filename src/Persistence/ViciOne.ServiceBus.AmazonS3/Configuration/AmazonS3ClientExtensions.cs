// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus;

using Amazon.S3;
using AmazonS3.MessageData;


public static class AmazonS3ClientExtensions
{
    public static AmazonS3MessageDataRepository CreateMessageDataRepository(this AmazonS3Client client, string bucket)
    {
        return new AmazonS3MessageDataRepository(client, bucket);
    }
}
