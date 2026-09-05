using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace ViciOne.ServiceBus.Quartz.Tests.Configuration;

public sealed class QuartzOptionsStartupValidationTests
{
    [Theory]
    [InlineData(InvalidOption.Prefetch, "PrefetchCount")]
    [InlineData(InvalidOption.Concurrency, "ConcurrentMessageLimit")]
    [InlineData(InvalidOption.QueueName, "QueueName")]
    public void InvalidEndpointOption_FailsAtStartup(InvalidOption invalid, string property)
    {
        using ServiceProvider provider = Provider(invalid);

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IStartupValidator>().Validate());

        Assert.Contains(property, string.Join(Environment.NewLine, exception.Failures), StringComparison.Ordinal);
    }

    [Fact]
    public void CoherentEndpointOptions_PassTheSameStartupBoundary()
    {
        using ServiceProvider provider = Provider(null);

        provider.GetRequiredService<IStartupValidator>().Validate();
    }

    static ServiceProvider Provider(InvalidOption? invalid)
    {
        var services = new ServiceCollection();
        services.AddViciOneServiceBus(bus =>
        {
            bus.Limits(MessageLimits.Conservative);
            bus.AddQuartzConsumers(options =>
            {
                if (invalid == InvalidOption.Prefetch)
                    options.PrefetchCount = 0;
                if (invalid == InvalidOption.Concurrency)
                    options.ConcurrentMessageLimit = 0;
                if (invalid == InvalidOption.QueueName)
                    options.QueueName = " ";
            });
            bus.UsingInMemory();
        });
        return services.BuildServiceProvider();
    }

    public enum InvalidOption
    {
        Prefetch,
        Concurrency,
        QueueName,
    }
}
