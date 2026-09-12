using System.Net;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using ViciOne.ServiceBus.AmazonSqs.Middleware;
using ViciOne.ServiceBus.AmazonSqs.Tests.TestDoubles;
using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;
using SqsQueue = ViciOne.ServiceBus.AmazonSqs.Topology.Queue;
using SqsTopic = ViciOne.ServiceBus.AmazonSqs.Topology.Topic;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

public sealed class AmazonSqsLifecycleBoundaryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-LIFECYCLE", "purge-is-single-flight-and-retryable")]
    public async Task PurgeOnStartup_IsSingleFlightAndRetriesAfterFailureAsync()
    {
        var filter = new PurgeOnStartupFilter("orders");
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        ClientContext context = InterfaceProxy<ClientContext>.Create((method, _) => method.Name switch
        {
            "get_CancellationToken" => CancellationToken.None,
            nameof(ClientContext.PurgeQueueAsync) => PurgeAsync(),
            _ => throw new NotSupportedException(method.Name)
        });

        async Task PurgeAsync()
        {
            Interlocked.Increment(ref calls);
            firstStarted.TrySetResult();
            await release.Task.WaitAsync(TestContext.Current.CancellationToken);
        }

        Task[] callers = Enumerable.Range(0, 16).Select(_ => filter.PurgeIfRequestedAsync(context)).ToArray();
        await firstStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, Volatile.Read(ref calls));
        release.TrySetResult();
        await Task.WhenAll(callers);
        await filter.PurgeIfRequestedAsync(context);
        Assert.Equal(1, Volatile.Read(ref calls));

        var retryFilter = new PurgeOnStartupFilter("orders");
        var expected = new InvalidOperationException("transient purge failure");
        var attempts = 0;
        ClientContext retryContext = InterfaceProxy<ClientContext>.Create((method, _) => method.Name switch
        {
            "get_CancellationToken" => CancellationToken.None,
            nameof(ClientContext.PurgeQueueAsync) => Interlocked.Increment(ref attempts) == 1
                ? Task.FromException(expected)
                : Task.CompletedTask,
            _ => throw new NotSupportedException(method.Name)
        });

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() => retryFilter.PurgeIfRequestedAsync(retryContext));
        Assert.Same(expected, actual);
        await retryFilter.PurgeIfRequestedAsync(retryContext);
        Assert.Equal(2, Volatile.Read(ref attempts));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-LIFECYCLE", "topology-entities-declared-exactly-once")]
    public async Task TopologyDeclaration_CreatesEachEntityExactlyOnceAsync()
    {
        var topicCalls = 0;
        var queueCalls = 0;
        var topicInfo = new TopicInfo("events", "arn:aws:sns:eu-central-1:123456789012:events", null!, CancellationToken.None, false);
        var queueInfo = new QueueInfo(
            "orders",
            "https://sqs.eu-central-1.amazonaws.com/123456789012/orders",
            new Dictionary<string, string> { [QueueAttributeName.QueueArn] = "arn:aws:sqs:eu-central-1:123456789012:orders" },
            null!,
            CancellationToken.None,
            false);
        ClientContext context = InterfaceProxy<ClientContext>.Create((method, _) => method.Name switch
        {
            nameof(ClientContext.CreateTopicAsync) => ReturnTopicAsync(),
            nameof(ClientContext.CreateQueueAsync) => ReturnQueueAsync(),
            _ => throw new NotSupportedException(method.Name)
        });

        Task<TopicInfo> ReturnTopicAsync()
        {
            Interlocked.Increment(ref topicCalls);
            return Task.FromResult(topicInfo);
        }

        Task<QueueInfo> ReturnQueueAsync()
        {
            Interlocked.Increment(ref queueCalls);
            return Task.FromResult(queueInfo);
        }

        TopicInfo declaredTopic = await ConfigureAmazonSqsTopologyFilter<object>.DeclareAsync(context, (SqsTopic)null!, CancellationToken.None);
        QueueInfo declaredQueue = await ConfigureAmazonSqsTopologyFilter<object>.DeclareAsync(context, (SqsQueue)null!, CancellationToken.None);

        Assert.Same(topicInfo, declaredTopic);
        Assert.Same(queueInfo, declaredQueue);
        Assert.Equal(1, Volatile.Read(ref topicCalls));
        Assert.Equal(1, Volatile.Read(ref queueCalls));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SNS-SUBSCRIPTION", "attribute-update-failure-propagates")]
    public async Task ExistingSubscriptionAttributeUpdateFailure_IsNotReportedAsSuccessAsync()
    {
        const string topicArn = "arn:aws:sns:eu-central-1:123456789012:events";
        const string queueArn = "arn:aws:sqs:eu-central-1:123456789012:orders";
        const string subscriptionArn = "arn:aws:sns:eu-central-1:123456789012:events:subscription";
        var expected = new InvalidOperationException("attribute update failed");

        IAmazonSimpleNotificationService sns = InterfaceProxy<IAmazonSimpleNotificationService>.Create((method, _) => method.Name switch
        {
            nameof(IAmazonSimpleNotificationService.SubscribeAsync) => Task.FromException<SubscribeResponse>(
                new InvalidParameterException("subscription already exists")),
            nameof(IAmazonSimpleNotificationService.ListSubscriptionsByTopicAsync) => Task.FromResult(new ListSubscriptionsByTopicResponse
            {
                HttpStatusCode = HttpStatusCode.OK,
                Subscriptions =
                [
                    new Subscription
                    {
                        TopicArn = topicArn,
                        Endpoint = queueArn,
                        Protocol = "sqs",
                        SubscriptionArn = subscriptionArn
                    }
                ]
            }),
            nameof(IAmazonSimpleNotificationService.GetSubscriptionAttributesAsync) => Task.FromResult(new GetSubscriptionAttributesResponse
            {
                HttpStatusCode = HttpStatusCode.OK,
                Attributes = new Dictionary<string, string> { ["RawMessageDelivery"] = "false" }
            }),
            nameof(IAmazonSimpleNotificationService.SetSubscriptionAttributesAsync) => Task.FromException<SetSubscriptionAttributesResponse>(expected),
            _ => throw new NotSupportedException(method.Name)
        });
        IAmazonSQS sqs = InterfaceProxy<IAmazonSQS>.Create((method, _) => throw new NotSupportedException(method.Name));
        var topicInfo = new TopicInfo("events", topicArn, sns, CancellationToken.None, true);
        var queueInfo = new QueueInfo(
            "orders",
            "https://sqs.eu-central-1.amazonaws.com/123456789012/orders",
            new Dictionary<string, string> { [QueueAttributeName.QueueArn] = queueArn },
            sqs,
            CancellationToken.None,
            true);
        ConnectionContext connection = InterfaceProxy<ConnectionContext>.Create((method, _) => method.Name switch
        {
            "get_CancellationToken" => CancellationToken.None,
            nameof(ConnectionContext.GetTopicAsync) => Task.FromResult(topicInfo),
            nameof(ConnectionContext.GetQueueAsync) => Task.FromResult(queueInfo),
            _ => throw new NotSupportedException(method.Name)
        });
        var context = new AmazonSqsClientContext(connection, sqs, sns, CancellationToken.None);
        SqsTopic topic = InterfaceProxy<SqsTopic>.Create((method, _) => method.Name switch
        {
            "get_TopicSubscriptionAttributes" => new Dictionary<string, object> { ["RawMessageDelivery"] = "true" },
            _ => throw new NotSupportedException(method.Name)
        });
        SqsQueue queue = InterfaceProxy<SqsQueue>.Create((method, _) => method.Name switch
        {
            "get_QueueSubscriptionAttributes" => new Dictionary<string, object>(),
            _ => throw new NotSupportedException(method.Name)
        });

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.CreateQueueSubscriptionAsync(topic, queue, CancellationToken.None));

        Assert.Same(expected, actual);
        Assert.Empty(queueInfo.SubscriptionArns);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SNS-SUBSCRIPTION", "outbound-raw-delivery-name-is-canonical")]
    public async Task QueueSubscription_CanonicalizesTheOutboundRawMessageDeliveryNameAsync()
    {
        const string topicArn = "arn:aws:sns:eu-central-1:123456789012:events";
        const string queueArn = "arn:aws:sqs:eu-central-1:123456789012:orders";
        var expected = new InvalidOperationException("stop after request capture");
        SubscribeRequest? observedRequest = null;

        IAmazonSimpleNotificationService sns = InterfaceProxy<IAmazonSimpleNotificationService>.Create((method, arguments) => method.Name switch
        {
            nameof(IAmazonSimpleNotificationService.SubscribeAsync) => CaptureAndStopAsync(arguments),
            _ => throw new NotSupportedException(method.Name)
        });
        IAmazonSQS sqs = InterfaceProxy<IAmazonSQS>.Create((method, _) => throw new NotSupportedException(method.Name));
        var topicInfo = new TopicInfo("events", topicArn, sns, CancellationToken.None, true);
        var queueInfo = new QueueInfo(
            "orders",
            "https://sqs.eu-central-1.amazonaws.com/123456789012/orders",
            new Dictionary<string, string> { [QueueAttributeName.QueueArn] = queueArn },
            sqs,
            CancellationToken.None,
            true);
        ConnectionContext connection = InterfaceProxy<ConnectionContext>.Create((method, _) => method.Name switch
        {
            "get_CancellationToken" => CancellationToken.None,
            nameof(ConnectionContext.GetTopicAsync) => Task.FromResult(topicInfo),
            nameof(ConnectionContext.GetQueueAsync) => Task.FromResult(queueInfo),
            _ => throw new NotSupportedException(method.Name)
        });
        var context = new AmazonSqsClientContext(connection, sqs, sns, CancellationToken.None);
        SqsTopic topic = InterfaceProxy<SqsTopic>.Create((method, _) => method.Name switch
        {
            "get_TopicSubscriptionAttributes" => new Dictionary<string, object> { ["rawmessagedelivery"] = "false" },
            _ => throw new NotSupportedException(method.Name)
        });
        SqsQueue queue = InterfaceProxy<SqsQueue>.Create((method, _) => method.Name switch
        {
            "get_QueueSubscriptionAttributes" => new Dictionary<string, object>(),
            _ => throw new NotSupportedException(method.Name)
        });

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.CreateQueueSubscriptionAsync(topic, queue, CancellationToken.None));

        Assert.Same(expected, actual);
        SubscribeRequest request = Assert.IsType<SubscribeRequest>(observedRequest);
        KeyValuePair<string, string> attribute = Assert.Single(request.Attributes);
        Assert.Equal("RawMessageDelivery", attribute.Key);
        Assert.Equal("false", attribute.Value);

        Task<SubscribeResponse> CaptureAndStopAsync(object?[]? arguments)
        {
            Assert.NotNull(arguments);
            Assert.Equal(2, arguments.Length);
            observedRequest = Assert.IsType<SubscribeRequest>(arguments[0]);
            return Task.FromException<SubscribeResponse>(expected);
        }
    }
}
