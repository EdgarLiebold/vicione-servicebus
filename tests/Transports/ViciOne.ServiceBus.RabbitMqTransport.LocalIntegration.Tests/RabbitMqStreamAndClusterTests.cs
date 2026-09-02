namespace ViciOne.ServiceBus.RabbitMqTransport.LocalIntegration.Tests;

using Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class RabbitMqStreamAndClusterTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-NATIVE-STREAM", "stream-retention-and-from-first-delivery")]
    public async Task StreamQueue_DeclaresRetentionAndConsumesThePublishedMessageFromFirst()
    {
        using RabbitMqBroker fixture = RabbitMqBroker.Create("stream");
        string queue = fixture.Name("events");
        Guid expected = NewId.NextGuid();
        var received = NewObservation<Guid>();
        int entries = 0;
        IBusControl bus = Bus.Factory.CreateUsingRabbitMq(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queue, endpoint =>
            {
                endpoint.Stream("native-reader", stream =>
                {
                    stream.MaxAge = TimeSpan.FromDays(14);
                    stream.FromFirst();
                });
                endpoint.Handler<StreamMessage>(context =>
                {
                    if (Interlocked.Increment(ref entries) != 1)
                        received.TrySetException(new InvalidDataException("The stream message was delivered more than once."));
                    else
                        received.TrySetResult(context.Message.Identity);
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
            RabbitMqBroker.QueueState state = await fixture.Queue(queue, cancellationToken);
            Assert.Equal("stream", state.Arguments["x-queue-type"]);
            Assert.Equal("14D", state.Arguments["x-max-age"]);

            await bus.Publish(new StreamMessage(expected), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.Equal(expected, await received.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));

            await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            started = false;
            Assert.Equal(1, entries);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            await fixture.CleanupAsync();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-NATIVE-CLUSTER", "logical-host-address-with-real-cluster-node")]
    public async Task ClusterNode_ConnectsThroughTheRealNodeWhileMessagesKeepTheLogicalHost()
    {
        using RabbitMqBroker fixture = RabbitMqBroker.Create("cluster");
        string queue = fixture.Name("input");
        var logicalAddress = new Uri("rabbitmq://logical-cluster/");
        Guid expected = NewId.NextGuid();
        var received = NewObservation<ConsumeContext<ClusterMessage>>();
        int entries = 0;
        IBusControl bus = Bus.Factory.CreateUsingRabbitMq(configurator =>
        {
            fixture.ConfigureClusteredHost(configurator, logicalAddress);
            configurator.ReceiveEndpoint(queue, endpoint => endpoint.Handler<ClusterMessage>(context =>
            {
                if (Interlocked.Increment(ref entries) != 1)
                    received.TrySetException(new InvalidDataException("The clustered message was delivered more than once."));
                else
                    received.TrySetResult(context);
                return Task.CompletedTask;
            }));
        });
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            ISendEndpoint endpoint = await bus.GetSendEndpoint(new Uri($"queue:{queue}"))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await endpoint.Send(new ClusterMessage(expected), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            ConsumeContext<ClusterMessage> actual = await received.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(expected, actual.Message.Identity);
            Assert.Equal(logicalAddress.Host, actual.DestinationAddress!.Host);
            Assert.Equal(logicalAddress.Host, actual.SourceAddress!.Host);

            await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            started = false;
            Assert.Equal(1, entries);
            Assert.Equal(0, (await fixture.Queue(queue, cancellationToken)).Messages);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            await fixture.CleanupAsync();
        }
    }

    private static TaskCompletionSource<T> NewObservation<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed record StreamMessage(Guid Identity);

    private sealed record ClusterMessage(Guid Identity);
}
