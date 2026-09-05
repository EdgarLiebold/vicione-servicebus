using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.RabbitMq.Testing;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.Configuration;

public sealed class RabbitMqTestHarnessOptionsStartupTests
{
    [Fact]
    public void ForceCleaningRootWithoutCleaning_FailsAtStartup()
    {
        using ServiceProvider provider = new ServiceCollection()
            .ConfigureRabbitMqTestOptions(options =>
            {
                options.ForceCleanRootVirtualHost = true;
                options.CleanVirtualHost = false;
            })
            .BuildServiceProvider();

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IStartupValidator>().Validate());

        Assert.Contains("ForceCleanRootVirtualHost", string.Join(Environment.NewLine, exception.Failures), StringComparison.Ordinal);
    }

    [Fact]
    public void CoherentHarnessOptions_PassTheSameStartupBoundary()
    {
        using ServiceProvider provider = new ServiceCollection()
            .ConfigureRabbitMqTestOptions(options =>
            {
                options.CleanVirtualHost = true;
                options.ForceCleanRootVirtualHost = true;
            })
            .BuildServiceProvider();

        provider.GetRequiredService<IStartupValidator>().Validate();
    }
}
