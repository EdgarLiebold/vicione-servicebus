using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Quartz;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Quartz.Runtime;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Quartz.Tests.Configuration;

public sealed class QuartzSchedulingExtensionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-REGISTRATION", "bus-specific-settings-snapshot")]
    public async Task AddQuartzScheduling_RegistersOneClockAndABusSpecificSettingsSnapshotAsync()
    {
        ISchedulerFactory schedulerFactory = QuartzSchedulingExtensions.CreateInMemorySchedulerFactory();
        await using var factoryLifetime = Assert.IsAssignableFrom<IAsyncDisposable>(schedulerFactory);
        var services = new ServiceCollection();
        services.AddViciOneServiceBus(configuration =>
            configuration.AddQuartzScheduling(
                _ => schedulerFactory,
                options => options.QueueName = "scheduled-messages"));

        await using ServiceProvider provider = services.BuildServiceProvider();
        QuartzSchedulerBinding<IBus> binding = provider.GetRequiredService<QuartzSchedulerBinding<IBus>>();

        Assert.Same(TimeProvider.System, provider.GetRequiredService<TimeProvider>());
        Assert.Equal("scheduled-messages", binding.Settings.QueueName);
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(TimeProvider));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-REGISTRATION", "application-clock-is-preserved")]
    public async Task AddQuartzScheduling_PreservesAnApplicationOwnedTimeProviderAsync()
    {
        ISchedulerFactory schedulerFactory = QuartzSchedulingExtensions.CreateInMemorySchedulerFactory();
        await using var factoryLifetime = Assert.IsAssignableFrom<IAsyncDisposable>(schedulerFactory);
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2042, 2, 3, 4, 5, 6, TimeSpan.Zero));
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(timeProvider);
        services.AddViciOneServiceBus(configuration =>
            configuration.AddQuartzScheduling(_ => schedulerFactory));

        await using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Same(timeProvider, provider.GetRequiredService<TimeProvider>());
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(TimeProvider));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-MULTIBUS", "typed-registration-creates-only-its-bus-binding")]
    public async Task TypedRegistration_CreatesOnlyTheRequestedBusBindingAsync()
    {
        ISchedulerFactory schedulerFactory = DispatchProxy.Create<ISchedulerFactory, NoOpDispatchProxy>();
        var services = new ServiceCollection();
        services.AddViciOneServiceBus<ISecondaryBus>(configuration =>
            configuration.AddQuartzScheduling(
                _ => schedulerFactory,
                options => options.QueueName = "secondary-quartz"));
        await using ServiceProvider provider = services.BuildServiceProvider();

        QuartzSchedulerBinding<ISecondaryBus> binding =
            provider.GetRequiredService<QuartzSchedulerBinding<ISecondaryBus>>();

        Assert.Equal(QuartzSchedulerNamespace.GetStableBusIdentity(typeof(ISecondaryBus)), binding.BusKey);
        Assert.Equal("secondary-quartz", binding.Settings.QueueName);
        Assert.Null(provider.GetService<QuartzSchedulerBinding<IBus>>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-REGISTRATION", "factory-resolver-must-return-a-factory")]
    public void AddQuartzScheduling_RejectsAResolverThatReturnsNull()
    {
        var services = new ServiceCollection();
        services.AddViciOneServiceBus(configuration =>
            configuration.AddQuartzScheduling(static _ => null!));
        using ServiceProvider provider = services.BuildServiceProvider();

        ArgumentNullException failure = Assert.Throws<ArgumentNullException>(() =>
            provider.GetRequiredService<QuartzSchedulerBinding<IBus>>());

        Assert.Equal("schedulerFactory", failure.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-REGISTRATION", "missing-arguments")]
    public void AddQuartzScheduling_RejectsMissingArguments()
    {
        ArgumentNullException missingConfigurator = Assert.Throws<ArgumentNullException>(() =>
            QuartzSchedulingExtensions.AddQuartzScheduling(null!, static _ => null!));
        var services = new ServiceCollection();
        IBusRegistrationConfigurator? configurator = null;
        services.AddViciOneServiceBus(value => configurator = value);

        ArgumentNullException missingFactory = Assert.Throws<ArgumentNullException>(() =>
            configurator!.AddQuartzScheduling(null!));

        Assert.Equal("configurator", missingConfigurator.ParamName);
        Assert.Equal("schedulerFactory", missingFactory.ParamName);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-OWNERSHIP", "hosted-service-conflict-is-order-independent")]
    public void QuartzHostedService_CannotShareTheBusOwnedSchedulerLifecycle(bool registerHostedServiceFirst)
    {
        var services = new ServiceCollection();
        services.AddQuartz();
        if (registerHostedServiceFirst)
            services.AddQuartzHostedService();
        services.AddViciOneServiceBus(configuration =>
            configuration.AddQuartzScheduling(provider => provider.GetRequiredService<ISchedulerFactory>()));
        if (!registerHostedServiceFirst)
            services.AddQuartzHostedService();
        using ServiceProvider provider = services.BuildServiceProvider();

        ConfigurationException failure = Assert.Throws<ConfigurationException>(() =>
            provider.GetRequiredService<QuartzSchedulerBinding<IBus>>());

        Assert.Contains(nameof(QuartzHostedService), failure.Message, StringComparison.Ordinal);
        Assert.Contains("lifecycle owner", failure.Message, StringComparison.Ordinal);
    }

    private class NoOpDispatchProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException(targetMethod?.Name);
    }

    public interface ISecondaryBus : IBus;
}
