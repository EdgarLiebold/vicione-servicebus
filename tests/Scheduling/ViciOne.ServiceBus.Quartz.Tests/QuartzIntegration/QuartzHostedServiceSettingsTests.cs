using Quartz;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Quartz.Tests.QuartzIntegration;

public sealed class QuartzHostedServiceSettingsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-HOSTED-SETTINGS", "immutable-snapshot")]
    public void From_SnapshotsEveryRuntimeValue()
    {
        var options = new QuartzHostedServiceOptions
        {
            StartDelay = TimeSpan.FromSeconds(17),
            WaitForJobsToComplete = true,
        };

        QuartzHostedServiceSettings settings = QuartzHostedServiceSettings.From(options);
        options.StartDelay = TimeSpan.FromDays(1);
        options.WaitForJobsToComplete = false;

        Assert.Equal(TimeSpan.FromSeconds(17), settings.StartDelay);
        Assert.True(settings.WaitForJobsToComplete);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-HOSTED-SETTINGS", "missing-options")]
    public void From_RejectsMissingOptions()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            QuartzHostedServiceSettings.From(null!));

        Assert.Equal("options", exception.ParamName);
    }
}
