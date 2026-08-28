namespace ViciOne.ServiceBus.ActiveMqTransport.LocalIntegration.Tests;

using System.Collections.Concurrent;
using ViciOne.ServiceBus.ActiveMqTransport.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.ActiveMqTransport.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Brokers;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class ActiveMqFixtureRecoveryTests
{
    [Theory]
    [InlineData(ActiveMqBroker.OpenWireFlavor)]
    [InlineData(ActiveMqBroker.AmqpFlavor)]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-BROKER-RECOVERY", "run-scoped-outage-recovers-the-configured-endpoint")]
    public async Task RunScopedOutageControl_RestoresStableEndpoints(string flavor)
    {
        using ActiveMqBroker fixture = ActiveMqBroker.Create(flavor, "broker-recovery");
        string queueName = fixture.Name("input");
        Guid before = Guid.NewGuid();
        Guid after = Guid.NewGuid();
        Guid barrier = Guid.NewGuid();
        var received = new ConcurrentDictionary<Guid, int>();
        var beforeObserved = NewObservation<bool>();
        var afterObserved = NewObservation<bool>();
        var barrierEntered = NewObservation<bool>();
        var releaseBarrier = NewObservation<bool>();
        var observer = new ReceiveEndpointRecoveryObserver(queueName);
        IBusControl bus = Bus.Factory.CreateUsingActiveMq(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.PrefetchCount = 1;
                endpoint.ConcurrentMessageLimit = 1;
                endpoint.Handler<RecoveryMessage>(async context =>
                {
                    int deliveries = received.AddOrUpdate(context.Message.FlowId, 1, static (_, count) => count + 1);
                    if (deliveries != 1)
                        throw new InvalidDataException($"Message '{context.Message.FlowId}' was delivered more than once.");

                    if (context.Message.FlowId == before)
                        beforeObserved.TrySetResult(true);
                    else if (context.Message.FlowId == after)
                        afterObserved.TrySetResult(true);
                    else if (context.Message.FlowId == barrier)
                    {
                        barrierEntered.TrySetResult(true);
                        await releaseBarrier.Task.WaitAsync(fixture.OperationTimeout, context.CancellationToken);
                    }
                });
            });
        });
        using ConnectHandle observerHandle = bus.ConnectReceiveEndpointObserver(observer);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;
        bool interrupted = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            ISendEndpoint input = await bus.GetSendEndpoint(new Uri($"queue:{queueName}"))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await input.Send(new RecoveryMessage(before), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.True(await beforeObserved.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));

            observer.Watch();
            BrokerOutageControlClient outage = BrokerOutageControlClient.FromEnvironment();
            await outage.InterruptAsync(cancellationToken);
            interrupted = true;
            ReceiveEndpointFaulted fault = await observer.FaultObserved
                .WaitAsync(TimeSpan.FromMinutes(2), cancellationToken);
            Assert.Equal(queueName, Uri.UnescapeDataString(fault.InputAddress.AbsolutePath.Split('/').Last()));

            await outage.RestoreAsync();
            interrupted = false;
            ReceiveEndpointReady recovered = await observer.RecoveryObserved
                .WaitAsync(TimeSpan.FromMinutes(2), cancellationToken);
            Assert.Equal(fault.InputAddress, recovered.InputAddress);

            await input.Send(new RecoveryMessage(after), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await input.Send(new RecoveryMessage(barrier), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.True(await afterObserved.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));
            Assert.True(await barrierEntered.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));
            ActiveMqBroker.ClassicQueueStatistics queue = await fixture.GetClassicQueueStatistics(queueName, cancellationToken);
            Assert.Equal(0, queue.QueueSize);
            Assert.Equal(1, received[before]);
            Assert.Equal(1, received[after]);
            Assert.Equal(1, received[barrier]);
        }
        finally
        {
            releaseBarrier.TrySetResult(true);
            if (interrupted)
                await BrokerOutageControlClient.FromEnvironment().RestoreAsync();
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    private static TaskCompletionSource<T> NewObservation<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed record RecoveryMessage(Guid FlowId);
}
