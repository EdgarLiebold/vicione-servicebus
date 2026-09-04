using System.Collections.Concurrent;
using ViciOne.ServiceBus.ActiveMqTransport.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Brokers;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.Brokers;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMqTransport.LocalIntegration.Tests;

[Collection(ActiveMqBrokerOutageCollection.Name)]
public sealed class ActiveMqFixtureRecoveryTests
{
    [Theory]
    [InlineData(ActiveMqBroker.OpenWireFlavor)]
    [InlineData(ActiveMqBroker.AmqpFlavor)]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-BROKER-RECOVERY", "run-scoped-outage-recovers-the-configured-endpoint")]
    public async Task RunScopedOutageControl_RestoresStableEndpointsAsync(string flavor)
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
        var firstReceiveCompleted = new ReceiveCompletionObserver(1);
        var allReceivesCompleted = new ReceiveCompletionObserver(3);
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
        using ConnectHandle firstReceiveObserverHandle = bus.ConnectReceiveObserver(firstReceiveCompleted);
        using ConnectHandle receiveObserverHandle = bus.ConnectReceiveObserver(allReceivesCompleted);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;
        bool interrupted = false;
        Exception? primaryFailure = null;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            ISendEndpoint input = await bus.GetSendEndpointAsync(new Uri($"queue:{queueName}"), TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            await input.SendAsync(new RecoveryMessage(before), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.True(await beforeObserved.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));
            await firstReceiveCompleted.Completed.WaitAsync(fixture.OperationTimeout, cancellationToken);

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

            await input.SendAsync(new RecoveryMessage(after), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await input.SendAsync(new RecoveryMessage(barrier), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.True(await afterObserved.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));
            Assert.True(await barrierEntered.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));
            releaseBarrier.TrySetResult(true);
            await allReceivesCompleted.Completed.WaitAsync(fixture.OperationTimeout, cancellationToken);
            await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            started = false;
            // The outage control replaces the broker process, so its JMX lifetime counters restart
            // at zero. Application-level exact-once counts below span both broker lifetimes; these
            // management counts deliberately describe only the recovered broker.
            ActiveMqBroker.ClassicQueueStatistics expectedQueue = new(2, 2, 0);
            ActiveMqBroker.ClassicQueueStatistics queue = await fixture.GetClassicQueueStatisticsAsync(queueName, cancellationToken);
            Assert.Equal(expectedQueue, queue);
            Assert.Equal(1, received[before]);
            Assert.Equal(1, received[after]);
            Assert.Equal(1, received[barrier]);
        }
        catch (Exception exception)
        {
            primaryFailure = exception;
            throw;
        }
        finally
        {
            releaseBarrier.TrySetResult(true);
            if (interrupted)
                await BrokerOutageControlClient.FromEnvironment().RestoreAsync();
            if (started)
            {
                try
                {
                    await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
                }
                catch (Exception cleanupFailure) when (primaryFailure is not null)
                {
                    primaryFailure.Data["CleanupFailure"] = cleanupFailure.ToString();
                }
            }
        }
    }

    private static TaskCompletionSource<T> NewObservation<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed record RecoveryMessage(Guid FlowId);

}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ActiveMqBrokerOutageCollection
{
    public const string Name = "ActiveMQ broker outage";
}
