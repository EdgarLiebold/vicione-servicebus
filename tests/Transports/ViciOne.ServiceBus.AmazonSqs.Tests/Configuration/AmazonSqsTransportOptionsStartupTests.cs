using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.Tests.Configuration;

public sealed class AmazonSqsTransportOptionsStartupTests
{
    [Theory]
    [InlineData(InvalidOption.Region, "Region")]
    [InlineData(InvalidOption.Scope, "Scope")]
    [InlineData(InvalidOption.ScopeWithoutRegion, "Scope requires Region")]
    public void InvalidOptions_FailBeforeAwsClientCreation(InvalidOption invalid, string reason)
    {
        using ServiceProvider provider = Provider(invalid);

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IStartupValidator>().Validate());

        Assert.Contains(reason, string.Join(Environment.NewLine, exception.Failures), StringComparison.Ordinal);
    }

    [Fact]
    public void EmptyAndCoherentOptions_PassStartupValidation()
    {
        using ServiceProvider empty = Provider(null);
        empty.GetRequiredService<IStartupValidator>().Validate();

        using ServiceProvider coherent = Provider(InvalidOption.None);
        coherent.GetRequiredService<IStartupValidator>().Validate();
    }

    static ServiceProvider Provider(InvalidOption? invalid)
    {
        var services = new ServiceCollection();
        services.Configure<AmazonSqsTransportOptions>(options =>
        {
            if (invalid is null)
                return;

            options.Region = invalid == InvalidOption.Region ? " " : invalid == InvalidOption.ScopeWithoutRegion ? null : "eu-central-1";
            options.Scope = invalid == InvalidOption.Scope ? " " : "orders";
        });
        services.AddViciOneServiceBus(bus =>
        {
            bus.Limits(MessageLimits.Conservative);
            bus.UsingAmazonSqs();
        });
        return services.BuildServiceProvider();
    }

    public enum InvalidOption
    {
        None,
        Region,
        Scope,
        ScopeWithoutRegion,
    }
}
