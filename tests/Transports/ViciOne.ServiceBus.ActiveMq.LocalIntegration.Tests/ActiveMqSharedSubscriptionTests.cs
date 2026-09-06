using ViciOne.ServiceBus.ActiveMq.Configuration;
using ViciOne.ServiceBus.ActiveMq.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMq.LocalIntegration.Tests;

public sealed class ActiveMqSharedSubscriptionTests
{
    private const int MessageCount = 20;

    [Fact]
    [RequirementCoverage("OBL-R0-BRK-0462", "artemis-amqp-shared-durable-subscription-load-balances-exactly-once")]
    public async Task ArtemisAmqpSharedDurableSubscription_LoadBalancesExactlyOnceAsync()
    {
        using ActiveMqBroker fixture = ActiveMqBroker.Create(ActiveMqBroker.ArtemisFlavor, "shared-subscription");
        string topicName = fixture.Name("shared");
        string consumerName = fixture.Name("subscriber");
        Guid[] expected = [.. Enumerable.Range(0, MessageCount).Select(_ => Guid.NewGuid())];
        var deliveries = new System.Collections.Concurrent.ConcurrentDictionary<Guid, string>();
        var duplicate = NewObservation<Guid>();
        var allDelivered = NewObservation<bool>();
        var receives = new ReceiveCompletionObserver(MessageCount);
        IBusControl firstBus = CreateBus(fixture.Name("consumer-a"), "a");
        IBusControl secondBus = CreateBus(fixture.Name("consumer-b"), "b");
        using ConnectHandle firstReceiveHandle = firstBus.ConnectReceiveObserver(receives);
        using ConnectHandle secondReceiveHandle = secondBus.ConnectReceiveObserver(receives);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool firstStarted = false;
        bool secondStarted = false;

        try
        {
            await firstBus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            firstStarted = true;
            await secondBus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            secondStarted = true;
            ISendEndpoint topic = await firstBus.GetSendEndpointAsync(new Uri($"topic:{topicName}"), TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            await Task.WhenAll(expected.Select(flowId => topic.SendAsync(new SharedMessage(flowId), cancellationToken)))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            Task finished = await Task.WhenAny(allDelivered.Task, duplicate.Task)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            if (ReferenceEquals(finished, duplicate.Task))
                throw new InvalidDataException($"Message '{await duplicate.Task}' was delivered more than once.");
            await allDelivered.Task;
            await receives.Completed.WaitAsync(fixture.OperationTimeout, cancellationToken);

            await secondBus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            secondStarted = false;
            await firstBus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            firstStarted = false;

            Assert.Equal(expected.Order(), deliveries.Keys.Order());
            Assert.Equal(MessageCount, deliveries.Count);
            Assert.False(duplicate.Task.IsCompleted, duplicate.Task.Exception?.ToString());
            Assert.Equal(MessageCount, receives.CompletedCount);
            Assert.Equal(["a", "b"], deliveries.Values.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal));
            Assert.Equal(
                new ActiveMqBroker.BrokerQueueStatistics(MessageCount, MessageCount, 0, 0, 0),
                await fixture.GetQueueStatisticsAsync(consumerName, cancellationToken));
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
