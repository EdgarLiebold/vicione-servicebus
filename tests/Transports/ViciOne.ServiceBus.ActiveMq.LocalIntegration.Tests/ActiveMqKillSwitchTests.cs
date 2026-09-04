using System.Collections.Concurrent;
using ViciOne.ServiceBus.ActiveMq.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMq.LocalIntegration.Tests;

public sealed class ActiveMqKillSwitchTests
{
    private const int FailureCount = 11;
    private const int RecoveryCount = 20;

    [Theory]
    [InlineData(ActiveMqBroker.OpenWireFlavor)]
    [InlineData(ActiveMqBroker.AmqpFlavor)]
    [RequirementCoverage("OBL-R0-BRK-0433", "repeated-consumer-failure-degrades-recovers-and-continues-delivery")]
    public async Task RepeatedConsumerFailure_TransitionsToDegradedAndRecoversAsync(string flavor)
    {
        using ActiveMqBroker fixture = ActiveMqBroker.Create(flavor, "kill-switch");
        string queueName = fixture.Name("input");
        Guid[] failures = [.. Enumerable.Range(0, FailureCount).Select(_ => Guid.NewGuid())];
        Guid[] recoveries = [.. Enumerable.Range(0, RecoveryCount).Select(_ => Guid.NewGuid())];
        var failed = new ConcurrentDictionary<Guid, byte>();
        var recovered = new ConcurrentDictionary<Guid, byte>();
        var allFailuresObserved = NewObservation<bool>();
        var allRecoveriesObserved = NewObservation<bool>();
        IBusControl bus = Bus.Factory.CreateUsingActiveMq(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.UseKillSwitch(options => options
                .SetActivationThreshold(10)
                .SetTripThresholdRatio(0.10)
                .SetRestartDelay(TimeSpan.FromSeconds(1)));
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.PrefetchCount = 1;
                endpoint.ConcurrentMessageLimit = 1;
                endpoint.Handler<FailingMessage>(context =>
                {
                    failed.TryAdd(context.Message.FlowId, 0);
                    if (failed.Count == FailureCount)
                        allFailuresObserved.TrySetResult(true);
                    throw new IntentionalFailure(context.Message.FlowId);
                });
                endpoint.Handler<HealthyMessage>(context =>
                {
                    recovered.TryAdd(context.Message.FlowId, 0);
                    if (recovered.Count == RecoveryCount)
                        allRecoveriesObserved.TrySetResult(true);
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
            Assert.Equal(
                BusHealthStatus.Healthy,
                (await bus.WaitForHealthStatusAsync(BusHealthStatus.Healthy, fixture.OperationTimeout, cancellationToken)).Status);

            ISendEndpoint input = await bus.GetSendEndpointAsync(new Uri($"queue:{queueName}"), TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            await Task.WhenAll(failures.Select(flowId => input.SendAsync(new FailingMessage(flowId), cancellationToken)))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(
                BusHealthStatus.Degraded,
                (await bus.WaitForHealthStatusAsync(BusHealthStatus.Degraded, fixture.OperationTimeout, cancellationToken)).Status);
            Assert.Equal(
                BusHealthStatus.Healthy,
                (await bus.WaitForHealthStatusAsync(BusHealthStatus.Healthy, fixture.OperationTimeout, cancellationToken)).Status);
            await allFailuresObserved.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);

            await Task.WhenAll(recoveries.Select(flowId => input.SendAsync(new HealthyMessage(flowId), cancellationToken)))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await allRecoveriesObserved.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(failures.Order(), failed.Keys.Order());
            Assert.Equal(FailureCount, failed.Count);
            Assert.Equal(recoveries.Order(), recovered.Keys.Order());
            Assert.Equal(RecoveryCount, recovered.Count);
            Assert.Equal(
                BusHealthStatus.Healthy,
                (await bus.WaitForHealthStatusAsync(BusHealthStatus.Healthy, fixture.OperationTimeout, cancellationToken)).Status);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    private static TaskCompletionSource<T> NewObservation<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public sealed record FailingMessage(Guid FlowId);
    public sealed record HealthyMessage(Guid FlowId);

    private sealed class IntentionalFailure(Guid flowId) : Exception($"Intentional failure for {flowId:D}.");
}
