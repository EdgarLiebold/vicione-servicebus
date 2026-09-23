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

    [Theory]
    [InlineData("FilterPolicy", null, "{\"RoutingKey\":[\"new\"]}")]
    [InlineData("FilterPolicy", "{\"RoutingKey\":[\"old\"]}", "{\"RoutingKey\":[\"new\"]}")]
    [InlineData("FilterPolicyScope", "MessageAttributes", "MessageBody")]
    [InlineData("RawMessageDelivery", "false", "true")]
    [InlineData("RedrivePolicy", "{\"deadLetterTargetArn\":\"arn:aws:sqs:eu-central-1:123456789012:old-dlq\"}",
        "{\"deadLetterTargetArn\":\"arn:aws:sqs:eu-central-1:123456789012:new-dlq\"}")]
    [RequirementCoverage("REQ-VSB-AWS-SNS-SUBSCRIPTION", "attribute-update-failure-propagates")]
    public async Task ExistingSubscriptionAttributeUpdateFailure_IsNotReportedAsSuccessAsync(
        string attributeName, string? existingValue, string desiredValue)
    {
        const string topicArn = "arn:aws:sns:eu-central-1:123456789012:events";
        const string queueArn = "arn:aws:sqs:eu-central-1:123456789012:orders";
        const string subscriptionArn = "arn:aws:sns:eu-central-1:123456789012:events:subscription";
        var expected = new InvalidOperationException("attribute update failed");
        SetSubscriptionAttributesRequest? observedUpdate = null;

        IAmazonSimpleNotificationService sns = InterfaceProxy<IAmazonSimpleNotificationService>.Create((method, arguments) => method.Name switch
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
                Attributes = existingValue is null
                    ? new Dictionary<string, string>()
                    : new Dictionary<string, string> { [attributeName] = existingValue }
            }),
            nameof(IAmazonSimpleNotificationService.SetSubscriptionAttributesAsync) => CaptureUpdateAndFailAsync(arguments),
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
            "get_TopicSubscriptionAttributes" => new Dictionary<string, object> { [attributeName] = desiredValue },
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
        SetSubscriptionAttributesRequest update = Assert.IsType<SetSubscriptionAttributesRequest>(observedUpdate);
        Assert.Equal(subscriptionArn, update.SubscriptionArn);
        Assert.Equal(attributeName, update.AttributeName);
        Assert.Equal(desiredValue, update.AttributeValue);
        Assert.Empty(queueInfo.SubscriptionArns);

        Task<SetSubscriptionAttributesResponse> CaptureUpdateAndFailAsync(object?[]? arguments)
        {
            Assert.NotNull(arguments);
            observedUpdate = Assert.IsType<SetSubscriptionAttributesRequest>(arguments[0]);
            return Task.FromException<SetSubscriptionAttributesResponse>(expected);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SNS-SUBSCRIPTION", "attribute-read-failure-rejects-existing-subscription")]
    public async Task ExistingSubscriptionAttributeReadFailure_DoesNotAcceptStaleSubscriptionAsync()
    {
        const string topicArn = "arn:aws:sns:eu-central-1:123456789012:events";
        const string queueArn = "arn:aws:sqs:eu-central-1:123456789012:orders";
        const string subscriptionArn = "arn:aws:sns:eu-central-1:123456789012:events:subscription";
        var attributeReads = 0;

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
            nameof(IAmazonSimpleNotificationService.GetSubscriptionAttributesAsync) => ReadFailedAttributesAsync(),
            _ => throw new NotSupportedException($"Unexpected SNS call after failed attribute read: {method.Name}")
        });
        IAmazonSQS sqs = InterfaceProxy<IAmazonSQS>.Create((method, _) =>
            throw new NotSupportedException($"Unexpected SQS call after failed attribute read: {method.Name}"));
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
            "get_TopicSubscriptionAttributes" => new Dictionary<string, object> { ["FilterPolicy"] = "{\"RoutingKey\":[\"new\"]}" },
            _ => throw new NotSupportedException(method.Name)
        });
        SqsQueue queue = InterfaceProxy<SqsQueue>.Create((method, _) => method.Name switch
        {
            "get_QueueSubscriptionAttributes" => new Dictionary<string, object>(),
            _ => throw new NotSupportedException(method.Name)
        });

        AmazonSqsTransportException error = await Assert.ThrowsAsync<AmazonSqsTransportException>(
            () => context.CreateQueueSubscriptionAsync(topic, queue, CancellationToken.None));

        Assert.Contains("ServiceUnavailable", error.Message, StringComparison.Ordinal);
        Assert.Equal(1, attributeReads);
        Assert.Empty(queueInfo.SubscriptionArns);

        Task<GetSubscriptionAttributesResponse> ReadFailedAttributesAsync()
        {
            attributeReads++;
            return Task.FromResult(new GetSubscriptionAttributesResponse
            {
                HttpStatusCode = HttpStatusCode.ServiceUnavailable,
                Attributes = new Dictionary<string, string> { ["FilterPolicy"] = "{\"RoutingKey\":[\"old\"]}" }
            });
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-AWS-SNS-SUBSCRIPTION", "all-changed-attributes-precede-queue-policy")]
    public async Task ExistingSubscription_ReconcilesChangedAttributesBeforeQueuePolicyAsync(bool changeSettings)
    {
        const string topicArn = "arn:aws:sns:eu-central-1:123456789012:events";
        const string queueArn = "arn:aws:sqs:eu-central-1:123456789012:orders";
        const string queueUrl = "https://sqs.eu-central-1.amazonaws.com/123456789012/orders";
        const string subscriptionArn = "arn:aws:sns:eu-central-1:123456789012:events:subscription";
        const string existingPolicy = "{\"RoutingKey\":[\"old\"]}";
        const string desiredPolicy = "{\"RoutingKey\":[\"orders\"]}";
        const string redrivePolicy = "{\"deadLetterTargetArn\":\"arn:aws:sqs:eu-central-1:123456789012:dead-letter\"}";
        var attributeReads = 0;
        var policyUpdates = 0;
        var observedUpdates = new List<(string SubscriptionArn, string AttributeName, string AttributeValue)>();
        (string SubscriptionArn, string AttributeName, string AttributeValue)[] expectedUpdates = changeSettings
            ? [(subscriptionArn, "FilterPolicy", desiredPolicy), (subscriptionArn, "RawMessageDelivery", "true")]
            : [];

        IAmazonSimpleNotificationService sns = InterfaceProxy<IAmazonSimpleNotificationService>.Create((method, arguments) => method.Name switch
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
            nameof(IAmazonSimpleNotificationService.GetSubscriptionAttributesAsync) => ReadMatchingAttributesAsync(),
            nameof(IAmazonSimpleNotificationService.SetSubscriptionAttributesAsync) => CaptureAttributeUpdateAsync(arguments),
            _ => throw new NotSupportedException($"Unexpected SNS operation: {method.Name}")
        });
        IAmazonSQS sqs = InterfaceProxy<IAmazonSQS>.Create((method, arguments) => method.Name switch
        {
            nameof(IAmazonSQS.SetQueueAttributesAsync) => CapturePolicyUpdateAsync(arguments),
            _ => throw new NotSupportedException(method.Name)
        });
        var topicInfo = new TopicInfo("events", topicArn, sns, CancellationToken.None, true);
        var queueInfo = new QueueInfo(
            "orders", queueUrl,
            new Dictionary<string, string> { [QueueAttributeName.QueueArn] = queueArn },
            sqs, CancellationToken.None, true);
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
            "get_TopicSubscriptionAttributes" => new Dictionary<string, object>
            {
                ["FilterPolicy"] = changeSettings ? desiredPolicy : existingPolicy,
                ["FilterPolicyScope"] = "MessageBody",
                ["RawMessageDelivery"] = changeSettings ? "true" : "false",
                ["RedrivePolicy"] = redrivePolicy
            },
            _ => throw new NotSupportedException(method.Name)
        });
        SqsQueue queue = InterfaceProxy<SqsQueue>.Create((method, _) => method.Name switch
        {
            "get_QueueSubscriptionAttributes" => new Dictionary<string, object>(),
            _ => throw new NotSupportedException(method.Name)
        });

        bool changed = await context.CreateQueueSubscriptionAsync(topic, queue, CancellationToken.None);

        Assert.True(changed);
        Assert.Equal(1, attributeReads);
        Assert.Equal(1, policyUpdates);
        Assert.Equal(expectedUpdates.Length, observedUpdates.Count);
        Assert.Equal(expectedUpdates.OrderBy(update => update.AttributeName),
            observedUpdates.OrderBy(update => update.AttributeName));
        Assert.Equal(subscriptionArn, Assert.Single(queueInfo.SubscriptionArns));
        Assert.Contains(topicArn, queueInfo.Attributes[QueueAttributeName.Policy], StringComparison.Ordinal);

        Task<GetSubscriptionAttributesResponse> ReadMatchingAttributesAsync()
        {
            attributeReads++;
            return Task.FromResult(new GetSubscriptionAttributesResponse
            {
                HttpStatusCode = HttpStatusCode.OK,
                Attributes = new Dictionary<string, string>
                {
                    ["FilterPolicy"] = existingPolicy,
                    ["FilterPolicyScope"] = "MessageBody",
                    ["RawMessageDelivery"] = "false",
                    ["RedrivePolicy"] = redrivePolicy
                }
            });
        }

        Task<SetSubscriptionAttributesResponse> CaptureAttributeUpdateAsync(object?[]? arguments)
        {
            Assert.Equal(0, policyUpdates);
            Assert.NotNull(arguments);
            SetSubscriptionAttributesRequest request = Assert.IsType<SetSubscriptionAttributesRequest>(arguments[0]);
            observedUpdates.Add((request.SubscriptionArn, request.AttributeName, request.AttributeValue));
            return Task.FromResult(new SetSubscriptionAttributesResponse { HttpStatusCode = HttpStatusCode.OK });
        }

        Task<SetQueueAttributesResponse> CapturePolicyUpdateAsync(object?[]? arguments)
        {
            policyUpdates++;
            Assert.Equal(expectedUpdates.Length, observedUpdates.Count);
            Assert.NotNull(arguments);
            Assert.Equal(queueUrl, Assert.IsType<string>(arguments[0]));
            Dictionary<string, string> attributes = Assert.IsType<Dictionary<string, string>>(arguments[1]);
            Assert.Contains(topicArn, attributes[QueueAttributeName.Policy], StringComparison.Ordinal);
            return Task.FromResult(new SetQueueAttributesResponse { HttpStatusCode = HttpStatusCode.OK });
        }
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
