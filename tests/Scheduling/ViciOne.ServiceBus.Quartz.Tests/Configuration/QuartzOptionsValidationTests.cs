using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using Xunit;

namespace ViciOne.ServiceBus.Quartz.Tests.Configuration;

public sealed class QuartzOptionsValidationTests
{
    [Theory]
    [InlineData(InvalidOption.Prefetch, "PrefetchCount")]
    [InlineData(InvalidOption.Concurrency, "ConcurrentMessageLimit")]
    [InlineData(InvalidOption.QueueName, "QueueName")]
    [InlineData(InvalidOption.StartDelay, "StartDelay")]
    [InlineData(InvalidOption.RetryPolicy, "DeliveryRetryPolicy")]
    public void InvalidEndpointOption_FailsDuringRegistration(InvalidOption invalid, string property)
    {
        var services = new ServiceCollection();

        ArgumentException exception = Assert.ThrowsAny<ArgumentException>(() =>
            services.AddViciOneServiceBus(bus =>
            {
                bus.Limits(MessageLimits.Conservative);
                bus.AddQuartzScheduling(
                    static _ => null!,
                    options =>
                    {
                        if (invalid == InvalidOption.Prefetch)
                            options.PrefetchCount = 0;
                        if (invalid == InvalidOption.Concurrency)
                            options.ConcurrentMessageLimit = 0;
                        if (invalid == InvalidOption.QueueName)
                            options.QueueName = " ";
                        if (invalid == InvalidOption.StartDelay)
                            options.StartDelay = TimeSpan.FromSeconds(-1);
                        if (invalid == InvalidOption.RetryPolicy)
                            options.DeliveryRetryPolicy = null!;
                    });
            }));

        Assert.Contains(property, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CoherentEndpointOptions_PassTheRegistrationBoundary()
    {
        var services = new ServiceCollection();

        services.AddViciOneServiceBus(bus =>
            bus.AddQuartzScheduling(static _ => null!));
    }

    public enum InvalidOption
    {
        Prefetch,
        Concurrency,
        QueueName,
        StartDelay,
        RetryPolicy,
    }
}
