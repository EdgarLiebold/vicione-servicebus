using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;
using IHost = Microsoft.Extensions.Hosting.IHost;

namespace ViciOne.ServiceBus.Tests.DependencyInjection;

public sealed class MultiBusHostOwnershipIntegrationTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-BUS-HEALTH", "host-registration-health-and-delivery-retain-bus-owner")]
    public async Task HostLifecycle_ReportsEachBusAndKeepsTheOtherBusOperationalAsync(
        bool explicitInstance, bool customHealth)
    {
        var observations = new ConcurrentQueue<(string Bus, string Marker)>();
        var primaryDelivery = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondaryDelivery = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        object marker = new();
        var contexts = new List<HostBuilderContext>();
        IHostBuilder builder = CreateBuilder();
        builder.Properties["owner-marker"] = marker;

        Assert.Same(builder, builder.UseViciOneServiceBus((context, bus) =>
        {
            contexts.Add(context);
            bus.Limits(MessageLimits.Conservative);
            if (customHealth)
                bus.ConfigureHealthCheckOptions(options => ConfigureHealth(options, "primary", HealthStatus.Unhealthy));
            bus.UsingInMemory((_, transport) =>
            {
                transport.Host(new Uri("loopback://t48-primary/"));
                transport.ReceiveEndpoint("input", endpoint => endpoint.Handler<OwnerMessage>(consume =>
                {
                    observations.Enqueue(("primary", consume.Message.Marker));
                    primaryDelivery.TrySetResult();
                    return Task.CompletedTask;
                }));
            });
        }));

        void ConfigureSecondary(HostBuilderContext context, IBusRegistrationConfigurator<ISecondaryBus> bus)
        {
            contexts.Add(context);
            bus.Limits(MessageLimits.Conservative);
            if (customHealth)
                bus.ConfigureHealthCheckOptions(options => ConfigureHealth(options, "secondary", HealthStatus.Degraded));
            bus.UsingInMemory((_, transport) =>
            {
                transport.Host(new Uri("loopback://t48-secondary/"));
                transport.ReceiveEndpoint("input", endpoint => endpoint.Handler<OwnerMessage>(consume =>
                {
                    observations.Enqueue(("secondary", consume.Message.Marker));
                    secondaryDelivery.TrySetResult();
                    return Task.CompletedTask;
                }));
            });
        }

        Assert.Same(builder, explicitInstance
            ? builder.UseViciOneServiceBus<ISecondaryBus, SecondaryBus>(ConfigureSecondary)
            : builder.UseViciOneServiceBus<ISecondaryBus>(ConfigureSecondary));
        using IHost host = builder.Build();
        Assert.Equal(2, contexts.Count);
        Assert.All(contexts, context => Assert.Same(marker, context.Properties["owner-marker"]));
        Assert.Same(contexts[0], contexts[1]);
        IBusInstance[] instances = host.Services.GetServices<IBusInstance>().ToArray();
        Assert.Equal(2, instances.Length);
        IBusControl primary = Assert.Single(instances, instance => instance.InstanceType == typeof(IBus)).BusControl;
        IBusControl secondary = Assert.Single(instances, instance => instance.InstanceType == typeof(ISecondaryBus)).BusControl;
        Assert.NotSame(primary, secondary);
        if (explicitInstance)
            Assert.IsType<SecondaryBus>(host.Services.GetRequiredService<ISecondaryBus>());

        string primaryName = customHealth ? "primary" : "vicione-servicebus-bus";
        string secondaryName = customHealth ? "secondary" : "vicione-servicebus-secondary-bus";
        HealthStatus secondaryStopped = customHealth ? HealthStatus.Degraded : HealthStatus.Unhealthy;
        HealthCheckService health = host.Services.GetRequiredService<HealthCheckService>();
        HealthCheckRegistration[] registrations = host.Services
            .GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations.ToArray();
        Assert.Equal(2, registrations.Length);
        foreach (HealthCheckRegistration registration in registrations)
        {
            Assert.Equal((customHealth ? new[] { registration.Name, "ready" } : ["ready", "vicione-servicebus"]).Order(StringComparer.OrdinalIgnoreCase),
                registration.Tags.Order(StringComparer.OrdinalIgnoreCase));
        }
        await AssertHealthAsync(HealthStatus.Unhealthy, secondaryStopped);
        try
        {
            await host.StartAsync(Token).WaitAsync(Timeout, Token);
            await AssertHealthAsync(HealthStatus.Healthy, HealthStatus.Healthy);

            await primary.StopAsync(Token).WaitAsync(Timeout, Token);
            await AssertHealthAsync(HealthStatus.Unhealthy, HealthStatus.Healthy);
            ISendEndpoint secondaryInput = await secondary.GetSendEndpointAsync(new Uri("loopback://t48-secondary/input"), cancellationToken: Token);
            await secondaryInput.SendAsync(new OwnerMessage("survives-primary-stop"), Token);
            await secondaryDelivery.Task.WaitAsync(Timeout, Token);

            await primary.StartAsync(Token).WaitAsync(Timeout, Token);
            await secondary.StopAsync(Token).WaitAsync(Timeout, Token);
            await AssertHealthAsync(HealthStatus.Healthy, secondaryStopped);
            ISendEndpoint primaryInput = await primary.GetSendEndpointAsync(new Uri("loopback://t48-primary/input"), cancellationToken: Token);
            await primaryInput.SendAsync(new OwnerMessage("survives-secondary-stop"), Token);
            await primaryDelivery.Task.WaitAsync(Timeout, Token);
        }
        finally
        {
            await host.StopAsync(CancellationToken.None).WaitAsync(Timeout, CancellationToken.None);
        }

        await AssertHealthAsync(HealthStatus.Unhealthy, secondaryStopped);
        Assert.Equal(new[] { ("secondary", "survives-primary-stop"), ("primary", "survives-secondary-stop") }, observations);

        async Task AssertHealthAsync(HealthStatus expectedPrimary, HealthStatus expectedSecondary)
        {
            HealthReport report = await health.CheckHealthAsync(Token).WaitAsync(Timeout, Token);
            Assert.Equal(new[] { primaryName, secondaryName }.Order(StringComparer.Ordinal),
                report.Entries.Keys.Order(StringComparer.Ordinal));
            Assert.Equal(expectedPrimary, report.Entries[primaryName].Status);
            Assert.Equal(expectedSecondary, report.Entries[secondaryName].Status);
            Assert.Equal((HealthStatus)Math.Min((int)expectedPrimary, (int)expectedSecondary), report.Status);
            Assert.Null(report.Entries[primaryName].Exception);
            Assert.Null(report.Entries[secondaryName].Exception);
        }
    }

    [Theory]
    [InlineData(false, InvalidOption.EmptyName)]
    [InlineData(false, InvalidOption.WhitespaceName)]
    [InlineData(false, InvalidOption.Status)]
    [InlineData(false, InvalidOption.EmptyTag)]
    [InlineData(false, InvalidOption.NullTag)]
    [InlineData(true, InvalidOption.EmptyName)]
    [InlineData(true, InvalidOption.WhitespaceName)]
    [InlineData(true, InvalidOption.Status)]
    [InlineData(true, InvalidOption.EmptyTag)]
    [InlineData(true, InvalidOption.NullTag)]
    [RequirementCoverage("REQ-VSB-BUS-HEALTH", "invalid-options-stop-host-with-exact-bus-owner")]
    public async Task InvalidHealthOptions_RejectStartupWithoutContaminatingTheCompanionBusAsync(bool typed, InvalidOption invalid)
    {
        IHostBuilder builder = CreateBuilder();
        builder.UseViciOneServiceBus((_, bus) =>
        {
            bus.Limits(MessageLimits.Conservative);
            bus.ConfigureHealthCheckOptions(options => ConfigureOptions(options, !typed));
            bus.UsingInMemory();
        });
        builder.UseViciOneServiceBus<ISecondaryBus>((_, bus) =>
        {
            bus.Limits(MessageLimits.Conservative);
            bus.ConfigureHealthCheckOptions(options => ConfigureOptions(options, typed));
            bus.UsingInMemory();
        });
        using IHost host = builder.Build();

        IHealthCheckOptions companion = typed
            ? host.Services.GetRequiredService<IOptions<ViciOneServiceBusHealthCheckOptions<IBus>>>().Value
            : host.Services.GetRequiredService<IOptions<ViciOneServiceBusHealthCheckOptions<ISecondaryBus>>>().Value;
        Assert.Equal("valid-companion", companion.Name);
        Assert.Equal(HealthStatus.Unhealthy, companion.MinimalFailureStatus);
        Assert.Equal("companion", Assert.Single(companion.Tags));

        Exception? startupFailure = await Record.ExceptionAsync(() => host.StartAsync(Token).WaitAsync(Timeout, Token));
        Assert.NotNull(startupFailure);
        OptionsValidationException failure = Assert.IsType<OptionsValidationException>(
            startupFailure is TargetInvocationException invocation ? invocation.InnerException : startupFailure);
        Assert.Equal(typed ? typeof(ViciOneServiceBusHealthCheckOptions<ISecondaryBus>)
            : typeof(ViciOneServiceBusHealthCheckOptions<IBus>), failure.OptionsType);
        string diagnostic = Assert.Single(failure.Failures);
        Assert.Contains($"bus '{(typed ? typeof(ISecondaryBus).FullName : "default")}'", diagnostic, StringComparison.Ordinal);
        Assert.Contains(invalid switch
        {
            InvalidOption.Status => "MinimalFailureStatus is not defined",
            InvalidOption.EmptyTag or InvalidOption.NullTag => "Tags contains an empty value",
            _ => "Name must not be empty",
        }, diagnostic, StringComparison.Ordinal);

        void ConfigureOptions(IHealthCheckOptionsConfigurator options, bool invalidOwner)
        {
            ConfigureHealth(options, "valid-companion", HealthStatus.Unhealthy);
            options.Tags.Clear();
            options.Tags.Add("companion");
            if (!invalidOwner)
                return;
            switch (invalid)
            {
                case InvalidOption.EmptyName: options.Name = ""; break;
                case InvalidOption.WhitespaceName: options.Name = " \t"; break;
                case InvalidOption.Status: options.MinimalFailureStatus = (HealthStatus)99; break;
                case InvalidOption.EmptyTag: options.Tags.Add(" "); break;
                case InvalidOption.NullTag: options.Tags.Add(null!); break;
                default: throw new ArgumentOutOfRangeException(nameof(invalid));
            }
        }
    }

    private static IHostBuilder CreateBuilder() => new HostBuilder()
        .UseDefaultServiceProvider(options =>
        {
            options.ValidateOnBuild = true;
            options.ValidateScopes = true;
        })
        .ConfigureServices(services =>
        {
            services.AddViciOneServiceBusTextWriterLogger(TextWriter.Null);
            services.Configure<ViciOneServiceBusHostOptions>(options => options.WaitUntilStarted = true);
        });

    private static void ConfigureHealth(IHealthCheckOptionsConfigurator options, string name, HealthStatus floor)
    {
        options.Name = name;
        options.MinimalFailureStatus = floor;
        options.Tags.Add(name);
        options.Tags.Add("ready");
        options.Tags.Add("READY");
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;
    private static TimeSpan Timeout => TestConfigurationProvider.ForCurrentTestRun().GetValidatedOptions().OperationTimeout!.Value;

    public interface ISecondaryBus : IBus;
    public sealed class SecondaryBus(IBusControl control) : BusInstance<ISecondaryBus>(control), ISecondaryBus;
    public sealed record OwnerMessage(string Marker);
    public enum InvalidOption { EmptyName, WhitespaceName, Status, EmptyTag, NullTag }
}
