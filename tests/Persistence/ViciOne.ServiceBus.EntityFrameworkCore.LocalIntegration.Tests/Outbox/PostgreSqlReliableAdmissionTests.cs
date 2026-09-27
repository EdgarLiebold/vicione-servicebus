using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.EntityFrameworkCore.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCore.LocalIntegration.Tests.Outbox;

public sealed class PostgreSqlReliableAdmissionTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-EF-RELIABLE-INBOX-PIPELINE", "postgresql-admission-failure-rolls-back-business-and-earlier-intent-before-recovery")]
    public async Task RejectedSecondPublish_RollsBackPostgreSqlTransactionAndRecoversOnlyReplacementAsync()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase database = await PostgreSqlTestDatabase.CreateAsync("admission", token);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPooledDbContextFactory<AdmissionDbContext>(options => options.UseNpgsql(database.ConnectionString));
        services.AddViciOneServiceBusTestHarness(TextWriter.Null, configuration =>
        {
            configuration.SetTestTimeouts(database.OperationTimeout, database.OperationTimeout);
            configuration.Limits(new MessageLimits
            {
                MaxBodyBytes = 256,
                MaxEnvelopeBytes = 8192,
                MaxJsonDepth = 32,
            });
            configuration.UseReliableMessaging(reliable =>
            {
                reliable.UseEntityFramework<AdmissionDbContext>();
                reliable.Store(new ReliableStoreLimits
                {
                    MaximumStoredCount = 100,
                    MaximumStoredBytes = 1024 * 1024,
                });
                reliable.Delivery(delivery =>
                {
                    delivery.MaximumAttempts = 3;
                    delivery.InitialRetryDelay = TimeSpan.FromSeconds(1);
                    delivery.MaximumRetryDelay = TimeSpan.FromSeconds(1);
                    delivery.RetryJitterFraction = 0;
                    delivery.PollInterval = TimeSpan.FromMilliseconds(20);
                });
                reliable.Retention(TimeSpan.FromDays(7));
                reliable.AddMessageContract<AdmissionCommand>("postgresql-admission-command");
                reliable.AddMessageContract<AdmissionEvent>("postgresql-admission-event");
            });
        });

        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        await using (AsyncServiceScope schemaScope = provider.CreateAsyncScope())
            await schemaScope.ServiceProvider.GetRequiredService<AdmissionDbContext>()
                .Database.EnsureCreatedAsync(token);
        // The scoped factory is the subject. A running delivery worker would race its
        // serializable transaction while the test deliberately saves a partial attempt.
        {
            Guid inputId = Guid.NewGuid();
            Guid consumerId = Guid.NewGuid();
            Guid firstId = Guid.NewGuid();
            Guid rejectedId = Guid.NewGuid();
            Guid replacementId = Guid.NewGuid();
            var command = new AdmissionCommand(Guid.NewGuid());
            var consumeOptions = new OutboxConsumeOptions
            {
                ConsumerId = consumerId,
                ConsumerType = nameof(PostgreSqlReliableAdmissionTests),
                MessageDeliveryLimit = 1,
                MessageDeliveryTimeout = TimeSpan.FromMinutes(1),
            };

            await using (AsyncServiceScope firstScope = provider.CreateAsyncScope())
            {
                AdmissionDbContext db = firstScope.ServiceProvider.GetRequiredService<AdmissionDbContext>();
                IPublishEndpoint publisher = firstScope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
                var factory = firstScope.ServiceProvider.GetRequiredService<
                    IOutboxContextFactory<EntityFrameworkReliableInboxScope<IBus, AdmissionDbContext>>>();
                ConsumeContext<AdmissionCommand> input = InMemoryOutboxTestContextFactory.Create(
                    command, token, messageId: inputId);
                Exception? failure = await Record.ExceptionAsync(() => factory.SendAsync(input, consumeOptions,
                    Pipe.ExecuteAwaited<OutboxConsumeContext<AdmissionCommand>>(async context =>
                    {
                        db.BusinessRecords.Add(new AdmissionBusiness { Id = command.CorrelationId, Value = "failed" });
                        await publisher.PublishAsync(new AdmissionEvent(command.CorrelationId, "first"), send =>
                        {
                            send.MessageId = firstId;
                            send.Delay = TimeSpan.FromDays(1);
                        }, token);
                        Assert.Equal(firstId, Assert.Single(db.ChangeTracker.Entries<DurableSendRecord>()).Entity.Id);
                        await db.SaveChangesAsync(token);
                        await publisher.PublishAsync(new AdmissionEvent(command.CorrelationId, new string('x', 4096)),
                            send => send.MessageId = rejectedId, token);
                        await context.SetConsumedAsync(token);
                    }), token));
                Assert.NotNull(failure);
                Assert.Equal("ViciOne.ServiceBus.Providers.Persistence.ReliableInboxRetryRequiredException",
                    failure.GetType().FullName);
                PayloadAdmissionException rejected = Assert.IsType<PayloadAdmissionException>(failure.InnerException);
                Assert.Equal(PayloadAdmissionStage.SerializedBody, rejected.Stage);
                Assert.Equal(256, rejected.ConfiguredLimitBytes);
                Assert.True(rejected.ActualBytes > rejected.ConfiguredLimitBytes);
                Assert.Empty(db.ChangeTracker.Entries());
            }

            await using (AdmissionDbContext failed = OpenDatabase(database.ConnectionString))
            {
                Assert.Empty(await failed.BusinessRecords.AsNoTracking().ToArrayAsync(token));
                Assert.Empty(await failed.Set<DurableSendRecord>().AsNoTracking().ToArrayAsync(token));
                ReliableInboxRecord retry = await failed.Set<ReliableInboxRecord>().AsNoTracking().SingleAsync(token);
                Assert.Equal((inputId, consumerId, ReliableInboxStatus.RetryScheduled, 1),
                    (retry.MessageId, retry.ConsumerId, retry.Status, retry.Attempts));
                Assert.Equal(typeof(PayloadAdmissionException).FullName, retry.FailureType);
                Assert.NotNull(retry.DueAt);
                Assert.Empty(await failed.Set<DurableSendCapacityState>().AsNoTracking().ToArrayAsync(token));
            }

            await using (AsyncServiceScope recoveryScope = provider.CreateAsyncScope())
            {
                AdmissionDbContext db = recoveryScope.ServiceProvider.GetRequiredService<AdmissionDbContext>();
                IPublishEndpoint publisher = recoveryScope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
                var factory = recoveryScope.ServiceProvider.GetRequiredService<
                    IOutboxContextFactory<EntityFrameworkReliableInboxScope<IBus, AdmissionDbContext>>>();
                ConsumeContext<AdmissionCommand> input = InMemoryOutboxTestContextFactory.Create(
                    command, token, messageId: inputId);
                await factory.SendAsync(input, consumeOptions,
                    Pipe.ExecuteAwaited<OutboxConsumeContext<AdmissionCommand>>(async context =>
                    {
                        db.BusinessRecords.Add(new AdmissionBusiness { Id = command.CorrelationId, Value = "recovered" });
                        await publisher.PublishAsync(new AdmissionEvent(command.CorrelationId, "replacement"), send =>
                        {
                            send.MessageId = replacementId;
                            send.Delay = TimeSpan.FromDays(1);
                        }, token);
                        await context.SetConsumedAsync(token);
                    }), token);
            }

            await using AdmissionDbContext final = OpenDatabase(database.ConnectionString);
            AdmissionBusiness business = await final.BusinessRecords.AsNoTracking().SingleAsync(token);
            Assert.Equal((command.CorrelationId, "recovered"), (business.Id, business.Value));
            ReliableInboxRecord consumed = await final.Set<ReliableInboxRecord>().AsNoTracking().SingleAsync(token);
            Assert.Equal((inputId, consumerId, ReliableInboxStatus.Consumed, 2),
                (consumed.MessageId, consumed.ConsumerId, consumed.Status, consumed.Attempts));
            Assert.Null(consumed.FailureType);
            DurableSendRecord retained = await final.Set<DurableSendRecord>().AsNoTracking().SingleAsync(token);
            Assert.Equal(replacementId, retained.Id);
            Assert.Equal(command.CorrelationId, retained.CorrelationId);
            Assert.Equal(DurableSendStatus.Pending, retained.Status);
            using (System.Text.Json.JsonDocument envelope = System.Text.Json.JsonDocument.Parse(retained.Body))
            {
                System.Text.Json.JsonElement message = envelope.RootElement.GetProperty("message");
                Assert.Equal("replacement", message.GetProperty("text").GetString());
                Assert.Equal(command.CorrelationId, message.GetProperty("correlationId").GetGuid());
            }
            Assert.NotEqual(firstId, retained.Id);
            Assert.NotEqual(rejectedId, retained.Id);
            DurableSendCapacityState finalCapacity = await final.Set<DurableSendCapacityState>()
                .AsNoTracking().SingleAsync(token);
            Assert.Equal((1, retained.StorageSize), (finalCapacity.StoredCount, finalCapacity.StoredBytes));
        }
    }

    private static AdmissionDbContext OpenDatabase(string connectionString) => new(
        new DbContextOptionsBuilder<AdmissionDbContext>().UseNpgsql(connectionString).Options);

    public sealed record AdmissionCommand(Guid CorrelationId);

    public sealed record AdmissionEvent(Guid CorrelationId, string Text);

    public sealed class AdmissionBusiness
    {
        public Guid Id { get; set; }

        public required string Value { get; set; }
    }

    public sealed class AdmissionDbContext(DbContextOptions<AdmissionDbContext> options) : DbContext(options)
    {
        public DbSet<AdmissionBusiness> BusinessRecords => Set<AdmissionBusiness>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<AdmissionBusiness>().HasKey(row => row.Id);
            builder.AddViciOneReliableMessaging();
        }
    }
}
