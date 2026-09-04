using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration.DependencyInjection;

public sealed class ViciOneServiceBusHostOptionsValidationTests
{
    [Theory]
    [InlineData(InvalidHostOptions.StartTimeout, "StartTimeout")]
    [InlineData(InvalidHostOptions.StopTimeout, "StopTimeout")]
    [InlineData(InvalidHostOptions.ConsumerStopTimeout, "ConsumerStopTimeout")]
    [InlineData(InvalidHostOptions.ConsumerExceedsStop, "ConsumerStopTimeout must be less than or equal to StopTimeout")]
    [RequirementCoverage("REQ-VSB-HOST-OPTIONS-STARTUP", "every-invalid-static-lifecycle-policy")]
    public void StartupValidator_RejectsEveryInvalidStaticLifecyclePolicy(
        InvalidHostOptions invalid,
        string expectedReason)
    {
        using ServiceProvider provider = CreateProvider(options =>
        {
            switch (invalid)
            {
                case InvalidHostOptions.StartTimeout:
                    options.StartTimeout = TimeSpan.Zero;
                    break;
                case InvalidHostOptions.StopTimeout:
                    options.StopTimeout = TimeSpan.FromTicks(-1);
                    break;
                case InvalidHostOptions.ConsumerStopTimeout:
                    options.ConsumerStopTimeout = TimeSpan.Zero;
                    break;
                case InvalidHostOptions.ConsumerExceedsStop:
                    options.StopTimeout = TimeSpan.FromSeconds(5);
                    options.ConsumerStopTimeout = TimeSpan.FromSeconds(6);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(invalid), invalid, "Unknown invalid host policy.");
            }
        });

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IStartupValidator>().Validate());

        Assert.Contains(expectedReason, string.Join(" | ", exception.Failures), StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-HOST-OPTIONS-STARTUP", "valid-lifecycle-policy")]
    public void StartupValidator_AcceptsPositiveCoherentLifecyclePolicy()
    {
        using ServiceProvider provider = CreateProvider(options =>
        {
            options.StartTimeout = TimeSpan.FromSeconds(10);
            options.StopTimeout = TimeSpan.FromSeconds(10);
            options.ConsumerStopTimeout = TimeSpan.FromSeconds(10);
        });

        provider.GetRequiredService<IStartupValidator>().Validate();
    }

    private static ServiceProvider CreateProvider(Action<ViciOneServiceBusHostOptions> configure)
    {
        var services = new ServiceCollection();
        services.AddViciOneServiceBusTextWriterLogger(TextWriter.Null);
        services.AddOptions<ViciOneServiceBusHostOptions>().Configure(configure);
        services.AddViciOneServiceBus(configuration => configuration.UsingInMemory());
        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
    }

    public enum InvalidHostOptions
    {
        StartTimeout,
        StopTimeout,
        ConsumerStopTimeout,
        ConsumerExceedsStop,
    }
}
