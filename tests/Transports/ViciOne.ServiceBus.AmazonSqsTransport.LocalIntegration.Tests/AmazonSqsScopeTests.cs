namespace ViciOne.ServiceBus.AmazonSqsTransport.LocalIntegration.Tests;

using ViciOne.ServiceBus.AmazonSqsTransport.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.AmazonSqsTransport.Topology;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class AmazonSqsScopeTests
{
    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0241", "queue-address-is-contained-by-host-scope")]
    public async Task ScopedSend_UsesOnlyTheScopedQueue()
    {
        await using AmazonSqsLocalStack fixture = AmazonSqsLocalStack.Create("queuescope");
        string queueName = fixture.Name("input");
        Guid messageId = Guid.NewGuid();
        var consumed = new TaskCompletionSource<ScopedObservation>(TaskCreationOptions.RunContinuationsAsynchronously);
        IBusControl bus = Bus.Factory.CreateUsingAmazonSqs(configurator =>
        {
            fixture.ConfigureHost(configurator, scopeTopics: false);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.Durable = false;
                endpoint.AutoDelete = true;
                endpoint.Handler<ScopedMessage>(context =>
                {
                    consumed.TrySetResult(new ScopedObservation(context.Message.Id, context.DestinationAddress));
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
            await input.Send(new ScopedMessage(messageId), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            ScopedObservation actual = await consumed.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(messageId, actual.Id);
            Assert.Equal(
                new Uri($"amazonsqs://{fixture.Region}/{fixture.Prefix}/{queueName}"),
                actual.DestinationAddress);
            Assert.Empty(await fixture.ListOwnedTopicNames(cancellationToken));
        }
        finally
        {
            if (started)
                await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0242", "published-topic-name-is-contained-by-host-scope")]
    public async Task ScopedPublish_UsesOnlyTheScopedTopic()
    {
        await using AmazonSqsLocalStack fixture = AmazonSqsLocalStack.Create("topicscope");
        string queueName = fixture.Name("input");
        Guid messageId = Guid.NewGuid();
        var consumed = new TaskCompletionSource<ScopedObservation>(TaskCreationOptions.RunContinuationsAsynchronously);
        IBusControl bus = Bus.Factory.CreateUsingAmazonSqs(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.Durable = false;
                endpoint.AutoDelete = true;
                endpoint.Handler<ScopedMessage>(context =>
                {
                    consumed.TrySetResult(new ScopedObservation(context.Message.Id, context.DestinationAddress));
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
            await bus.Publish(new ScopedMessage(messageId), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            ScopedObservation actual = await consumed.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            string entityName = new AmazonSqsMessageNameFormatter().GetMessageName(typeof(ScopedMessage));
            string expectedTopicName = $"{fixture.Prefix}_{entityName}";

            Assert.Equal(messageId, actual.Id);
            Assert.Equal(new Uri($"amazonsqs://{fixture.Region}/{expectedTopicName}?type=topic"), actual.DestinationAddress);
            Assert.Equal([expectedTopicName], await fixture.ListOwnedTopicNames(cancellationToken));
        }
        finally
        {
            if (started)
                await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
    }

    public sealed record ScopedMessage(Guid Id);
    private sealed record ScopedObservation(Guid Id, Uri? DestinationAddress);
}
