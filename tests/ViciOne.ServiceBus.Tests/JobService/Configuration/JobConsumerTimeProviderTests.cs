using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DependencyInjection.Registration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.JobService.Configuration;

public sealed class JobConsumerTimeProviderTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-CLOCK", "options-clock-owner")]
    public void Options_DefaultAndAssignedPathsHaveOneExplicitClockOwner()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2043, 2, 3, 4, 5, 6, TimeSpan.Zero));
        var options = new JobConsumerOptions();

        Assert.Same(TimeProvider.System, options.TimeProvider);
        options.TimeProvider = clock;

        Assert.Same(clock, options.TimeProvider);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-CLOCK", "direct-options-propagate-all-local-runtime-settings")]
    public void DirectJobServiceOptions_PropagateAllLocalRuntimeSettings()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2044, 3, 4, 5, 6, 7, TimeSpan.Zero));
        var options = new JobServiceOptions
        {
            HeartbeatInterval = TimeSpan.FromSeconds(17),
            RejectedJobDelay = TimeSpan.FromSeconds(23),
            TimeProvider = clock,
        };

        var settings = new InstanceJobServiceSettings(options);

        Assert.Equal(options.HeartbeatInterval, settings.HeartbeatInterval);
        Assert.Equal(options.RejectedJobDelay, settings.RejectedJobDelay);
        Assert.Same(clock, settings.TimeProvider);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-CLOCK", "direct-options-are-required")]
    public void DirectJobServiceOptions_RejectNull()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            new InstanceJobServiceSettings((JobServiceOptions)null!));

        Assert.Equal("options", exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-OPTIONS", "registration-validates-materialized-runtime-options")]
    public void JobServiceRegistration_RejectsInvalidMaterializedRuntimeOptions()
    {
        var registration = new JobServiceRegistration();
        registration.AddConfigureAction(options => options.HeartbeatInterval = TimeSpan.Zero);

        ConfigurationException exception = Assert.Throws<ConfigurationException>(() =>
            _ = registration.EndpointDefinition);

        Assert.Contains(exception.Results, result =>
            result.Disposition == ValidationResultDisposition.Failure
            && result.Key == nameof(JobConsumerOptions)
            && result.Value == nameof(JobConsumerOptions.HeartbeatInterval));
    }
}
