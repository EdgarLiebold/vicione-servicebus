using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transports;

public sealed class BusHealthLifecycleTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-HEALTH", "configured-endpoint-transitions-unhealthy-to-healthy")]
    public async Task ConfiguredEndpoint_IsUnhealthyBeforeStartAndHealthyAfterStartAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using ServiceProvider provider = BuildSingleBus();
        ITestHarness harness = provider.GetTestHarness();
        HealthCheckService healthChecks = provider.GetRequiredService<HealthCheckService>();

        HealthReport beforeStart = await healthChecks.CheckHealthAsync(cancellationToken);
        Assert.Equal(HealthStatus.Unhealthy, beforeStart.Status);

        await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            BusHealthResult started = await provider.GetRequiredService<IBusControl>()
                .WaitForHealthStatusAsync(BusHealthStatus.Healthy, timeout, cancellationToken);
            Assert.Equal(BusHealthStatus.Healthy, started.Status);
            Assert.Equal(HealthStatus.Healthy, (await healthChecks.CheckHealthAsync(cancellationToken)).Status);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-HEALTH", "stopped-connected-endpoint-degrades-running-bus")]
    public async Task StoppingAConnectedEndpoint_DegradesTheOtherwiseRunningBusAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using ServiceProvider provider = BuildSingleBus();
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
        IBusControl bus = provider.GetRequiredService<IBusControl>();

        try
        {
            HostReceiveEndpointHandle handle = bus.ConnectReceiveEndpoint("health-dependent", _ => { });
            await handle.Ready.WaitAsync(timeout, cancellationToken);
            Assert.Equal(BusHealthStatus.Healthy,
                (await bus.WaitForHealthStatusAsync(BusHealthStatus.Healthy, timeout, cancellationToken)).Status);

            await handle.ReceiveEndpoint.StopAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
            BusHealthResult degraded = await bus.WaitForHealthStatusAsync(
                BusHealthStatus.Degraded,
                timeout,
                cancellationToken);

            Assert.Equal(BusHealthStatus.Degraded, degraded.Status);
            Assert.Equal(
                HealthStatus.Degraded,
                (await provider.GetRequiredService<HealthCheckService>().CheckHealthAsync(cancellationToken)).Status);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-HEALTH", "stopped-bus-restarts-to-healthy")]
    public async Task StoppedBus_RestartsAndReturnsToHealthyAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using ServiceProvider provider = BuildSingleBus();
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
        IBusControl bus = provider.GetRequiredService<IBusControl>();

        try
        {
            await bus.StopAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
            Assert.Equal(
                HealthStatus.Unhealthy,
                (await provider.GetRequiredService<HealthCheckService>().CheckHealthAsync(cancellationToken)).Status);

            await bus.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
            BusHealthResult restarted = await bus.WaitForHealthStatusAsync(
                BusHealthStatus.Healthy,
                timeout,
                cancellationToken);

            Assert.Equal(BusHealthStatus.Healthy, restarted.Status);
            Assert.Equal(
                HealthStatus.Healthy,
                (await provider.GetRequiredService<HealthCheckService>().CheckHealthAsync(cancellationToken)).Status);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-HEALTH", "multiple-bus-instances-compose-one-healthy-report")]
    public async Task MultipleBusInstances_ComposeAHealthyReportOnlyAfterBothAreReadyAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration =>
                configuration.UsingInMemory((_, bus) => bus.ReceiveEndpoint("health-primary", _ => { })))
            .AddViciOneServiceBus<IHealthBus>(configuration =>
                configuration.UsingInMemory((_, bus) =>
                {
                    bus.Host(new Uri("loopback://health-secondary/"));
                    bus.ReceiveEndpoint("health-secondary-endpoint", _ => { });
                }))
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        ITestHarness harness = provider.GetTestHarness();

        Assert.Equal(
            HealthStatus.Unhealthy,
            (await provider.GetRequiredService<HealthCheckService>().CheckHealthAsync(cancellationToken)).Status);

        await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            IBus primary = provider.GetRequiredService<IBus>();
            IHealthBus secondary = provider.GetRequiredService<IHealthBus>();
            HealthReport report = await provider.GetRequiredService<HealthCheckService>()
                .CheckHealthAsync(cancellationToken);

            Assert.Equal(HealthStatus.Healthy, report.Status);
            Assert.NotSame(primary, secondary);
            Assert.Equal("loopback", secondary.Address.Scheme);
            Assert.Equal("health-secondary", secondary.Address.Host);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static ServiceProvider BuildSingleBus() => new ServiceCollection()
        .AddViciOneServiceBusTestHarness(configuration =>
            configuration.UsingInMemory((_, bus) => bus.ReceiveEndpoint("health-input", _ => { })))
        .BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    public interface IHealthBus : IBus;
}
