using System.Collections.Concurrent;
using ViciOne.ServiceBus.ActiveMqTransport.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMqTransport.LocalIntegration.Tests;

public sealed class ActiveMqMessageFlowTests
{
    private const int PublishCount = 100;

    [Theory]
    [InlineData(ActiveMqBroker.OpenWireFlavor)]
    [InlineData(ActiveMqBroker.AmqpFlavor)]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-MESSAGE-FLOW", "send-publish-count-and-unconsumed-topic-are-exact")]
    public async Task NativeFixture_SendPublishAndNoConsumerRoutesAreExact(string flavor)
    {
        using ActiveMqBroker fixture = ActiveMqBroker.Create(flavor, "messageflow");
        string queueName = fixture.Name("input");
        string publishedEntity = fixture.Name("published");
        string unconsumedEntity = fixture.Name("unconsumed");
        Guid sentId = Guid.NewGuid();
        var sent = NewObservation<Guid>();
        var published = new ConcurrentDictionary<Guid, byte>();
        var allPublished = NewObservation<bool>();
        Guid[] expectedPublished = Enumerable.Range(0, PublishCount).Select(_ => Guid.NewGuid()).ToArray();
        IBusControl bus = Bus.Factory.CreateUsingActiveMq(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.MessageTopology.GetMessageTopology<PublishedMessage>().SetEntityName(publishedEntity);
            configurator.MessageTopology.GetMessageTopology<UnconsumedMessage>().SetEntityName(unconsumedEntity);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.Durable = false;
                endpoint.AutoDelete = true;
                endpoint.PrefetchCount = PublishCount;
                endpoint.ConcurrentMessageLimit = 32;
                endpoint.Handler<SentMessage>(context =>
                {
                    sent.TrySetResult(context.Message.CorrelationId);
                    return Task.CompletedTask;
                });
                endpoint.Handler<PublishedMessage>(context =>
                {
                    if (!published.TryAdd(context.Message.CorrelationId, 0))
                        allPublished.TrySetException(new InvalidDataException("A published message was delivered more than once."));
                    else if (published.Count == PublishCount)
                        allPublished.TrySetResult(true);
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
            ISendEndpoint input = await bus.GetSendEndpoint(new Uri($"queue:{queueName}"))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            await input.Send(new SentMessage(sentId), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            Task[] publications = expectedPublished
                .Select(id => bus.Publish(new PublishedMessage(id), cancellationToken))
                .ToArray();
            await Task.WhenAll(publications).WaitAsync(fixture.OperationTimeout, cancellationToken);
            await bus.Publish(new UnconsumedMessage(Guid.NewGuid()), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(sentId, await sent.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));
            Assert.True(await allPublished.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));
            Assert.Equal(expectedPublished.Order(), published.Keys.Order());

            ActiveMqBroker.ClassicTopicStatistics unconsumed = await fixture.GetClassicTopicStatistics(
                $"VirtualTopic.{unconsumedEntity}",
                cancellationToken);
            Assert.Equal(1, unconsumed.EnqueueCount);
            Assert.Equal(0, unconsumed.ConsumerCount);
        }
        finally
        {
            if (started)
                await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
        }

        Assert.Equal(PublishCount, published.Count);
    }

    private static TaskCompletionSource<T> NewObservation<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed record SentMessage(Guid CorrelationId);
    private sealed record PublishedMessage(Guid CorrelationId);
    private sealed record UnconsumedMessage(Guid CorrelationId);
}
