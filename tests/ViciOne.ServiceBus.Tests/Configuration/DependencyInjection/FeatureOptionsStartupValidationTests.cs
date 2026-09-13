using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration.DependencyInjection;

public sealed class FeatureOptionsStartupValidationTests
{
    [Theory]
    [InlineData(InvalidFeatureOption.JobHeartbeat, "HeartbeatInterval")]
    [InlineData(InvalidFeatureOption.JobRejectedDelay, "RejectedJobDelay")]
    [InlineData(InvalidFeatureOption.JobTimeProvider, "TimeProvider")]
    [InlineData(InvalidFeatureOption.JobSagaSlotWait, "SlotWaitTime")]
    [InlineData(InvalidFeatureOption.JobSagaStatusCheck, "StatusCheckInterval")]
    [InlineData(InvalidFeatureOption.JobSagaHeartbeat, "HeartbeatTimeout")]
    [InlineData(InvalidFeatureOption.JobSagaConcurrency, "ConcurrentMessageLimit")]
    [InlineData(InvalidFeatureOption.JobSagaRetryCount, "SuspectJobRetryCount")]
    [InlineData(InvalidFeatureOption.JobSagaRetryDelay, "SuspectJobRetryDelay")]
    [InlineData(InvalidFeatureOption.HealthName, "Name")]
    [InlineData(InvalidFeatureOption.HealthStatus, "MinimalFailureStatus")]
    [InlineData(InvalidFeatureOption.HealthTag, "Tags")]
    [RequirementCoverage("REQ-VSB-FEATURE-OPTIONS-STARTUP", "each-static-invariant-is-causal")]
    public void InvalidStaticFeatureOption_FailsTheStartupValidatorWithItsOwnCause(
        InvalidFeatureOption invalid,
        string expectedProperty)
    {
        using ServiceProvider provider = CreateProvider(invalid);

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IStartupValidator>().Validate());

        string failure = string.Join(Environment.NewLine, exception.Failures);
        Assert.Contains(expectedProperty, failure, StringComparison.Ordinal);
        Assert.Contains("for bus 'default':", failure, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FEATURE-OPTIONS-STARTUP", "valid-feature-options-start-together")]
    public void PositiveCoherentFeatureOptions_PassTheSameStartupBoundary()
    {
        using ServiceProvider provider = CreateProvider(null);

        provider.GetRequiredService<IStartupValidator>().Validate();
    }

    static ServiceProvider CreateProvider(InvalidFeatureOption? invalid)
    {
        var services = new ServiceCollection();
        services.AddViciOneServiceBusTextWriterLogger(TextWriter.Null);
        services.AddViciOneServiceBus(bus =>
        {
            bus.Limits(MessageLimits.Conservative);
            bus.SetInMemorySagaRepositoryProvider();
            bus.AddJobService(options => ConfigureJobConsumer(options, invalid));
            bus.AddJobSagaStateMachines(options => ConfigureJobSaga(options, invalid));
            bus.ConfigureHealthCheckOptions(options => ConfigureHealth(options, invalid));
            bus.UsingInMemory();
        });

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
    }

    static void ConfigureJobConsumer(JobConsumerOptions options, InvalidFeatureOption? invalid)
    {
        switch (invalid)
        {
            case InvalidFeatureOption.JobHeartbeat:
                options.HeartbeatInterval = TimeSpan.Zero;
                break;
            case InvalidFeatureOption.JobRejectedDelay:
                options.RejectedJobDelay = TimeSpan.Zero;
                break;
            case InvalidFeatureOption.JobTimeProvider:
                options.TimeProvider = null!;
                break;
        }
    }

    static void ConfigureJobSaga(JobSagaOptions options, InvalidFeatureOption? invalid)
    {
        switch (invalid)
        {
            case InvalidFeatureOption.JobSagaSlotWait:
                options.SlotWaitTime = TimeSpan.Zero;
                break;
            case InvalidFeatureOption.JobSagaStatusCheck:
                options.StatusCheckInterval = TimeSpan.FromSeconds(29);
                break;
            case InvalidFeatureOption.JobSagaHeartbeat:
                options.HeartbeatTimeout = TimeSpan.Zero;
                break;
            case InvalidFeatureOption.JobSagaConcurrency:
                options.ConcurrentMessageLimit = 0;
                break;
            case InvalidFeatureOption.JobSagaRetryCount:
                options.SuspectJobRetryCount = -1;
                break;
            case InvalidFeatureOption.JobSagaRetryDelay:
                options.SuspectJobRetryDelay = TimeSpan.Zero;
                break;
        }
    }

    static void ConfigureHealth(IHealthCheckOptionsConfigurator options, InvalidFeatureOption? invalid)
    {
        switch (invalid)
        {
            case InvalidFeatureOption.HealthName:
                options.Name = " ";
                break;
            case InvalidFeatureOption.HealthStatus:
                options.MinimalFailureStatus = (HealthStatus)42;
                break;
            case InvalidFeatureOption.HealthTag:
                options.Tags.Add(" ");
                break;
        }
    }

    public enum InvalidFeatureOption
    {
        JobHeartbeat,
        JobRejectedDelay,
        JobTimeProvider,
        JobSagaSlotWait,
        JobSagaStatusCheck,
        JobSagaHeartbeat,
        JobSagaConcurrency,
        JobSagaRetryCount,
        JobSagaRetryDelay,
        HealthName,
        HealthStatus,
        HealthTag,
    }
}
