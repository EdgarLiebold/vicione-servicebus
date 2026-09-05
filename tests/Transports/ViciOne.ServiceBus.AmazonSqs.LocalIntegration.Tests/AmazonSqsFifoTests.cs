using System.Collections.Concurrent;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.AmazonSqs.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.LocalIntegration.Tests;

public sealed class AmazonSqsFifoTests
{
    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0208", "fifo-topic-and-queue-deliver-one-grouped-publish")]
    public async Task FifoTopicAndQueue_PreserveExactGroupOrderAsync()
    {
        await using AmazonSqsLocalStack fixture = AmazonSqsLocalStack.Create("fifopublish");
        using AmazonSQSClient sqs = fixture.CreateSqsClient();
        using AmazonSimpleNotificationServiceClient sns = fixture.CreateSnsClient();
        string queueName = fixture.Name("orderedqueue", fifo: true);
        string topicName = fixture.Name("orderedtopic", fifo: true);
        Guid correlationId = Guid.NewGuid();
        string groupId = Guid.NewGuid().ToString("N");
        var received = new TaskCompletionSource<ObservedFifoMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        IBusControl bus = Bus.Factory.CreateUsingAmazonSqs(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.Message<FifoPublishedMessage>(message => message.SetEntityName(topicName));
            configurator.Publish<FifoPublishedMessage>(publish =>
                publish.TopicAttributes[QueueAttributeName.ContentBasedDeduplication] = true);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.QueueAttributes[QueueAttributeName.ContentBasedDeduplication] = true;
                endpoint.Handler<FifoPublishedMessage>(context =>
                {
                    received.TrySetResult(new ObservedFifoMessage(
                        context.Message.CorrelationId,
                        GetGroupId(context)));
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
            await bus.PublishAsync(
                    new FifoPublishedMessage(correlationId),
                    context => context.SetGroupId(groupId),
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(
                new ObservedFifoMessage(correlationId, groupId),
                await received.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));
            Assert.Equal([queueName], await ListOwnedQueueNamesAsync(sqs, fixture.Prefix, fixture.OperationTimeout, cancellationToken));
            string actualTopic = Assert.Single(await ListOwnedTopicNamesAsync(sns, fixture.Prefix, fixture.OperationTimeout, cancellationToken));
            Assert.EndsWith(topicName, actualTopic, StringComparison.Ordinal);
        }
        finally
        {
            if (started)
                await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-FIFO-PUBLISH", "missing-group-id-is-rejected-by-provider")]
    public async Task FifoPublishWithoutGroupId_IsRejectedAsync()
    {
        await using AmazonSqsLocalStack fixture = AmazonSqsLocalStack.Create("fifonogroup");
        string topicName = fixture.Name("orderedtopic", fifo: true);
        IBusControl bus = Bus.Factory.CreateUsingAmazonSqs(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.Message<FifoPublishedMessage>(message => message.SetEntityName(topicName));
            configurator.Publish<FifoPublishedMessage>(publish =>
                publish.TopicAttributes[QueueAttributeName.ContentBasedDeduplication] = true);
        });
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;

            await Assert.ThrowsAsync<InvalidParameterException>(() =>
                bus.PublishAsync(new FifoPublishedMessage(Guid.NewGuid()), cancellationToken));
        }
        finally
        {
            if (started)
                await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0209", "global-formatters-create-working-fifo-topology")]
    public async Task EntityFormatter_ProducesValidFifoTopicAndQueueNamesAsync()
    {
        await using AmazonSqsLocalStack fixture = AmazonSqsLocalStack.Create("fifoformatters");
        using AmazonSQSClient sqs = fixture.CreateSqsClient();
        using AmazonSimpleNotificationServiceClient sns = fixture.CreateSnsClient();
        var received = new TaskCompletionSource<Guid>(TaskCreationOptions.RunContinuationsAsynchronously);
        var endpointFormatter = new FifoEndpointNameFormatter(KebabCaseEndpointNameFormatter.Instance);
        var services = new ServiceCollection();
        services.AddSingleton(received);
        services.AddViciOneServiceBus(registration =>
        {
            registration.SetEndpointNameFormatter(endpointFormatter);
            registration.AddConsumer<FormatterMessageConsumer>();
            registration.AddConfigureEndpointsCallback((_, endpoint) =>
            {
                if (endpoint is IAmazonSqsReceiveEndpointConfigurator amazonEndpoint)
                    amazonEndpoint.QueueAttributes[QueueAttributeName.ContentBasedDeduplication] = true;
            });
            registration.UsingAmazonSqs((context, configurator) =>
            {
                fixture.ConfigureHost(configurator);
                configurator.MessageTopology.SetEntityNameFormatter(new FifoEntityNameFormatter());
                configurator.PublishTopology.TopicAttributes[QueueAttributeName.ContentBasedDeduplication] = true;
                configurator.ConfigureEndpoints(context);
            });
        });
        await using ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);
        IBusControl bus = provider.GetRequiredService<IBusControl>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            Guid expected = Guid.NewGuid();
            await bus.PublishAsync(
                    new FormatterMessage(expected),
                    context => context.SetGroupId(expected.ToString("N")),
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(expected, await received.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));
            Assert.All(
                new[]
                {
                    endpointFormatter.TemporaryEndpoint("probe"),
                    endpointFormatter.Consumer<FormatterMessageConsumer>(),
                    endpointFormatter.Message<FormatterMessage>(),
                    endpointFormatter.Saga<FormatterSaga>(),
                    endpointFormatter.ExecuteActivity<FormatterExecuteActivity, FormatterArguments>(),
                    endpointFormatter.CompensateActivity<FormatterCompensateActivity, FormatterLog>(),
                },
                name => Assert.EndsWith(".fifo", name, StringComparison.Ordinal));
            Assert.All(
                await ListOwnedQueueNamesAsync(sqs, fixture.Prefix, fixture.OperationTimeout, cancellationToken),
                name => Assert.EndsWith(".fifo", name, StringComparison.Ordinal));
            Assert.All(
                await ListOwnedTopicNamesAsync(sns, fixture.Prefix, fixture.OperationTimeout, cancellationToken),
                name => Assert.EndsWith(".fifo", name, StringComparison.Ordinal));
        }
        finally
        {
            if (started)
                await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0210", "one-message-group-is-delivered-in-exact-sequence")]
    public async Task OneGroup_IsDeliveredInStrictSequenceAsync()
    {
        const int messageCount = 20;
        await using AmazonSqsLocalStack fixture = AmazonSqsLocalStack.Create("fifoonegroup");
        string queueName = fixture.Name("input", fifo: true);
        string groupId = Guid.NewGuid().ToString("N");
        var received = new ConcurrentQueue<int>();
        var allReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int count = 0;
        IBusControl bus = Bus.Factory.CreateUsingAmazonSqs(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.ConfigureConsumeTopology = false;
                endpoint.QueueAttributes[QueueAttributeName.ContentBasedDeduplication] = true;
                endpoint.ConcurrentMessageLimit = 8;
                endpoint.ConcurrentDeliveryLimit = 1;
                endpoint.Handler<OrderedMessage>(context =>
                {
                    received.Enqueue(context.Message.Index);
                    if (Interlocked.Increment(ref count) == messageCount)
                        allReceived.TrySetResult();
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
            ISendEndpoint endpoint = await bus.GetSendEndpointAsync(new Uri($"queue:{queueName}"), TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            for (int index = 0; index < messageCount; index++)
            {
                await endpoint.SendAsync(
                        new OrderedMessage(groupId, index),
                        context => context.SetGroupId(groupId),
                        cancellationToken)
                    .WaitAsync(fixture.OperationTimeout, cancellationToken);
            }

            await allReceived.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.Equal(Enumerable.Range(0, messageCount), received);
        }
        finally
        {
            if (started)
                await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0211", "groups-remain-ordered-and-one-blocked-group-does-not-block-another")]
    public async Task MultipleGroups_AreEachOrderedAndCanProgressIndependentlyAsync()
    {
        const int messagesPerGroup = 10;
        await using AmazonSqsLocalStack fixture = AmazonSqsLocalStack.Create("fifogroups");
        string queueName = fixture.Name("input", fifo: true);
        (string blockedGroup, string progressingGroup) = CreateDistinctPartitionGroups(messagesPerGroup * 2);
        var blockedEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseBlocked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var progressingComplete = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var allReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var blockedReceived = new ConcurrentQueue<int>();
        var progressingReceived = new ConcurrentQueue<int>();
        int progressingCount = 0;
        int totalCount = 0;
        IBusControl bus = Bus.Factory.CreateUsingAmazonSqs(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.ConfigureConsumeTopology = false;
                endpoint.QueueAttributes[QueueAttributeName.ContentBasedDeduplication] = true;
                endpoint.ConcurrentMessageLimit = messagesPerGroup * 2;
                endpoint.ConcurrentDeliveryLimit = 1;
                endpoint.Handler<OrderedMessage>(async context =>
                {
                    if (context.Message.GroupId == blockedGroup)
                    {
                        if (context.Message.Index == 0)
                        {
                            blockedEntered.TrySetResult();
                            await releaseBlocked.Task.WaitAsync(fixture.OperationTimeout, context.CancellationToken);
                        }
                        blockedReceived.Enqueue(context.Message.Index);
                    }
                    else if (context.Message.GroupId == progressingGroup)
                    {
                        progressingReceived.Enqueue(context.Message.Index);
                        if (Interlocked.Increment(ref progressingCount) == messagesPerGroup)
                            progressingComplete.TrySetResult();
                    }
                    else
                        throw new InvalidDataException($"Unexpected FIFO group '{context.Message.GroupId}'.");

                    if (Interlocked.Increment(ref totalCount) == messagesPerGroup * 2)
                        allReceived.TrySetResult();
                });
            });
        });
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            ISendEndpoint endpoint = await bus.GetSendEndpointAsync(new Uri($"queue:{queueName}"), TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            await SendOrderedAsync(endpoint, blockedGroup, 0, fixture.OperationTimeout, cancellationToken);
            await blockedEntered.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);

            for (int index = 0; index < messagesPerGroup; index++)
                await SendOrderedAsync(endpoint, progressingGroup, index, fixture.OperationTimeout, cancellationToken);
            await progressingComplete.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.Empty(blockedReceived);

            for (int index = 1; index < messagesPerGroup; index++)
                await SendOrderedAsync(endpoint, blockedGroup, index, fixture.OperationTimeout, cancellationToken);
            releaseBlocked.TrySetResult();
            await allReceived.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(Enumerable.Range(0, messagesPerGroup), blockedReceived);
            Assert.Equal(Enumerable.Range(0, messagesPerGroup), progressingReceived);
        }
        finally
        {
            releaseBlocked.TrySetResult();
            if (started)
                await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0270", "disabled-ordering-dispatches-one-group-concurrently")]
    public async Task DisableOrdering_AllowsIndependentSameGroupDispatchAsync()
    {
        await using AmazonSqsLocalStack fixture = AmazonSqsLocalStack.Create("fifounordered");
        string queueName = fixture.Name("input", fifo: true);
        string groupId = Guid.NewGuid().ToString("N");
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        IBusControl sender = Bus.Factory.CreateUsingAmazonSqs(fixture.ConfigureHost);
        bool senderStarted = false;
        try
        {
            await sender.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            senderStarted = true;
            ISendEndpoint input = await sender.GetSendEndpointAsync(new Uri($"queue:{queueName}"), TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            for (int index = 0; index < 2; index++)
            {
                await input.SendAsync(
                        new OrderedMessage(groupId, index),
                        context =>
                        {
                            context.SetGroupId(groupId);
                            context.SetDeduplicationId($"{groupId}-{index}");
                        },
                        cancellationToken)
                    .WaitAsync(fixture.OperationTimeout, cancellationToken);
            }
        }
        finally
        {
            if (senderStarted)
                await sender.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }

        var bothEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var allCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var received = new ConcurrentDictionary<int, byte>();
        var active = 0;
        var completed = 0;
        IBusControl receiver = Bus.Factory.CreateUsingAmazonSqs(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.ConfigureConsumeTopology = false;
                endpoint.PrefetchCount = 2;
                endpoint.ConcurrentMessageLimit = 2;
                endpoint.DisableMessageOrdering();
                endpoint.Handler<OrderedMessage>(async context =>
                {
                    received.TryAdd(context.Message.Index, 0);
                    if (Interlocked.Increment(ref active) == 2)
                        bothEntered.TrySetResult();
                    try
                    {
                        await release.Task.WaitAsync(fixture.OperationTimeout, context.CancellationToken);
                    }
                    finally
                    {
                        Interlocked.Decrement(ref active);
                    }

                    if (Interlocked.Increment(ref completed) == 2)
                        allCompleted.TrySetResult();
                });
            });
        });
        bool receiverStarted = false;

        try
        {
            await receiver.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            receiverStarted = true;
            await bothEntered.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.Equal(2, Volatile.Read(ref active));
            Assert.Equal([0, 1], received.Keys.Order());

            release.TrySetResult();
            await allCompleted.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.Equal(2, Volatile.Read(ref completed));
        }
        finally
        {
            release.TrySetResult();
            if (receiverStarted)
                await receiver.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    private static Task SendOrderedAsync(
        ISendEndpoint endpoint,
        string groupId,
        int index,
        TimeSpan timeout,
        CancellationToken cancellationToken) =>
        endpoint.SendAsync(
                new OrderedMessage(groupId, index),
                context => context.SetGroupId(groupId),
                cancellationToken)
            .WaitAsync(timeout, cancellationToken);

    private static (string First, string Second) CreateDistinctPartitionGroups(int partitionCount)
    {
        var hash = new Murmur3UnsafeHashGenerator();
        string first = Guid.NewGuid().ToString("N");
        uint firstPartition = hash.Hash(System.Text.Encoding.UTF8.GetBytes(first)) % (uint)partitionCount;
        string second;
        do
        {
            second = Guid.NewGuid().ToString("N");
        }
        while (hash.Hash(System.Text.Encoding.UTF8.GetBytes(second)) % (uint)partitionCount == firstPartition);

        return (first, second);
    }

    private static string? GetGroupId<T>(ConsumeContext<T> context)
        where T : class
    {
        if (!context.TryGetPayload(out AmazonSqsMessageContext? amazonContext))
            throw new InvalidDataException("The receive context is missing its Amazon SQS transport payload.");

        return amazonContext.TransportMessage.Attributes != null
            && amazonContext.TransportMessage.Attributes.TryGetValue(MessageSystemAttributeName.MessageGroupId, out string? groupId)
                ? groupId
                : null;
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

    private static async Task<string[]> ListOwnedTopicNamesAsync(
        IAmazonSimpleNotificationService sns,
        string prefix,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        ListTopicsResponse response = await sns.ListTopicsAsync(new ListTopicsRequest(), cancellationToken)
            .WaitAsync(timeout, cancellationToken);
        return (response.Topics ?? [])
            .Select(topic => topic.TopicArn[(topic.TopicArn.LastIndexOf(':') + 1)..])
            .Where(name => name.StartsWith(prefix, StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private sealed record FifoPublishedMessage(Guid CorrelationId);
    private sealed record ObservedFifoMessage(Guid CorrelationId, string? GroupId);
    private sealed record OrderedMessage(string GroupId, int Index);
    public sealed record FormatterMessage(Guid CorrelationId);
    public sealed record FormatterArguments;
    public sealed record FormatterLog;

    public sealed class FormatterMessageConsumer(TaskCompletionSource<Guid> received) : IConsumer<FormatterMessage>
    {
        public Task ConsumeAsync(ConsumeContext<FormatterMessage> context)
        {
            received.TrySetResult(context.Message.CorrelationId);
            return Task.CompletedTask;
        }
    }

    public sealed class FormatterSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    public sealed class FormatterExecuteActivity : IExecuteActivity<FormatterArguments>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<FormatterArguments> context) =>
            Task.FromResult(context.Completed());
    }

    public sealed class FormatterCompensateActivity : ICompensateActivity<FormatterLog>
    {
        public Task<CompensationResult> CompensateAsync(CompensateContext<FormatterLog> context) =>
            Task.FromResult(context.Compensated());
    }

    private sealed class FifoEntityNameFormatter : IEntityNameFormatter
    {
        public string FormatEntityName<T>() => $"{typeof(T).Name}.fifo";
    }

    private sealed class FifoEndpointNameFormatter(IEndpointNameFormatter inner) : IEndpointNameFormatter
    {
        public string Separator => inner.Separator;
        public string TemporaryEndpoint(string tag) => inner.TemporaryEndpoint(tag) + ".fifo";
        public string Consumer<T>() where T : class, IConsumer => inner.Consumer<T>() + ".fifo";
        public string Message<T>() where T : class => inner.Message<T>() + ".fifo";
        public string Saga<T>() where T : class => inner.Saga<T>() + ".fifo";
        public string ExecuteActivity<T, TArguments>()
            where T : class
            where TArguments : class => inner.ExecuteActivity<T, TArguments>() + ".fifo";
        public string CompensateActivity<T, TLog>()
            where T : class
            where TLog : class => inner.CompensateActivity<T, TLog>() + ".fifo";
        public string SanitizeName(string name) => inner.SanitizeName(name);
    }
}
