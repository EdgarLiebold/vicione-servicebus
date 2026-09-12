using System.Text;
using Amazon.S3.Model;
using Amazon.S3.Util;
using ViciOne.ServiceBus.AmazonS3.MessageData;
using ViciOne.ServiceBus.AmazonSqs.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.MessageData.Values;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.LocalIntegration.Tests;

public sealed class AmazonSqsMessageDataTests
{
    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0228", "s3-backed-payload-round-trips-through-sqs")]
    public async Task S3BackedPayload_RoundTripsThroughSqsAsync()
    {
        await using AmazonSqsLocalStack fixture = AmazonSqsLocalStack.Create("message-data");
        string queueName = fixture.Name("input");
        string bucketName = fixture.BucketName("payload");
        string expected = string.Concat(Enumerable.Repeat("ViciOne-ÄΩ-0123456789|", 350));
        var repository = new AmazonS3MessageDataRepository(
            fixture.S3Client,
            new AmazonS3MessageDataRepositoryOptions(bucketName));
        var handled = new TaskCompletionSource<MessageDataObservation>(TaskCreationOptions.RunContinuationsAsynchronously);
        IBusControl bus = Bus.Factory.CreateUsingAmazonSqs(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.UseMessageData(_ => repository);
            configurator.ReceiveEndpoint(queueName, endpoint => endpoint.Handler<S3PayloadMessage>(async context =>
            {
                try
                {
                    MessageData<string> payload = context.Message.Payload;
                    string value = Assert.IsType<string>(
                        await payload.Value.WaitAsync(fixture.OperationTimeout, context.CancellationToken));
                    Uri address = Assert.IsType<Uri>(payload.Address);
                    handled.TrySetResult(new MessageDataObservation(address, value));
                }
                catch (Exception exception)
                {
                    handled.TrySetException(exception);
                    throw;
                }
            }));
        });
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            Assert.False(await AmazonS3Util
                .DoesS3BucketExistV2Async(fixture.S3Client, bucketName)
                .WaitAsync(fixture.OperationTimeout, cancellationToken));

            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            Assert.True(await AmazonS3Util
                .DoesS3BucketExistV2Async(fixture.S3Client, bucketName)
                .WaitAsync(fixture.OperationTimeout, cancellationToken));

            await bus.PublishAsync(
                    new S3PayloadMessage(ViciOne.ServiceBus.Advanced.MessageData.FromValue(expected)),
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            MessageDataObservation observation = await handled.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(expected, observation.Value);
            Assert.Equal("s3", observation.Address.Scheme);
            Assert.Equal(bucketName, observation.Address.IdnHost);
            Assert.True(observation.Address.IsDefaultPort);
            Assert.Empty(observation.Address.Query);
            Assert.Empty(observation.Address.Fragment);
            string objectKey = observation.Address.AbsolutePath.TrimStart('/');
            ListObjectsV2Response objects = await fixture.S3Client.ListObjectsV2Async(
                    new ListObjectsV2Request { BucketName = bucketName },
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            S3Object storedObject = Assert.Single(objects.S3Objects);
            Assert.Equal(objectKey, storedObject.Key);
            using GetObjectResponse stored = await fixture.S3Client.GetObjectAsync(
                    bucketName,
                    objectKey,
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            using var bytes = new MemoryStream();
            await stored.ResponseStream.CopyToAsync(bytes, cancellationToken);
            Assert.Equal(Encoding.UTF8.GetBytes(expected), bytes.ToArray());
        }
        finally
        {
            if (started)
                await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
    }

    public sealed record S3PayloadMessage(MessageData<string> Payload);

    private sealed record MessageDataObservation(Uri Address, string Value);
}
