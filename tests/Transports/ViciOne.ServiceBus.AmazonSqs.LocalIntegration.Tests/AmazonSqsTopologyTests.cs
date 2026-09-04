using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using ViciOne.ServiceBus.AmazonSqs.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.LocalIntegration.Tests;

public sealed class AmazonSqsTopologyTests
{
    private const int SubscriptionCount = 23;

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0182", "one-queue-twenty-three-distinct-topic-subscriptions")]
    public async Task OneQueue_CarriesMultipleDistinctTopicSubscriptionsAsync()
    {
        await using AmazonSqsLocalStack fixture = AmazonSqsLocalStack.Create("subscriptions");
        using AmazonSQSClient sqs = fixture.CreateSqsClient();
        using AmazonSimpleNotificationServiceClient sns = fixture.CreateSnsClient();
        string queueName = fixture.Name("input");
        Guid[] expected = Enumerable.Range(0, SubscriptionCount).Select(_ => Guid.NewGuid()).ToArray();
        TaskCompletionSource<Guid>[] received = Enumerable.Range(0, SubscriptionCount)
            .Select(_ => new TaskCompletionSource<Guid>(TaskCreationOptions.RunContinuationsAsynchronously))
            .ToArray();
        object[] messages =
        [
            new TopologyMessage00(expected[0]), new TopologyMessage01(expected[1]), new TopologyMessage02(expected[2]),
            new TopologyMessage03(expected[3]), new TopologyMessage04(expected[4]), new TopologyMessage05(expected[5]),
            new TopologyMessage06(expected[6]), new TopologyMessage07(expected[7]), new TopologyMessage08(expected[8]),
            new TopologyMessage09(expected[9]), new TopologyMessage10(expected[10]), new TopologyMessage11(expected[11]),
            new TopologyMessage12(expected[12]), new TopologyMessage13(expected[13]), new TopologyMessage14(expected[14]),
            new TopologyMessage15(expected[15]), new TopologyMessage16(expected[16]), new TopologyMessage17(expected[17]),
            new TopologyMessage18(expected[18]), new TopologyMessage19(expected[19]), new TopologyMessage20(expected[20]),
            new TopologyMessage21(expected[21]), new TopologyMessage22(expected[22]),
        ];
        IBusControl bus = Bus.Factory.CreateUsingAmazonSqs(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.Durable = false;
                endpoint.AutoDelete = true;
                endpoint.Handler<TopologyMessage00>(context => CompleteAsync(context.Message, received));
                endpoint.Handler<TopologyMessage01>(context => CompleteAsync(context.Message, received));
                endpoint.Handler<TopologyMessage02>(context => CompleteAsync(context.Message, received));
                endpoint.Handler<TopologyMessage03>(context => CompleteAsync(context.Message, received));
                endpoint.Handler<TopologyMessage04>(context => CompleteAsync(context.Message, received));
                endpoint.Handler<TopologyMessage05>(context => CompleteAsync(context.Message, received));
                endpoint.Handler<TopologyMessage06>(context => CompleteAsync(context.Message, received));
                endpoint.Handler<TopologyMessage07>(context => CompleteAsync(context.Message, received));
                endpoint.Handler<TopologyMessage08>(context => CompleteAsync(context.Message, received));
                endpoint.Handler<TopologyMessage09>(context => CompleteAsync(context.Message, received));
                endpoint.Handler<TopologyMessage10>(context => CompleteAsync(context.Message, received));
                endpoint.Handler<TopologyMessage11>(context => CompleteAsync(context.Message, received));
                endpoint.Handler<TopologyMessage12>(context => CompleteAsync(context.Message, received));
                endpoint.Handler<TopologyMessage13>(context => CompleteAsync(context.Message, received));
                endpoint.Handler<TopologyMessage14>(context => CompleteAsync(context.Message, received));
                endpoint.Handler<TopologyMessage15>(context => CompleteAsync(context.Message, received));
                endpoint.Handler<TopologyMessage16>(context => CompleteAsync(context.Message, received));
                endpoint.Handler<TopologyMessage17>(context => CompleteAsync(context.Message, received));
                endpoint.Handler<TopologyMessage18>(context => CompleteAsync(context.Message, received));
                endpoint.Handler<TopologyMessage19>(context => CompleteAsync(context.Message, received));
                endpoint.Handler<TopologyMessage20>(context => CompleteAsync(context.Message, received));
                endpoint.Handler<TopologyMessage21>(context => CompleteAsync(context.Message, received));
                endpoint.Handler<TopologyMessage22>(context => CompleteAsync(context.Message, received));
            });
        });
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            await Task.WhenAll(messages.Select(message => bus.PublishAsync(message, message.GetType(), cancellationToken)))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            Guid[] actual = await Task.WhenAll(received.Select(completion => completion.Task))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.Equal(expected, actual);

            string[] queueNames = await ListOwnedQueueNamesAsync(sqs, fixture.Prefix, fixture.OperationTimeout, cancellationToken);
            Topic[] topics = await ListOwnedTopicsAsync(sns, fixture.Prefix, fixture.OperationTimeout, cancellationToken);
            Assert.Equal([queueName], queueNames);
            Assert.Equal(SubscriptionCount, topics.Length);

            foreach (Topic topic in topics)
            {
                ListSubscriptionsByTopicResponse response = await sns.ListSubscriptionsByTopicAsync(
                        new ListSubscriptionsByTopicRequest { TopicArn = topic.TopicArn }, cancellationToken)
                    .WaitAsync(fixture.OperationTimeout, cancellationToken);
                Subscription subscription = Assert.Single(response.Subscriptions ?? []);
                Assert.Equal("sqs", subscription.Protocol);
                Assert.EndsWith(':' + queueName, subscription.Endpoint, StringComparison.Ordinal);
            }
        }
        finally
        {
            if (started)
                await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0273", "queue-and-topic-tags-persist-exactly")]
    public async Task QueueAndTopicTags_ArePersistedExactlyAsync()
    {
        await using AmazonSqsLocalStack fixture = AmazonSqsLocalStack.Create("topologytags");
        using AmazonSQSClient sqs = fixture.CreateSqsClient();
        using AmazonSimpleNotificationServiceClient sns = fixture.CreateSnsClient();
        string queueName = fixture.Name("input");
        string topicName = new AmazonSqsMessageNameFormatter().GetMessageName(typeof(TaggedMessage));
        var delivered = new TaskCompletionSource<Guid>(TaskCreationOptions.RunContinuationsAsynchronously);
        Guid correlationId = Guid.NewGuid();
        Dictionary<string, string> expectedQueueTags = new(StringComparer.Ordinal)
        {
            ["environment"] = "local-integration",
            ["owner"] = "vicione-servicebus",
        };
        Dictionary<string, string> expectedTopicTags = new(StringComparer.Ordinal)
        {
            ["contract"] = "tagged-message",
            ["owner"] = "vicione-servicebus",
        };
        IBusControl bus = Bus.Factory.CreateUsingAmazonSqs(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.Publish<TaggedMessage>(publish =>
            {
                foreach ((string key, string value) in expectedTopicTags)
                    publish.TopicTags[key] = value;
            });
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                foreach ((string key, string value) in expectedQueueTags)
                    endpoint.QueueTags[key] = value;
                endpoint.Handler<TaggedMessage>(context =>
                {
                    delivered.TrySetResult(context.Message.CorrelationId);
                    return Task.CompletedTask;
                });
            });
        });
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            await bus.PublishAsync(new TaggedMessage(correlationId), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.Equal(correlationId, await delivered.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));

            string queueUrl = (await sqs.GetQueueUrlAsync(queueName, cancellationToken)
                    .WaitAsync(fixture.OperationTimeout, cancellationToken))
                .QueueUrl;
            ListQueueTagsResponse queueTags = await sqs.ListQueueTagsAsync(
                    new ListQueueTagsRequest { QueueUrl = queueUrl },
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.Equal(expectedQueueTags, queueTags.Tags);

            Topic[] topics = await ListOwnedTopicsAsync(sns, fixture.Prefix, fixture.OperationTimeout, cancellationToken);
            Topic topic = Assert.Single(topics, candidate =>
                candidate.TopicArn.EndsWith(':' + fixture.Prefix + '_' + topicName, StringComparison.Ordinal));
            ListTagsForResourceResponse topicTags = await sns.ListTagsForResourceAsync(
                    new ListTagsForResourceRequest { ResourceArn = topic.TopicArn },
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.Equal(
                expectedTopicTags,
                topicTags.Tags.ToDictionary(tag => tag.Key, tag => tag.Value, StringComparer.Ordinal));
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    private static Task CompleteAsync(IIndexedTopologyMessage message, TaskCompletionSource<Guid>[] received)
    {
        if (message.Index is < 0 or >= SubscriptionCount)
            throw new InvalidDataException($"The topology message index {message.Index} is outside the test-owned subscription set.");
        received[message.Index].TrySetResult(message.CorrelationId);
        return Task.CompletedTask;
    }

    private static async Task<string[]> ListOwnedQueueNamesAsync(
        IAmazonSQS sqs,
        string prefix,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        ListQueuesResponse response = await sqs.ListQueuesAsync(
                new ListQueuesRequest { QueueNamePrefix = prefix }, cancellationToken)
            .WaitAsync(timeout, cancellationToken);
        return (response.QueueUrls ?? [])
            .Select(url => url[(url.LastIndexOf('/') + 1)..])
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private static async Task<Topic[]> ListOwnedTopicsAsync(
        IAmazonSimpleNotificationService sns,
        string prefix,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var topics = new List<Topic>();
        string? token = null;
        do
        {
            ListTopicsResponse response = await sns.ListTopicsAsync(new ListTopicsRequest { NextToken = token }, cancellationToken)
                .WaitAsync(timeout, cancellationToken);
            topics.AddRange((response.Topics ?? []).Where(topic =>
                topic.TopicArn[(topic.TopicArn.LastIndexOf(':') + 1)..].StartsWith(prefix, StringComparison.Ordinal)));
            token = response.NextToken;
        }
        while (!string.IsNullOrEmpty(token));

        return topics.OrderBy(topic => topic.TopicArn, StringComparer.Ordinal).ToArray();
    }

    private interface IIndexedTopologyMessage
    {
        int Index { get; }
        Guid CorrelationId { get; }
    }

    private sealed record TopologyMessage00(Guid CorrelationId) : IIndexedTopologyMessage { public int Index => 0; }
    private sealed record TopologyMessage01(Guid CorrelationId) : IIndexedTopologyMessage { public int Index => 1; }
    private sealed record TopologyMessage02(Guid CorrelationId) : IIndexedTopologyMessage { public int Index => 2; }
    private sealed record TopologyMessage03(Guid CorrelationId) : IIndexedTopologyMessage { public int Index => 3; }
    private sealed record TopologyMessage04(Guid CorrelationId) : IIndexedTopologyMessage { public int Index => 4; }
    private sealed record TopologyMessage05(Guid CorrelationId) : IIndexedTopologyMessage { public int Index => 5; }
    private sealed record TopologyMessage06(Guid CorrelationId) : IIndexedTopologyMessage { public int Index => 6; }
    private sealed record TopologyMessage07(Guid CorrelationId) : IIndexedTopologyMessage { public int Index => 7; }
    private sealed record TopologyMessage08(Guid CorrelationId) : IIndexedTopologyMessage { public int Index => 8; }
    private sealed record TopologyMessage09(Guid CorrelationId) : IIndexedTopologyMessage { public int Index => 9; }
    private sealed record TopologyMessage10(Guid CorrelationId) : IIndexedTopologyMessage { public int Index => 10; }
    private sealed record TopologyMessage11(Guid CorrelationId) : IIndexedTopologyMessage { public int Index => 11; }
    private sealed record TopologyMessage12(Guid CorrelationId) : IIndexedTopologyMessage { public int Index => 12; }
    private sealed record TopologyMessage13(Guid CorrelationId) : IIndexedTopologyMessage { public int Index => 13; }
    private sealed record TopologyMessage14(Guid CorrelationId) : IIndexedTopologyMessage { public int Index => 14; }
    private sealed record TopologyMessage15(Guid CorrelationId) : IIndexedTopologyMessage { public int Index => 15; }
    private sealed record TopologyMessage16(Guid CorrelationId) : IIndexedTopologyMessage { public int Index => 16; }
    private sealed record TopologyMessage17(Guid CorrelationId) : IIndexedTopologyMessage { public int Index => 17; }
    private sealed record TopologyMessage18(Guid CorrelationId) : IIndexedTopologyMessage { public int Index => 18; }
    private sealed record TopologyMessage19(Guid CorrelationId) : IIndexedTopologyMessage { public int Index => 19; }
    private sealed record TopologyMessage20(Guid CorrelationId) : IIndexedTopologyMessage { public int Index => 20; }
    private sealed record TopologyMessage21(Guid CorrelationId) : IIndexedTopologyMessage { public int Index => 21; }
    private sealed record TopologyMessage22(Guid CorrelationId) : IIndexedTopologyMessage { public int Index => 22; }
    private sealed record TaggedMessage(Guid CorrelationId);
}
