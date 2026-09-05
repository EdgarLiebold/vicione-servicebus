using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.EntityFrameworkCore;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Tests.Configuration;

public sealed class EntityFrameworkOptionsStartupValidationTests
{
    [Theory]
    [InlineData(InvalidOption.LockProvider, "LockStatementProvider")]
    [InlineData(InvalidOption.InboxQueryDelay, "QueryDelay")]
    [InlineData(InvalidOption.DeliveryRetryOrder, "MaximumDeliveryRetryDelay")]
    public void ExternalOptionsOverride_CannotBypassStartupValidation(InvalidOption invalid, string property)
    {
        using ServiceProvider provider = Provider(invalid);

        Exception exception = Assert.ThrowsAny<Exception>(
            () => provider.GetRequiredService<IStartupValidator>().Validate());

        Assert.Contains(property, exception.ToString(), StringComparison.Ordinal);
        Assert.Contains("for bus '", exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void CoherentOutboxInboxAndDeliveryOptions_PassTheSameStartupBoundary()
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
            bus.AddEntityFrameworkOutbox<OptionsDbContext>(outbox =>
            {
                outbox.UseSqlite();
                outbox.UseBusOutbox();
            });
            bus.UsingInMemory();
        });
        services.Configure<EntityFrameworkOutboxOptions<OptionsDbContext>>(options =>
        {
            if (invalid == InvalidOption.LockProvider)
                options.LockStatementProvider = null!;
        });
        services.Configure<InboxCleanupServiceOptions<OptionsDbContext>>(options =>
        {
            if (invalid == InvalidOption.InboxQueryDelay)
                options.QueryDelay = TimeSpan.Zero;
        });
        services.Configure<OutboxDeliveryServiceOptions<EntityFrameworkBusOutboxScope<IBus, OptionsDbContext>>>(options =>
        {
            if (invalid == InvalidOption.DeliveryRetryOrder)
            {
                options.InitialDeliveryRetryDelay = TimeSpan.FromSeconds(2);
                options.MaximumDeliveryRetryDelay = TimeSpan.FromSeconds(1);
            }
        });
        return services.BuildServiceProvider();
    }

    public enum InvalidOption
    {
        LockProvider,
        InboxQueryDelay,
        DeliveryRetryOrder,
    }

    public sealed class OptionsDbContext : DbContext
    {
    }
}
