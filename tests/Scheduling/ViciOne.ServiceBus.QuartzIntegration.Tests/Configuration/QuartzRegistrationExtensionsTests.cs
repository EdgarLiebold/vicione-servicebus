using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.QuartzIntegration.Tests.Configuration;

public sealed class QuartzRegistrationExtensionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-REGISTRATION", "default-services-and-configured-endpoint")]
    public async Task AddQuartzConsumers_RegistersOneClockAndTheConfiguredEndpointOptionsAsync()
    {
        var services = new ServiceCollection();
        services.AddViciOneServiceBus(configuration =>
            configuration.AddQuartzConsumers(options => options.QueueName = "scheduled-messages"));

        await using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Same(TimeProvider.System, provider.GetRequiredService<TimeProvider>());
        Assert.Equal("scheduled-messages", provider.GetRequiredService<IOptions<QuartzEndpointOptions>>().Value.QueueName);
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(TimeProvider));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-REGISTRATION", "application-clock-is-preserved")]
    public async Task AddQuartzConsumers_PreservesAnApplicationOwnedTimeProviderAsync()
    {
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2042, 2, 3, 4, 5, 6, TimeSpan.Zero));
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(timeProvider);
        services.AddViciOneServiceBus(configuration => configuration.AddQuartzConsumers());

        await using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Same(timeProvider, provider.GetRequiredService<TimeProvider>());
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(TimeProvider));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-REGISTRATION", "missing-configurator")]
    public void AddQuartzConsumers_RejectsMissingConfigurator()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            QuartzRegistrationExtensions.AddQuartzConsumers(null!));

        Assert.Equal("configurator", exception.ParamName);
    }
}
