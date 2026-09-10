using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Testing;

public sealed class TestHarnessOptionsStartupValidationTests
{
    [Theory]
    [InlineData(InvalidOption.SaveMode, "ContextSaveMode")]
    [InlineData(InvalidOption.MaximumSavedContexts, "MaximumSavedContexts")]
    [InlineData(InvalidOption.LogLevel, "LogLevel")]
    [RequirementCoverage("REQ-VSB-TEST-OPTIONS-STARTUP", "each-static-invariant-is-causal")]
    public void InvalidHarnessOrLoggerOption_FailsBeforeTheHarnessStarts(InvalidOption invalid, string property)
    {
        using ServiceProvider provider = Provider(invalid);

        Exception exception = Assert.ThrowsAny<Exception>(
            () => provider.GetRequiredService<IStartupValidator>().Validate());

        Assert.Contains(property, exception.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(InvalidOption.TestTimeout, "testTimeout")]
    [InlineData(InvalidOption.InactivityTimeout, "testInactivityTimeout")]
    [RequirementCoverage("REQ-VSB-TEST-OPTIONS-STARTUP", "invalid-timeout-rejected-at-configuration-boundary")]
    public void InvalidTimeout_IsRejectedAtTheConfigurationBoundary(InvalidOption invalid, string parameter)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => Provider(invalid));

        Assert.Equal(parameter, exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-OPTIONS-STARTUP", "coherent-options-pass")]
    public void CoherentHarnessAndLoggerOptions_PassTheSameStartupBoundary()
    {
        using ServiceProvider provider = Provider(null);

        provider.GetRequiredService<IStartupValidator>().Validate();
    }

    static ServiceProvider Provider(InvalidOption? invalid)
    {
        var services = new ServiceCollection();
        services.AddViciOneServiceBusTestHarness(bus =>
        {
            if (invalid == InvalidOption.TestTimeout)
                bus.SetTestTimeouts(testTimeout: TimeSpan.Zero);
            if (invalid == InvalidOption.InactivityTimeout)
                bus.SetTestTimeouts(testInactivityTimeout: TimeSpan.Zero);
        });
        services.Configure<TestHarnessOptions>(options =>
        {
            if (invalid == InvalidOption.SaveMode)
                options.ContextSaveMode = (TestContextSaveMode)42;
            if (invalid == InvalidOption.MaximumSavedContexts)
                options.MaximumSavedContexts = 0;
        });
        services.Configure<TextWriterLoggerOptions>(options =>
        {
            if (invalid == InvalidOption.LogLevel)
                options.LogLevel = (LogLevel)42;
        });
        return services.BuildServiceProvider();
    }

    public enum InvalidOption
    {
        TestTimeout,
        InactivityTimeout,
        SaveMode,
        MaximumSavedContexts,
        LogLevel,
    }
}
