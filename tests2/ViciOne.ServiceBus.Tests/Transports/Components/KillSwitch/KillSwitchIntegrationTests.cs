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
    [RequirementCoverage("REQ-VSB-KILL-SWITCH-INMEMORY", "healthy-degraded-healthy-with-continued-delivery")]
    public async Task InMemoryEndpoint_TransitionsHealthyDegradedHealthyAndContinuesDelivery()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var time = new ObservableTimeProvider(new DateTimeOffset(2036, 1, 2, 3, 4, 5, TimeSpan.Zero));
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.AddConsumer<FailingAndHealthyConsumer>();
                configuration.UsingInMemory((context, configurator) =>
                {
                    configurator.UseKillSwitch(options => options
                        .SetActivationThreshold(4)
                        .SetTripThresholdRatio(1)
                        .SetRestartDelay(TimeSpan.FromSeconds(1))
                        .SetTimeProvider(time));
                    configurator.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(timeout, cancellationToken);
        IBusControl bus = provider.GetRequiredService<IBusControl>();
        HealthCheckService healthChecks = provider.GetRequiredService<HealthCheckService>();

        try
        {
            BusHealthResult initial = await bus.WaitForHealthStatusAsync(
                BusHealthStatus.Healthy,
                timeout,
                cancellationToken);
            Assert.Equal(BusHealthStatus.Healthy, initial.Status);
            Assert.Equal(HealthStatus.Healthy, (await healthChecks.CheckHealthAsync(cancellationToken)).Status);

            await bus.PublishBatch(
                Enumerable.Range(0, 4).Select(index => new FailingMessage(index)),
                cancellationToken);
            BusHealthResult degraded = await bus.WaitForHealthStatusAsync(
                BusHealthStatus.Degraded,
                timeout,
                cancellationToken);
            Assert.Equal(BusHealthStatus.Degraded, degraded.Status);
            Assert.Equal(HealthStatus.Degraded, (await healthChecks.CheckHealthAsync(cancellationToken)).Status);

            await time.WaitForTimerCount(1).WaitAsync(timeout, cancellationToken);
            time.Advance(TimeSpan.FromSeconds(1));
            BusHealthResult recovered = await bus.WaitForHealthStatusAsync(
                BusHealthStatus.Healthy,
                timeout,
                cancellationToken);
            Assert.Equal(BusHealthStatus.Healthy, recovered.Status);
            Assert.Equal(HealthStatus.Healthy, (await healthChecks.CheckHealthAsync(cancellationToken)).Status);

            await bus.Publish(new HealthyMessage("after recovery"), cancellationToken);
            Assert.True(await harness.Consumed.Any<HealthyMessage>(cancellationToken));
        }
        finally
        {
            await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-KILL-SWITCH-INMEMORY", "terminal-bus-stop-prevents-zombie-restart")]
    public async Task BusStopWhileEndpointIsPaused_CancelsRecoveryAndPreventsAZombieRestart()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var time = new ObservableTimeProvider(new DateTimeOffset(2036, 1, 2, 3, 4, 5, TimeSpan.Zero));
        var lifecycle = new EndpointLifecycleObserver("FailingAndHealthy");
        var services = new ServiceCollection();
        services.AddSingleton<IReceiveEndpointObserver>(lifecycle);
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
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(timeout, cancellationToken);
        IBusControl bus = provider.GetRequiredService<IBusControl>();
        var stopped = false;

        try
        {
            await bus.Publish(new FailingMessage(0), cancellationToken);
            await bus.WaitForHealthStatusAsync(BusHealthStatus.Degraded, timeout, cancellationToken);
            await time.WaitForTimerCount(1).WaitAsync(timeout, cancellationToken);
            Assert.Equal(1, lifecycle.TargetReadyCount);

            await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
            stopped = true;
            time.Advance(TimeSpan.FromDays(1));
            await Task.Yield();

            Assert.Equal(1, lifecycle.TargetReadyCount);
            Assert.Equal(0, time.ActiveTimerCount);
        }
        finally
        {
            if (!stopped)
                await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static TimeSpan OperationTimeout() =>
        TestConfigurationProvider.ForCurrentTestRun().GetValidatedOptions().OperationTimeout!.Value;

    private sealed record FailingMessage(int Index);
    private sealed record HealthyMessage(string Value);

    private sealed class FailingAndHealthyConsumer :
        IConsumer<FailingMessage>,
        IConsumer<HealthyMessage>
    {
        public Task Consume(ConsumeContext<FailingMessage> context) =>
            throw new InvalidOperationException($"intentional failure {context.Message.Index}");

        public Task Consume(ConsumeContext<HealthyMessage> context) => Task.CompletedTask;
    }

    private sealed class EndpointLifecycleObserver(string targetEndpointName) : IReceiveEndpointObserver
    {
        private int _targetReadyCount;

        public int TargetReadyCount => Volatile.Read(ref _targetReadyCount);

        public Task Ready(ReceiveEndpointReady ready)
        {
            if (ready.InputAddress.AbsolutePath.Trim('/').Equals(targetEndpointName, StringComparison.OrdinalIgnoreCase))
                Interlocked.Increment(ref _targetReadyCount);

            return Task.CompletedTask;
        }

        public Task Stopping(ReceiveEndpointStopping stopping) => Task.CompletedTask;

        public Task Completed(ReceiveEndpointCompleted completed) => Task.CompletedTask;

        public Task Faulted(ReceiveEndpointFaulted faulted) => Task.CompletedTask;
    }
}
