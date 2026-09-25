using Amazon.SQS;
using Amazon.SQS.Model;
using ViciOne.ServiceBus.AmazonSqs.Middleware;
using ViciOne.ServiceBus.AmazonSqs.Tests.TestDoubles;
using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;
using PublishBatchRequestEntry = Amazon.SimpleNotificationService.Model.PublishBatchRequestEntry;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

public sealed class SqsMoveTransportTests
{
    [Theory]
    [InlineData(9, true)]
    [InlineData(10, false)]
    [RequirementCoverage("REQ-VSB-AWS-SQS-MOVE", "dead-letter-reason-respects-ten-attribute-provider-limit")]
    public async Task DeadLetterMove_HandlesTheSqsAttributeLimitBeforeProviderSubmissionAsync(int customCount, bool shouldSend)
    {
        var message = new Message
        {
            Body = "wire-body",
            MessageAttributes = Enumerable.Range(0, customCount).ToDictionary(
                index => $"Custom{index:D2}",
                index => new MessageAttributeValue { DataType = "String", StringValue = $"value-{index}" })
        };
        AmazonSqsMessageContext messageContext = CreateMessageContext(message);
        int sendCount = 0;
        Dictionary<string, string>? sentAttributes = null;
        var clientContext = new TestClientContext(CancellationToken.None, (_, entry, _) =>
        {
            sendCount++;
            sentAttributes = entry.MessageAttributes.ToDictionary(pair => pair.Key, pair => pair.Value.StringValue);
            return Task.CompletedTask;
        });
        ReceiveContext receiveContext = CreateReceiveContext(clientContext, CancellationToken.None, messageContext);
        var adapter = new TransportSetHeaderAdapter<MessageAttributeValue>(new SqsHeaderValueConverter());
        DeadLetterSettings settings = InterfaceProxy<DeadLetterSettings>.Create((method, _) => throw new NotSupportedException(method.Name));
        var topology = new AmazonSqsBrokerTopology([], [], []);
        var transport = new SqsDeadLetterTransport("skipped", adapter,
            new ConfigureAmazonSqsTopologyFilter<DeadLetterSettings>(settings, topology));

        if (shouldSend)
        {
            await transport.SendAsync(receiveContext, "unhandled", TestContext.Current.CancellationToken);

            Assert.Equal(1, sendCount);
            Dictionary<string, string> attributes = Assert.IsType<Dictionary<string, string>>(sentAttributes);
            Assert.Equal(10, attributes.Count);
            Assert.Equal("unhandled", attributes[MessageHeaders.Reason]);
            for (int index = 0; index < customCount; index++)
                Assert.Equal($"value-{index}", attributes[$"Custom{index:D2}"]);
        }
        else
        {
            InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
                () => transport.SendAsync(receiveContext, "unhandled", TestContext.Current.CancellationToken));

            Assert.Contains("10 message attributes", error.Message, StringComparison.Ordinal);
            Assert.Equal(0, sendCount);
            Assert.Equal(customCount, message.MessageAttributes.Count);
            Assert.DoesNotContain(MessageHeaders.Reason, message.MessageAttributes.Keys);
            for (int index = 0; index < customCount; index++)
                Assert.Equal($"value-{index}", message.MessageAttributes[$"Custom{index:D2}"].StringValue);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-MOVE", "failed-provider-send-redeclares-destination-before-retry")]
    public async Task Move_ProviderFailureEvictsTopologySoRetryRedeclaresDestinationAsync()
    {
        const string destination = "errors";
        var providerFailure = new InvalidOperationException("provider rejected the move");
        int declarations = 0;
        int sends = 0;
        IAmazonSQS sqs = InterfaceProxy<IAmazonSQS>.Create((method, _) => throw new NotSupportedException(method.Name));
        await using var queueInfo = new QueueInfo(destination,
            "https://sqs.eu-central-1.amazonaws.com/123456789012/errors",
            new Dictionary<string, string> { [QueueAttributeName.QueueArn] = "arn:aws:sqs:eu-central-1:123456789012:errors" },
            sqs, CancellationToken.None, true);
        var clientContext = new TestClientContext(CancellationToken.None, (queue, entry, _) =>
        {
            Assert.Equal(destination, queue);
            Assert.Equal("move-body", entry.MessageBody);
            if (Interlocked.Increment(ref sends) == 1)
                throw providerFailure;
            return Task.CompletedTask;
        }, (queue, _) =>
        {
            Assert.Equal(destination, queue.EntityName);
            Interlocked.Increment(ref declarations);
            return Task.FromResult(queueInfo);
        });
        ReceiveContext receiveContext = CreateReceiveContext(clientContext, CancellationToken.None, body: "move-body");
        var topology = new AmazonSqsBrokerTopology([], [new QueueEntity(1, destination, true, false)], []);
        var transport = new TestMoveTransport(new ConfigureAmazonSqsTopologyFilter<object>(new object(), topology));

        Exception firstFailure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => transport.MoveAsync(receiveContext, TestContext.Current.CancellationToken));
        Assert.Same(providerFailure, firstFailure);
        Assert.Equal(1, declarations);
        Assert.Equal(1, sends);

        await transport.MoveAsync(receiveContext, TestContext.Current.CancellationToken);

        Assert.Equal(2, declarations);
        Assert.Equal(2, sends);

        await transport.MoveAsync(receiveContext, TestContext.Current.CancellationToken);

        Assert.Equal(2, declarations);
        Assert.Equal(3, sends);
    }

    [Theory]
    [InlineData("errors.fifo", true)]
    [InlineData("errors", false)]
    [RequirementCoverage("REQ-VSB-AWS-SQS-MOVE", "provider-request-uses-context-body-and-fifo-only-for-fifo-destinations")]
    public async Task Move_SendsContextBodyAndCustomAttributesWithoutStaleTransportHeadersAsync(string destination, bool fifo)
    {
        var originalCustom = new MessageAttributeValue { DataType = "String", StringValue = "customer-42" };
        byte[] originalBinary = [0x00, 0x80, 0xff];
        using var binaryValue = new MemoryStream(originalBinary);
        var binaryCustom = new MessageAttributeValue { DataType = "Binary", BinaryValue = binaryValue };
        var staleTransportHeader = new MessageAttributeValue { DataType = "String", StringValue = "old" };
        var newTransportHeader = new MessageAttributeValue { DataType = "String", StringValue = "moved" };
        string staleHeaderName = MessageHeaders.Prefix + "Old";
        string newHeaderName = MessageHeaders.Prefix + "Move";
        var message = new Message
        {
            Body = "wire-body",
            Attributes = new Dictionary<string, string>
            {
                [Amazon.SQS.MessageSystemAttributeName.MessageGroupId] = "tenant-42",
                [Amazon.SQS.MessageSystemAttributeName.MessageDeduplicationId] = "order-99"
            },
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                ["Customer"] = originalCustom,
                ["Payload"] = binaryCustom,
                [staleHeaderName] = staleTransportHeader
            }
        };
        string? sentBody = null;
        string? sentGroup = null;
        string? sentDeduplication = null;
        Dictionary<string, (string Type, string Value)>? sentAttributes = null;
        byte[]? sentBinary = null;
        string? sentQueue = null;
        var clientContext = new TestClientContext(CancellationToken.None, (queue, entry, _) =>
        {
            sentQueue = queue;
            sentBody = entry.MessageBody;
            sentGroup = entry.MessageGroupId;
            sentDeduplication = entry.MessageDeduplicationId;
            sentAttributes = entry.MessageAttributes.ToDictionary(
                pair => pair.Key, pair => (pair.Value.DataType, pair.Value.StringValue));
            sentBinary = entry.MessageAttributes["Payload"].BinaryValue.ToArray();
            return Task.CompletedTask;
        });
        AmazonSqsMessageContext messageContext = CreateMessageContext(message);
        ReceiveContext receiveContext = CreateReceiveContext(clientContext, CancellationToken.None, messageContext, "admitted-body");
        var topology = new AmazonSqsBrokerTopology([], [], []);
        var transport = new TestMoveTransport(destination, new ConfigureAmazonSqsTopologyFilter<object>(new object(), topology));

        await transport.MoveWithPreSendAsync(receiveContext, (_, attributes) => attributes[newHeaderName] = newTransportHeader,
            TestContext.Current.CancellationToken);

        Assert.Equal(destination, sentQueue);
        Assert.Equal("admitted-body", sentBody);
        Assert.Equal(fifo ? "tenant-42" : null, sentGroup);
        Assert.Equal(fifo ? "order-99" : null, sentDeduplication);
        Dictionary<string, (string Type, string Value)> attributesAtSend =
            Assert.IsType<Dictionary<string, (string Type, string Value)>>(sentAttributes);
        Assert.Equal(3, attributesAtSend.Count);
        Assert.Equal(("String", "customer-42"), attributesAtSend["Customer"]);
        Assert.Equal("Binary", attributesAtSend["Payload"].Type);
        Assert.Equal(originalBinary, sentBinary);
        Assert.Equal(("String", "moved"), attributesAtSend[newHeaderName]);
        Assert.DoesNotContain(staleHeaderName, attributesAtSend.Keys);
        Assert.Same(staleTransportHeader, message.MessageAttributes[staleHeaderName]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-CANCELLATION", "move-links-caller-lifetime")]
    public async Task Move_LinksCallerCancellationToTopologyAndProviderOperationsAsync()
    {
        using var receiveLifetime = new CancellationTokenSource();
        using var callerLifetime = new CancellationTokenSource();
        var providerStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken observedToken = default;
        var clientContext = new TestClientContext(receiveLifetime.Token, async (_, _, token) =>
        {
            observedToken = token;
            providerStarted.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
        });
        ReceiveContext receiveContext = CreateReceiveContext(clientContext, receiveLifetime.Token);
        var topology = new AmazonSqsBrokerTopology([], [], []);
        var transport = new TestMoveTransport(new ConfigureAmazonSqsTopologyFilter<object>(new object(), topology));

        Task move = transport.MoveAsync(receiveContext, callerLifetime.Token);
        await providerStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

        try
        {
            callerLifetime.Cancel();
            Task completed = await Task.WhenAny(move, Task.Delay(TimeSpan.FromMilliseconds(500), TestContext.Current.CancellationToken));

            Assert.Same(move, completed);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => move);
            Assert.True(observedToken.IsCancellationRequested);
        }
        finally
        {
            receiveLifetime.Cancel();
            await IgnoreCancellationAsync(move);
        }
    }

    private static ReceiveContext CreateReceiveContext(ClientContext clientContext, CancellationToken cancellationToken,
        AmazonSqsMessageContext? messageContext = null, string body = "payload")
    {
        return InterfaceProxy<ReceiveContext>.Create((method, args) => method.Name switch
        {
            "get_Body" => new StringMessageBody(body),
            "get_CancellationToken" => cancellationToken,
            nameof(PipeContext.TryGetPayload) => TryGetPayload(method.GetGenericArguments()[0], args, clientContext, messageContext),
            _ => Default(method.ReturnType)
        });
    }

    private static AmazonSqsMessageContext CreateMessageContext(Message message) =>
        InterfaceProxy<AmazonSqsMessageContext>.Create((method, _) => method.Name switch
        {
            "get_TransportMessage" => message,
            "get_Attributes" => message.MessageAttributes,
            _ => throw new NotSupportedException(method.Name)
        });

    private static bool TryGetPayload(Type payloadType, object?[]? arguments, ClientContext clientContext,
        AmazonSqsMessageContext? messageContext)
    {
        if (payloadType.IsInstanceOfType(clientContext))
        {
            arguments![0] = clientContext;
            return true;
        }

        if (messageContext is not null && payloadType.IsInstanceOfType(messageContext))
        {
            arguments![0] = messageContext;
            return true;
        }

        arguments![0] = null;
        return false;
    }

    private static object? Default(Type returnType) => returnType.IsValueType ? Activator.CreateInstance(returnType) : null;

    private static async Task IgnoreCancellationAsync(Task task)
    {
        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
        }
    }

    private sealed class TestMoveTransport(string destination, ConfigureAmazonSqsTopologyFilter<object> topologyFilter)
        : SqsMoveTransport<object>(destination, topologyFilter)
    {
        public TestMoveTransport(ConfigureAmazonSqsTopologyFilter<object> topologyFilter) : this("errors", topologyFilter) { }

        public Task MoveAsync(ReceiveContext context, CancellationToken cancellationToken)
        {
            return MoveAsync(context, static (_, _) => { }, cancellationToken);
        }

        public Task MoveWithPreSendAsync(ReceiveContext context,
            Action<SendMessageBatchRequestEntry, IDictionary<string, MessageAttributeValue>> preSend,
            CancellationToken cancellationToken) => base.MoveAsync(context, preSend, cancellationToken);
    }

    private sealed class TestClientContext(CancellationToken cancellationToken,
        Func<string, SendMessageBatchRequestEntry, CancellationToken, Task> send,
        Func<ViciOne.ServiceBus.AmazonSqs.Topology.Queue, CancellationToken, Task<QueueInfo>>? createQueue = null)
        : BasePipeContext(cancellationToken), ClientContext
    {
        public ConnectionContext ConnectionContext => throw new NotSupportedException();

        public Task<TopicInfo> CreateTopicAsync(Topic topic, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<QueueInfo> CreateQueueAsync(ViciOne.ServiceBus.AmazonSqs.Topology.Queue queue, CancellationToken cancellationToken) =>
            createQueue?.Invoke(queue, cancellationToken) ?? throw new NotSupportedException();

        public Task<bool> CreateQueueSubscriptionAsync(Topic topic, Queue queue, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task DeleteTopicAsync(Topic topic, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task DeleteQueueAsync(Queue queue, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task PublishAsync(string topicName, PublishBatchRequestEntry request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task SendMessageAsync(string queueName, SendMessageBatchRequestEntry request, CancellationToken cancellationToken) =>
            send(queueName, request, cancellationToken);

        public Task DeleteMessageAsync(string queueName, string receiptHandle, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task PurgeQueueAsync(string queueName, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IList<Message>> ReceiveMessagesAsync(string queueName, int messageLimit, int waitTime,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<QueueInfo> GetQueueInfoAsync(string queueName, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task ChangeMessageVisibilityAsync(string queueUrl, string receiptHandle, int seconds, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
