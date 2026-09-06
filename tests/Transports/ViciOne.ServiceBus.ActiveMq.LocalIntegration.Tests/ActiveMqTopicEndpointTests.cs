using ViciOne.ServiceBus.ActiveMq.Configuration;
using ViciOne.ServiceBus.ActiveMq.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMq.LocalIntegration.Tests;

public sealed class ActiveMqTopicEndpointTests
{
    [Theory]
    [InlineData(ActiveMqBroker.OpenWireFlavor)]
    [InlineData(ActiveMqBroker.AmqpFlavor)]
    [RequirementCoverage("OBL-R0-BRK-0465", "topic-endpoint-delivers-across-classic-protocols")]
    public Task TopicEndpoint_DeliversAcrossOpenWireAndAmqpAsync(string flavor) =>
        AssertTopicDeliveryAsync(flavor, virtualTopic: false);

    [Theory]
    [InlineData(ActiveMqBroker.OpenWireFlavor)]
    [InlineData(ActiveMqBroker.AmqpFlavor)]
    [RequirementCoverage("OBL-R0-BRK-0466", "virtual-topic-endpoint-delivers-across-classic-protocols")]
    public Task VirtualTopicEndpoint_DeliversAcrossOpenWireAndAmqpAsync(string flavor) =>
        AssertTopicDeliveryAsync(flavor, virtualTopic: true);

    private static async Task AssertTopicDeliveryAsync(string flavor, bool virtualTopic)
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
            ISendEndpoint topic = await bus.GetSendEndpointAsync(new Uri($"topic:{topicName}"))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await topic.SendAsync(new TopicMessage(flowId), cancellationToken)
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
