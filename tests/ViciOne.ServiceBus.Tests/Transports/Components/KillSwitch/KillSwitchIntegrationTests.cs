using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Testing;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transports.Components.KillSwitch;

public sealed class KillSwitchIntegrationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-KILL-SWITCH-INMEMORY", "partitioned-pipeline-remains-healthy-after-kill-switch-restart")]
    public async Task PartitionedInMemoryEndpoint_RemainsHealthyAndContinuesDeliveryAfterKillSwitchRestartAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var time = new ObservableTimeProvider(new DateTimeOffset(2036, 1, 2, 3, 4, 5, TimeSpan.Zero));
        var lifecycle = new EndpointLifecycleObserver("FailingAndHealthy");
        var services = new ServiceCollection();
        services.AddSingleton<IReceiveEndpointObserver>(lifecycle);
        services.AddSingleton<HealthyDeliveryProbe>();
        services.AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.AddConsumer<FailingAndHealthyConsumer>();
                configuration.UsingInMemory((context, configurator) =>
                {
                    configurator.UseKillSwitch(options => options
                        .SetActivationThreshold(4)
                        .SetTripThresholdRatio(1)
                        .SetRestartDelay(TimeSpan.FromSeconds(1))
                        .SetTimeProvider(time));
                    configurator.UseMessagePartitioner(4);
                    configurator.ConfigureEndpoints(context);
                });
            });
        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
        IBusControl bus = provider.GetRequiredService<IBusControl>();
        HealthCheckService healthChecks = provider.GetRequiredService<HealthCheckService>();
        HealthyDeliveryProbe delivery = provider.GetRequiredService<HealthyDeliveryProbe>();

        try
        {
            BusHealthResult initial = await bus.WaitForHealthStatusAsync(
                BusHealthStatus.Healthy,
                timeout,
                cancellationToken);
            Assert.Equal(BusHealthStatus.Healthy, initial.Status);
            Assert.Equal(HealthStatus.Healthy, (await healthChecks.CheckHealthAsync(cancellationToken)).Status);

            await bus.PublishBatchAsync(
                Enumerable.Range(0, 4).Select(index => new FailingMessage(NewId.NextGuid(), index)),
                cancellationToken);
            BusHealthResult degraded = await bus.WaitForHealthStatusAsync(
                BusHealthStatus.Degraded,
                timeout,
                cancellationToken);
            Assert.Equal(BusHealthStatus.Degraded, degraded.Status);
            Assert.Equal(HealthStatus.Degraded, (await healthChecks.CheckHealthAsync(cancellationToken)).Status);

            await time.WaitForTimerCountAsync(1).WaitAsync(timeout, cancellationToken);
            time.Advance(TimeSpan.FromSeconds(1));
            await lifecycle.SecondReady.WaitAsync(timeout, cancellationToken);
            BusHealthResult recovered = await bus.WaitForHealthStatusAsync(
                BusHealthStatus.Healthy,
                timeout,
                cancellationToken);
            Assert.Equal(BusHealthStatus.Healthy, recovered.Status);
            Assert.Equal(HealthStatus.Healthy, (await healthChecks.CheckHealthAsync(cancellationToken)).Status);

            await bus.PublishAsync(new HealthyMessage(NewId.NextGuid(), "after recovery"), cancellationToken);
            HealthyMessage consumed = await delivery.Delivered.WaitAsync(timeout, cancellationToken);
            Assert.Equal("after recovery", consumed.Value);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-KILL-SWITCH-INMEMORY", "terminal-bus-stop-prevents-zombie-restart")]
    public async Task BusStopWhileEndpointIsPaused_CancelsRecoveryAndPreventsAZombieRestartAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var time = new ObservableTimeProvider(new DateTimeOffset(2036, 1, 2, 3, 4, 5, TimeSpan.Zero));
        var lifecycle = new EndpointLifecycleObserver("FailingAndHealthy");
        var services = new ServiceCollection();
        services.AddSingleton<IReceiveEndpointObserver>(lifecycle);
        services.AddSingleton<HealthyDeliveryProbe>();
        services.AddViciOneServiceBusTestHarness(configuration =>
        {
            configuration.AddConsumer<FailingAndHealthyConsumer>();
            configuration.UsingInMemory((context, configurator) =>
            {
                configurator.UseKillSwitch(options => options
                    .SetActivationThreshold(1)
                    .SetTripThresholdRatio(1)
                    .SetRestartDelay(TimeSpan.FromSeconds(1))
                    .SetTimeProvider(time));
                configurator.ConfigureEndpoints(context);
            });
        });
        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
        IBusControl bus = provider.GetRequiredService<IBusControl>();
        var stopped = false;

        try
        {
            await bus.PublishAsync(new FailingMessage(NewId.NextGuid(), 0), cancellationToken);
            await bus.WaitForHealthStatusAsync(BusHealthStatus.Degraded, timeout, cancellationToken);
            await time.WaitForTimerCountAsync(1).WaitAsync(timeout, cancellationToken);
            Assert.Equal(1, lifecycle.TargetReadyCount);

            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
            stopped = true;
            time.Advance(TimeSpan.FromDays(1));
            await Task.Yield();

            Assert.Equal(1, lifecycle.TargetReadyCount);
            Assert.Equal(0, time.ActiveTimerCount);
        }
        finally
        {
            if (!stopped)
                await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static TimeSpan OperationTimeout() =>
        TestConfigurationProvider.ForCurrentTestRun().GetValidatedOptions().OperationTimeout!.Value;

    private sealed record FailingMessage(Guid CorrelationId, int Index) : ICorrelatedBy<Guid>;
    private sealed record HealthyMessage(Guid CorrelationId, string Value) : ICorrelatedBy<Guid>;

    private sealed class FailingAndHealthyConsumer(HealthyDeliveryProbe delivery) :
        IConsumer<FailingMessage>,
        IConsumer<HealthyMessage>
    {
        public Task ConsumeAsync(ConsumeContext<FailingMessage> context) =>
            throw new InvalidOperationException($"intentional failure {context.Message.Index}");

        public Task ConsumeAsync(ConsumeContext<HealthyMessage> context)
        {
            delivery.MarkDelivered(context.Message);
            return Task.CompletedTask;
        }
    }

    private sealed class HealthyDeliveryProbe
    {
        private readonly TaskCompletionSource<HealthyMessage> _delivered =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<HealthyMessage> Delivered => _delivered.Task;

        public void MarkDelivered(HealthyMessage message) => _delivered.TrySetResult(message);
    }

    private sealed class EndpointLifecycleObserver(string targetEndpointName) : IReceiveEndpointObserver
    {
        private int _targetReadyCount;
        private readonly TaskCompletionSource<bool> _secondReady =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int TargetReadyCount => Volatile.Read(ref _targetReadyCount);
        public Task SecondReady => _secondReady.Task;

        public Task ReadyAsync(ReceiveEndpointReady ready)
        {
            if (ready.InputAddress.AbsolutePath.Trim('/').Equals(targetEndpointName, StringComparison.OrdinalIgnoreCase))
            {
                int readyCount = Interlocked.Increment(ref _targetReadyCount);
                if (readyCount >= 2)
                    _secondReady.TrySetResult(true);
            }

            return Task.CompletedTask;
        }

        public Task StoppingAsync(ReceiveEndpointStopping stopping) => Task.CompletedTask;

        public Task CompletedAsync(ReceiveEndpointCompleted completed) => Task.CompletedTask;

        public Task FaultedAsync(ReceiveEndpointFaulted faulted) => Task.CompletedTask;
    }
}
