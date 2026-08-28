namespace ViciOne.ServiceBus.ActiveMqTransport.LocalIntegration.Tests;

using ViciOne.ServiceBus.ActiveMqTransport.Configuration;
using ViciOne.ServiceBus.ActiveMqTransport.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class ActiveMqTopicEndpointTests
{
    [Theory]
    [InlineData(ActiveMqBroker.OpenWireFlavor)]
    [InlineData(ActiveMqBroker.AmqpFlavor)]
    [RequirementCoverage("OBL-R0-BRK-0465", "topic-endpoint-delivers-across-classic-protocols")]
    public Task TopicEndpoint_DeliversAcrossOpenWireAndAmqp(string flavor) =>
        AssertTopicDelivery(flavor, virtualTopic: false);

    [Theory]
    [InlineData(ActiveMqBroker.OpenWireFlavor)]
    [InlineData(ActiveMqBroker.AmqpFlavor)]
    [RequirementCoverage("OBL-R0-BRK-0466", "virtual-topic-endpoint-delivers-across-classic-protocols")]
    public Task VirtualTopicEndpoint_DeliversAcrossOpenWireAndAmqp(string flavor) =>
        AssertTopicDelivery(flavor, virtualTopic: true);

    private static async Task AssertTopicDelivery(string flavor, bool virtualTopic)
    {
        using ActiveMqBroker fixture = ActiveMqBroker.Create(
            flavor,
            virtualTopic ? "virtual-topic" : "topic");
        string inputQueue = fixture.Name("input");
        string topicName = virtualTopic
            ? $"VirtualTopic.{fixture.Name("private")}" 
            : fixture.Name("private");
        Guid flowId = Guid.NewGuid();
        var delivered = NewObservation<Guid>();
        var deliveryCount = 0;
        IBusControl bus = Bus.Factory.CreateUsingActiveMq(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(inputQueue, endpoint =>
            {
                endpoint.ConfigureConsumeTopology = false;
                endpoint.Bind(topicName);
                endpoint.Handler<TopicMessage>(context =>
                {
                    Interlocked.Increment(ref deliveryCount);
                    delivered.TrySetResult(context.Message.FlowId);
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
            ISendEndpoint topic = await bus.GetSendEndpoint(new Uri($"topic:{topicName}"))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await topic.Send(new TopicMessage(flowId), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(flowId, await delivered.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));
            Assert.Equal(1, Volatile.Read(ref deliveryCount));
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    private static TaskCompletionSource<T> NewObservation<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public sealed record TopicMessage(Guid FlowId);
}

public sealed class ActiveMqSharedSubscriptionTests
{
    private const int MessageCount = 20;

    [Fact]
    [RequirementCoverage("OBL-R0-BRK-0462", "artemis-amqp-shared-durable-subscription-load-balances-exactly-once")]
    public async Task ArtemisAmqpSharedDurableSubscription_LoadBalancesExactlyOnce()
    {
        using ActiveMqBroker fixture = ActiveMqBroker.Create(ActiveMqBroker.ArtemisFlavor, "shared-subscription");
        string topicName = fixture.Name("shared");
        string consumerName = fixture.Name("subscriber");
        Guid[] expected = [.. Enumerable.Range(0, MessageCount).Select(_ => Guid.NewGuid())];
        var deliveries = new System.Collections.Concurrent.ConcurrentDictionary<Guid, string>();
        var duplicate = NewObservation<Guid>();
        var allDelivered = NewObservation<bool>();
        IBusControl firstBus = CreateBus(fixture.Name("consumer-a"), "a");
        IBusControl secondBus = CreateBus(fixture.Name("consumer-b"), "b");
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool firstStarted = false;
        bool secondStarted = false;

        try
        {
            await firstBus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            firstStarted = true;
            await secondBus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            secondStarted = true;
            ISendEndpoint topic = await firstBus.GetSendEndpoint(new Uri($"topic:{topicName}"))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await Task.WhenAll(expected.Select(flowId => topic.Send(new SharedMessage(flowId), cancellationToken)))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            Task finished = await Task.WhenAny(allDelivered.Task, duplicate.Task)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            if (ReferenceEquals(finished, duplicate.Task))
                throw new InvalidDataException($"Message '{await duplicate.Task}' was delivered more than once.");
            await allDelivered.Task;

            Assert.Equal(expected.Order(), deliveries.Keys.Order());
            Assert.Equal(MessageCount, deliveries.Count);
            Assert.Equal(["a", "b"], deliveries.Values.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal));
        }
        finally
        {
            if (secondStarted)
                await secondBus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            if (firstStarted)
                await firstBus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }

        IBusControl CreateBus(string endpointName, string owner)
        {
            return Bus.Factory.CreateUsingActiveMq(configurator =>
            {
                fixture.ConfigureHost(configurator);
                configurator.ReceiveEndpoint(endpointName, endpoint =>
                {
                    endpoint.ConfigureConsumeTopology = false;
                    endpoint.Bind(topicName, specification =>
                    {
                        var topic = Assert.IsType<ConsumerConsumeTopicTopologySpecification>(specification);
                        topic.Shared = true;
                        topic.ConsumerName = consumerName;
                    });
                    endpoint.Handler<SharedMessage>(context =>
                    {
                        if (!deliveries.TryAdd(context.Message.FlowId, owner))
                            duplicate.TrySetResult(context.Message.FlowId);
                        if (deliveries.Count == MessageCount)
                            allDelivered.TrySetResult(true);
                        return Task.CompletedTask;
                    });
                });
            });
        }
    }

    private static TaskCompletionSource<T> NewObservation<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public sealed record SharedMessage(Guid FlowId);
}
