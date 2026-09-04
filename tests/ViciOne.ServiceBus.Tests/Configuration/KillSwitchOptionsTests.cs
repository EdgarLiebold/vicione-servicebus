using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.KillSwitch;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration;

public sealed class KillSwitchOptionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-KILL-SWITCH-CONFIGURATION", "unambiguous-defaults")]
    public void Defaults_AreOperationallySafeAndUseStandardTime()
    {
        var options = new KillSwitchOptions();

        Assert.Equal(100, options.ActivationThreshold);
        Assert.Equal(0.10, options.TripThresholdRatio);
        Assert.Equal(TimeSpan.FromMinutes(1), options.TrackingPeriod);
        Assert.Equal(TimeSpan.FromMinutes(1), options.RestartDelay);
        Assert.Same(TimeProvider.System, options.TimeProvider);
        Assert.True(options.ExceptionFilter.Match(new InvalidOperationException("matching default")));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-KILL-SWITCH-CONFIGURATION", "threshold-validation")]
    public void ThresholdConfiguration_RejectsEveryInvalidBoundary()
    {
        var options = new KillSwitchOptions();

        Assert.Equal("value", Assert.Throws<ArgumentOutOfRangeException>(() => options.SetActivationThreshold(0)).ParamName);
        Assert.Equal("value", Assert.Throws<ArgumentOutOfRangeException>(() => options.SetTripThresholdRatio(-0.01)).ParamName);
        Assert.Equal("value", Assert.Throws<ArgumentOutOfRangeException>(() => options.SetTripThresholdRatio(1.01)).ParamName);
        Assert.Equal("value", Assert.Throws<ArgumentOutOfRangeException>(() => options.SetTripThresholdRatio(double.NaN)).ParamName);
        Assert.Equal("value", Assert.Throws<ArgumentOutOfRangeException>(() => options.SetTripThresholdRatio(double.PositiveInfinity)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-KILL-SWITCH-CONFIGURATION", "time-validation")]
    public void TimeConfiguration_AcceptsExactMinimumsAndRejectsValuesBelowThem()
    {
        var options = new KillSwitchOptions();

        Assert.Same(options, options.SetTrackingPeriod(TimeSpan.FromTicks(1)));
        Assert.Equal(TimeSpan.FromTicks(1), options.TrackingPeriod);
        Assert.Same(options, options.SetRestartDelay(TimeSpan.FromSeconds(1)));
        Assert.Equal(TimeSpan.FromSeconds(1), options.RestartDelay);
        Assert.Equal("value", Assert.Throws<ArgumentOutOfRangeException>(() => options.SetTrackingPeriod(TimeSpan.Zero)).ParamName);
        Assert.Equal("value", Assert.Throws<ArgumentOutOfRangeException>(() => options.SetTrackingPeriod(TimeSpan.FromTicks(-1))).ParamName);
        Assert.Equal("value", Assert.Throws<ArgumentOutOfRangeException>(() => options.SetRestartDelay(TimeSpan.Zero)).ParamName);
        Assert.Equal("value", Assert.Throws<ArgumentOutOfRangeException>(() => options.SetRestartDelay(TimeSpan.FromSeconds(1) - TimeSpan.FromTicks(1))).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-KILL-SWITCH-CONFIGURATION", "required-collaborators")]
    public void CollaboratorConfiguration_RejectsNullInsteadOfSilentlyUsingDefaults()
    {
        var options = new KillSwitchOptions();

        Assert.Equal("value", Assert.Throws<ArgumentNullException>(() => options.SetTimeProvider(null!)).ParamName);
        Assert.Equal("configure", Assert.Throws<ArgumentNullException>(() => options.SetExceptionFilter(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-KILL-SWITCH-CONFIGURATION", "matching-exception-filter")]
    public void ExceptionFilter_CountsOnlyTheConfiguredExceptionFamily()
    {
        var options = new KillSwitchOptions()
            .SetExceptionFilter(filter => filter.Handle<InvalidOperationException>());

        Assert.True(options.ExceptionFilter.Match(new InvalidOperationException("included")));
        Assert.False(options.ExceptionFilter.Match(new ArgumentException("excluded")));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-KILL-SWITCH-CONFIGURATION", "immutable-runtime-snapshot")]
    public void RuntimeSnapshot_IsUnaffectedByLaterOptionMutations()
    {
        var initialTime = new FakeTimeProvider(new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var laterTime = new FakeTimeProvider(new DateTimeOffset(2040, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var options = new KillSwitchOptions()
            .SetActivationThreshold(4)
            .SetTripThresholdRatio(0.25)
            .SetTrackingPeriod(TimeSpan.FromSeconds(30))
            .SetRestartDelay(TimeSpan.FromSeconds(2))
            .SetTimeProvider(initialTime);
        var driver = new KillSwitchTestDriver(options, new BusLogContext(NullLoggerFactory.Instance));

        options
            .SetActivationThreshold(8)
            .SetTripThresholdRatio(0.75)
            .SetTrackingPeriod(TimeSpan.FromMinutes(2))
            .SetRestartDelay(TimeSpan.FromSeconds(9))
            .SetTimeProvider(laterTime);

        Assert.Equal(4, driver.Settings.ActivationThreshold);
        Assert.Equal(0.25, driver.Settings.TripThresholdRatio);
        Assert.Equal(TimeSpan.FromSeconds(30), driver.Settings.TrackingPeriod);
        Assert.Equal(TimeSpan.FromSeconds(2), driver.Settings.RestartDelay);
        Assert.Same(initialTime, driver.Settings.TimeProvider);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-KILL-SWITCH-CONFIGURATION", "greenfield-public-surface")]
    public void PublicSurface_UsesRatioAndDelayTermsAndHidesRuntimeStates()
    {
        string[] fluentMethods = typeof(KillSwitchOptions)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .Select(method => method.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();
        string[] publicRuntimeTypes = typeof(KillSwitchOptions).Assembly
            .GetExportedTypes()
            .Select(type => type.FullName ?? type.Name)
            .Where(name => name.Contains("KillSwitchState", StringComparison.Ordinal)
                || name.EndsWith(".IKillSwitch", StringComparison.Ordinal))
            .ToArray();

        Assert.Equal(
            [
                "SetActivationThreshold",
                "SetExceptionFilter",
                "SetRestartDelay",
                "SetTimeProvider",
                "SetTrackingPeriod",
                "SetTripThresholdRatio",
            ],
            fluentMethods);
        Assert.Empty(publicRuntimeTypes);
        Assert.True(typeof(KillSwitchOptions).IsSealed);
        Assert.All(
            typeof(KillSwitchOptions).GetProperties(BindingFlags.Instance | BindingFlags.Public),
            property => Assert.False(property.SetMethod?.IsPublic ?? false));
    }
}
