using System.Collections.Concurrent;
using ViciOne.ServiceBus.RabbitMq.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.LocalIntegration.Tests;

public sealed class RabbitMqLifecycleTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-NATIVE-LIFECYCLE", "prestart-bind-and-purge-on-start")]
    public async Task BoundQueue_BuffersBeforeEndpointStartAndPurgeRemovesOnlyTheStaleGenerationAsync()
    {
        using RabbitMqBroker fixture = RabbitMqBroker.Create("purge");
        string queue = fixture.Name("input");
        Guid stale = NewId.NextGuid();
        Guid fresh = NewId.NextGuid();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        IBusControl producer = Bus.Factory.CreateUsingRabbitMq(fixture.ConfigureHost);
        bool producerStarted = false;
        try
        {
            await producer.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            producerStarted = true;
            ISendEndpoint bound = await producer.GetSendEndpointAsync(new Uri($"queue:{queue}?bind=true"), TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            await bound.SendAsync(new LifecycleMessage(stale), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await producer.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            producerStarted = false;
            Assert.Equal(1U, await fixture.QueueMessageCountAsync(queue, cancellationToken));
        }
        finally
        {
            if (producerStarted)
                await producer.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }

        var received = NewObservation<Guid>();
        var identities = new ConcurrentDictionary<Guid, int>();
        IBusControl consumer = Bus.Factory.CreateUsingRabbitMq(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queue, endpoint =>
            {
                endpoint.PurgeOnStartup = true;
                endpoint.Handler<LifecycleMessage>(context =>
                {
                    int count = identities.AddOrUpdate(context.Message.Identity, 1, (_, value) => value + 1);
                    if (count != 1 || context.Message.Identity != fresh)
                        received.TrySetException(new InvalidDataException(
                            $"Purge admitted stale or duplicate identity {context.Message.Identity}."));
                    else
                        received.TrySetResult(context.Message.Identity);
                    return Task.CompletedTask;
                });
            });
        });
        bool consumerStarted = false;
        try
        {
            await consumer.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            consumerStarted = true;
            Assert.Equal(0U, await fixture.QueueMessageCountAsync(queue, cancellationToken));
            await SendAsync(consumer, queue, fresh, fixture, cancellationToken);
            Assert.Equal(fresh, await received.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));
            await consumer.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            consumerStarted = false;

            Assert.Single(identities);
            Assert.Equal(1, identities[fresh]);
            Assert.DoesNotContain(stale, identities.Keys);
            Assert.Equal(0U, await fixture.QueueMessageCountAsync(queue, cancellationToken));
        }
        finally
        {
            if (consumerStarted)
                await consumer.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            await fixture.CleanupAsync();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-NATIVE-LIFECYCLE", "restart-rebinds-and-drains-without-duplicate")]
    public async Task StopAndRestart_RebindsTheEndpointAndDrainsEachGenerationExactlyOnceAsync()
    {
        using RabbitMqBroker fixture = RabbitMqBroker.Create("restart");
        string queue = fixture.Name("input");
        Guid first = NewId.NextGuid();
        Guid second = NewId.NextGuid();
        var firstReceived = NewObservation();
        var secondReceived = NewObservation();
        var identities = new ConcurrentDictionary<Guid, int>();
        IBusControl bus = Bus.Factory.CreateUsingRabbitMq(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queue, endpoint =>
            {
                endpoint.Durable = true;
                endpoint.AutoDelete = false;
                endpoint.Handler<LifecycleMessage>(context =>
                {
                    int count = identities.AddOrUpdate(context.Message.Identity, 1, (_, value) => value + 1);
                    if (count != 1)
                        throw new InvalidDataException($"Lifecycle message {context.Message.Identity} was duplicated.");
                    if (context.Message.Identity == first)
                        firstReceived.TrySetResult();
                    else if (context.Message.Identity == second)
                        secondReceived.TrySetResult();
                    else
                        throw new InvalidDataException($"Unexpected lifecycle identity {context.Message.Identity}.");
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
            await SendAsync(bus, queue, first, fixture, cancellationToken);
            await firstReceived.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            started = false;
            RabbitMqBroker.QueueState stopped = await fixture.QueueAsync(queue, cancellationToken);
            Assert.Equal(0, stopped.Consumers);
            Assert.Equal(0, stopped.Messages);

            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            await SendAsync(bus, queue, second, fixture, cancellationToken);
            await secondReceived.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            started = false;

            Assert.Equal(2, identities.Count);
            Assert.All(identities.Values, count => Assert.Equal(1, count));
            RabbitMqBroker.QueueState terminal = await fixture.QueueAsync(queue, cancellationToken);
            Assert.Equal(0, terminal.Consumers);
            Assert.Equal(0, terminal.Messages);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            await fixture.CleanupAsync();
        }
    }

    private static async Task SendAsync(
        IBus bus,
        string queue,
        Guid identity,
        RabbitMqBroker fixture,
        CancellationToken cancellationToken)
    {
        ISendEndpoint endpoint = await bus.GetSendEndpointAsync(new Uri($"queue:{queue}"), cancellationToken: cancellationToken)
            .WaitAsync(fixture.OperationTimeout, cancellationToken);
        await endpoint.SendAsync(new LifecycleMessage(identity), cancellationToken)
            .WaitAsync(fixture.OperationTimeout, cancellationToken);
    }

    private static TaskCompletionSource NewObservation() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static TaskCompletionSource<T> NewObservation<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed record LifecycleMessage(Guid Identity);
}
