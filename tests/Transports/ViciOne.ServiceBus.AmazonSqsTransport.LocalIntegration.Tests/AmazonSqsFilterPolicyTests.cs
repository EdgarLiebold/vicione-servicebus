using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using ViciOne.ServiceBus.AmazonSqsTransport.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqsTransport.LocalIntegration.Tests;

public sealed class AmazonSqsFilterPolicyTests
{
    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0212", "mutually-exclusive-policies-route-to-only-their-own-queue")]
    public async Task MutuallyExclusivePolicies_DeliverOnlyToTheirMatchingQueueAsync()
    {
        await using AmazonSqsLocalStack fixture = AmazonSqsLocalStack.Create("filterpolicy");
        using AmazonSimpleNotificationServiceClient sns = fixture.CreateSnsClient();
        string fooQueue = fixture.Name("foo");
        string barQueue = fixture.Name("bar");
        var wrongDelivery = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var foo = new QueueRecorder("foo", "Hello", wrongDelivery);
        var bar = new QueueRecorder("bar", "World", wrongDelivery);
        IBusControl bus = Bus.Factory.CreateUsingAmazonSqs(configurator =>
        {
            fixture.ConfigureHost(configurator);
            ConfigureFilteredEndpoint(configurator, fooQueue, foo);
            ConfigureFilteredEndpoint(configurator, barQueue, bar);
        });
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            await bus.PublishAsync(
                    new FilteredMessage("foo", "Hello"),
                    context => context.Headers.Set("RoutingKey", "foo"),
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await bus.PublishAsync(
                    new FilteredMessage("bar", "World"),
                    context => context.Headers.Set("RoutingKey", "bar"),
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(new FilteredMessage("foo", "Hello"),
                await foo.Received.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));
            Assert.Equal(new FilteredMessage("bar", "World"),
                await bar.Received.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));

            Topic topic = Assert.Single(await ListOwnedTopicsAsync(sns, fixture.Prefix, fixture.OperationTimeout, cancellationToken));
            ListSubscriptionsByTopicResponse subscriptions = await sns.ListSubscriptionsByTopicAsync(
                    new ListSubscriptionsByTopicRequest { TopicArn = topic.TopicArn }, cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            string[] policies = await Task.WhenAll((subscriptions.Subscriptions ?? []).Select(async subscription =>
            {
                GetSubscriptionAttributesResponse attributes = await sns.GetSubscriptionAttributesAsync(
                        new GetSubscriptionAttributesRequest { SubscriptionArn = subscription.SubscriptionArn }, cancellationToken)
                    .WaitAsync(fixture.OperationTimeout, cancellationToken);
                return attributes.Attributes["FilterPolicy"];
            }));
            Assert.Equal(
                [Policy("bar"), Policy("foo")],
                policies.Order(StringComparer.Ordinal));

            await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = false;

            Assert.False(wrongDelivery.Task.IsCompleted,
                "At least one filtered queue received a message that did not match its own policy.");
            Assert.Equal(1, foo.DeliveryCount);
            Assert.Equal(1, bar.DeliveryCount);
        }
        finally
        {
            if (started)
                await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
    }

    private static void ConfigureFilteredEndpoint(
        IAmazonSqsBusFactoryConfigurator configurator,
        string queueName,
        QueueRecorder recorder)
    {
        configurator.ReceiveEndpoint(queueName, endpoint =>
        {
            endpoint.ConfigureConsumeTopology = false;
            endpoint.Durable = false;
            endpoint.AutoDelete = true;
            endpoint.QueueSubscriptionAttributes["FilterPolicy"] = Policy(recorder.ExpectedKey);
            endpoint.Subscribe<FilteredMessage>();
            endpoint.Handler<FilteredMessage>(recorder.ObserveAsync);
        });
    }

    private static string Policy(string key) => $"{{\"RoutingKey\":[\"{key}\"]}}";

    private static async Task<Topic[]> ListOwnedTopicsAsync(
        IAmazonSimpleNotificationService sns,
        string prefix,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        ListTopicsResponse response = await sns.ListTopicsAsync(new ListTopicsRequest(), cancellationToken)
            .WaitAsync(timeout, cancellationToken);
        return (response.Topics ?? [])
            .Where(topic => topic.TopicArn[(topic.TopicArn.LastIndexOf(':') + 1)..].StartsWith(prefix, StringComparison.Ordinal))
            .OrderBy(topic => topic.TopicArn, StringComparer.Ordinal)
            .ToArray();
    }

    private sealed record FilteredMessage(string Key, string Value);

    private sealed class QueueRecorder(
        string expectedKey,
        string expectedValue,
        TaskCompletionSource<string> wrongDelivery)
    {
        private int _deliveryCount;

        public string ExpectedKey { get; } = expectedKey;
        public int DeliveryCount => Volatile.Read(ref _deliveryCount);
        public TaskCompletionSource<FilteredMessage> Received { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task ObserveAsync(ConsumeContext<FilteredMessage> context)
        {
            Interlocked.Increment(ref _deliveryCount);
            if (context.Message is { Key: var key, Value: var value }
                && key == ExpectedKey
                && value == expectedValue)
                Received.TrySetResult(context.Message);
            else
                wrongDelivery.TrySetResult(
                    $"Queue '{ExpectedKey}' received the non-matching message '{context.Message.Key}:{context.Message.Value}'.");

            return Task.CompletedTask;
        }
    }
}
