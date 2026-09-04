using ViciOne.ServiceBus.AmazonSqsTransport.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqsTransport.LocalIntegration.Tests;

public sealed class AmazonSqsTopicEndpointTests
{
    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0246", "explicit-topic-send-reaches-only-its-subscribed-queue")]
    public async Task SendToTopicEndpoint_ReachesTheSubscribedQueue()
    {
        await using AmazonSqsLocalStack fixture = AmazonSqsLocalStack.Create("topicendpoint");
        string queueName = fixture.Name("input");
        const string topicName = "private-topic";
        string physicalTopicName = $"{fixture.Prefix}_{topicName}";
        Guid messageId = Guid.NewGuid();
        var consumed = new TaskCompletionSource<Guid>(TaskCreationOptions.RunContinuationsAsynchronously);
        IBusControl bus = Bus.Factory.CreateUsingAmazonSqs(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.Durable = false;
                endpoint.AutoDelete = true;
                endpoint.ConfigureConsumeTopology = false;
                // A topic: URI is relative to the host scope and therefore resolves to the physical
                // scope-prefixed name. An explicit subscription accepts a physical SNS topic name.
                endpoint.Subscribe(physicalTopicName);
                endpoint.Handler<TopicMessage>(context =>
                {
                    consumed.TrySetResult(context.Message.Id);
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
            await topic.Send(new TopicMessage(messageId), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(messageId, await consumed.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));
            Assert.Equal([physicalTopicName], await fixture.ListOwnedTopicNames(cancellationToken));
        }
        finally
        {
            if (started)
                await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
    }

    private sealed record TopicMessage(Guid Id);
}
