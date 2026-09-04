using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Tests.Configuration;

public sealed class EntityFrameworkTimeProviderTests
{
    private static readonly DateTimeOffset Now =
        new(2042, 3, 4, 5, 6, 7, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-TIME-SOURCE", "application-clock-is-preserved")]
    public void OutboxRegistration_PreservesTheApplicationTimeProvider()
    {
        var timeProvider = new FakeTimeProvider(Now);
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(timeProvider);
        services.AddViciOneServiceBus(configuration =>
            configuration.AddEntityFrameworkOutbox<TimeDbContext>(outbox =>
            {
                outbox.UseSqlite();
                outbox.DisableInboxCleanupService();
            }));

        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Same(timeProvider, provider.GetRequiredService<TimeProvider>());
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(TimeProvider));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-TIME-SOURCE", "outbox-message-timestamps-use-one-clock")]
    public async Task AddSend_UsesTheInjectedTimeProviderForDerivedTimestampsAsync()
    {
        var timeProvider = new FakeTimeProvider(Now)
        {
            AutoAdvanceAmount = TimeSpan.FromSeconds(1),
        };
        await using var dbContext = new TimeDbContext(
            new DbContextOptionsBuilder<TimeDbContext>()
                .UseSqlite("Data Source=:memory:")
                .Options);
        var sendContext = new MessageSendContext<TimeMessage>(new TimeMessage("payload"))
        {
            Serializer = ServiceBusMetadataJson.MessageSerializer,
            TimeToLive = TimeSpan.FromMinutes(5),
            Delay = TimeSpan.FromMinutes(2),
        };

        OutboxMessage message = OutboxMessageFactory.Create(
            sendContext,
            ServiceBusMetadataJson.ObjectDeserializer,
            timeProvider,
            outboxId: Guid.Parse("7d9cb098-bc59-4f7e-87ea-f98670543c3e"));
        dbContext.Add(message);

        OutboxMessage stored = Assert.Single(dbContext.Set<OutboxMessage>().Local);
        Assert.Equal(Now.UtcDateTime + TimeSpan.FromMinutes(5), stored.ExpirationTime);
        Assert.Equal(Now.UtcDateTime + TimeSpan.FromMinutes(2), stored.EnqueueTime);
        Assert.Equal(Now + TimeSpan.FromSeconds(1), timeProvider.GetUtcNow());
    }

    public sealed record TimeMessage(string Value);

    public sealed class TimeDbContext(DbContextOptions<TimeDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddTransactionalOutboxEntities();
        }
    }
}
