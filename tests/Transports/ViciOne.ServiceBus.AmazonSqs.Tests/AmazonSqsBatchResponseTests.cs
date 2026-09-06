using System.Net;
using global::Amazon.Runtime;
using global::Amazon.SimpleNotificationService;
using global::Amazon.SQS;
using global::Amazon.SQS.Model;
using ViciOne.ServiceBus.AmazonSqs;
using ViciOne.ServiceBus.AmazonSqs.Tests.TestDoubles;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;
using SnsMessageAttributeValue = global::Amazon.SimpleNotificationService.Model.MessageAttributeValue;
using SnsPublishBatchRequestEntry = global::Amazon.SimpleNotificationService.Model.PublishBatchRequestEntry;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

public sealed class AmazonSqsBatchResponseTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-BATCH", "every-request-entry-accounted-for")]
    public async Task PartialBatchResponse_FaultsEveryUnaccountedRequestAsync()
    {
        using var client = new PartialResponseSqsClient();
        await using var batcher = new SendBatcher(
            client,
            "http://127.0.0.1/queue/orders",
            TestContext.Current.CancellationToken,
            new FixedBatchSettings());

        Task first = batcher.ExecuteAsync(new SendMessageBatchRequestEntry("", "first"), TestContext.Current.CancellationToken);
        Task second = batcher.ExecuteAsync(new SendMessageBatchRequestEntry("", "second"), TestContext.Current.CancellationToken);

        AmazonSqsTransportException firstFailure = await Assert.ThrowsAsync<AmazonSqsTransportException>(() => first);
        AmazonSqsTransportException secondFailure = await Assert.ThrowsAsync<AmazonSqsTransportException>(() => second);

        Assert.Equal(2, client.RequestEntryCount);
        Assert.Contains("accounted for 1 of 2", firstFailure.Message, StringComparison.Ordinal);
        Assert.Contains("accounted for 1 of 2", secondFailure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-BATCH", "immutable-process-defaults")]
    public void BatchDefaults_AreImmutableSnapshots()
    {
        BatchSettings first = ClientContextBatchSettings.GetBatchSettings();
        BatchSettings second = ClientContextBatchSettings.GetBatchSettings();
        BatchSettings publish = PublishBatchSettings.GetBatchSettings();

        Assert.Same(first, second);
        Assert.Equal(10, first.MessageLimit);
        Assert.Equal(10, first.BatchLimit);
        Assert.Equal(240 * 1024, first.SizeLimit);
        Assert.Equal(TimeSpan.FromMilliseconds(1), first.Timeout);
        Assert.Equal(10, publish.MessageLimit);
        Assert.All(
            first.GetType().GetProperties(),
            property => Assert.False(property.CanWrite, property.Name));
        Assert.All(
            publish.GetType().GetProperties(),
            property => Assert.False(property.CanWrite, property.Name));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-BATCH", "entry-size-counts-every-attribute-component")]
    public async Task SqsEntryLength_CountsUtf8NamesDataTypesStringAndBinaryValuesAsync()
    {
        IAmazonSQS client = InterfaceProxy<IAmazonSQS>.Create((method, _) => Default(method.ReturnType));
        await using var batcher = new InspectableSendBatcher(client);
        var entry = new SendMessageBatchRequestEntry("", "ä")
        {
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                ["name"] = new MessageAttributeValue { DataType = "String.custom", StringValue = "é" },
                ["κ"] = new MessageAttributeValue { DataType = "Binary.png", BinaryValue = new MemoryStream([1, 2, 3]) },
            }
        };

        int length = batcher.EntryLength(entry);

        Assert.Equal(36, length);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SNS-BATCH", "entry-size-counts-every-attribute-component")]
    public async Task SnsEntryLength_CountsUtf8NamesDataTypesStringAndBinaryValuesAsync()
    {
        IAmazonSimpleNotificationService client = InterfaceProxy<IAmazonSimpleNotificationService>.Create(
            (method, _) => Default(method.ReturnType));
        await using var batcher = new InspectablePublishBatcher(client);
        var entry = new SnsPublishBatchRequestEntry
        {
            Message = "ä",
            MessageAttributes = new Dictionary<string, SnsMessageAttributeValue>
            {
                ["name"] = new SnsMessageAttributeValue { DataType = "String.custom", StringValue = "é" },
                ["κ"] = new SnsMessageAttributeValue { DataType = "Binary.png", BinaryValue = new MemoryStream([1, 2, 3]) },
            }
        };

        int length = batcher.EntryLength(entry);

        Assert.Equal(36, length);
    }

    private static object? Default(Type returnType) => returnType.IsValueType ? Activator.CreateInstance(returnType) : null;

    private sealed class FixedBatchSettings : BatchSettings
    {
        public int MessageLimit => 2;
        public int BatchLimit => 1;
        public int SizeLimit => 256 * 1024;
        public TimeSpan Timeout => TimeSpan.FromSeconds(5);
    }

    private sealed class InspectableSendBatcher(IAmazonSQS client)
        : SendBatcher(client, "http://127.0.0.1/queue/orders", CancellationToken.None)
    {
        public int EntryLength(SendMessageBatchRequestEntry entry) => CalculateEntryLength(entry);
    }

    private sealed class InspectablePublishBatcher(IAmazonSimpleNotificationService client)
        : PublishBatcher(client, "arn:aws:sns:eu-central-1:123456789012:orders", CancellationToken.None)
    {
        public int EntryLength(SnsPublishBatchRequestEntry entry) => CalculateEntryLength(entry);
    }

    private sealed class PartialResponseSqsClient()
        : AmazonSQSClient(
            new AnonymousAWSCredentials(),
            new AmazonSQSConfig
            {
                ServiceURL = "http://127.0.0.1:1",
                AuthenticationRegion = "eu-central-1",
            })
    {
        public int RequestEntryCount { get; private set; }

        public override Task<SendMessageBatchResponse> SendMessageBatchAsync(
            SendMessageBatchRequest request,
            CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return global::System.Threading.Tasks.Task.FromCanceled<global::Amazon.SQS.Model.SendMessageBatchResponse>(cancellationToken);

            RequestEntryCount = request.Entries.Count;
            return Task.FromResult(new SendMessageBatchResponse
            {
                HttpStatusCode = HttpStatusCode.OK,
                Successful = [new SendMessageBatchResultEntry { Id = request.Entries[0].Id }],
                Failed = [],
            });
        }
    }
}
