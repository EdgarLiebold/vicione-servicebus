using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using ViciOne.ServiceBus.EntityFrameworkCoreIntegration;
using ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.PostgreSql.LocalIntegration.Tests.PostgreSql;

public sealed class PostgreSqlBusOutboxTests
{
    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0060", "postgresql-native-owner")]
    public async Task SerializableTransaction_DefersTheExactEnvelopeUntilCommitAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using PostgreSqlTestDatabase database = await PostgreSqlTestDatabase.CreateAsync(
            "bus-outbox",
            cancellationToken);
        string endpointName = database.Name("outbox-consumer");
        var delivery = new DeliveryProbe();
        var services = new ServiceCollection();
        services.AddSingleton(delivery);
        services.AddDbContext<OutboxDbContext>(options => options.UseNpgsql(database.ConnectionString));
        services.AddViciOneServiceBusTestHarness(TextWriter.Null, configuration =>
        {
            configuration.SetTestTimeouts(database.OperationTimeout, database.OperationTimeout);
            configuration.AddEntityFrameworkOutbox<OutboxDbContext>(outbox =>
            {
                outbox.UsePostgres();
                outbox.DisableInboxCleanupService();
                outbox.QueryDelay = TimeSpan.FromHours(1);
                outbox.UseBusOutbox(busOutbox => busOutbox.MessageDeliveryLimit = 10);
            });
            configuration.AddConsumer<OutboxMessageConsumer>()
                .Endpoint(endpoint => endpoint.Name = endpointName);
            configuration.UsingPostgres(database.ConnectionString, (context, bus) =>
            {
                bus.ConfigureEndpoints(context);
            });
        });
        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        await using (var schema = new OutboxDbContext(
            new DbContextOptionsBuilder<OutboxDbContext>().UseNpgsql(database.ConnectionString).Options))
        {
            await schema.Database.ExecuteSqlRawAsync(
                schema.Database.GenerateCreateScript(),
                cancellationToken);
        }
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(database.OperationTimeout, cancellationToken);

        try
        {
            Guid messageId = Guid.NewGuid();
            Guid conversationId = Guid.NewGuid();
            await using (AsyncServiceScope scope = provider.CreateAsyncScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<OutboxDbContext>();
                var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
                await using var transaction = await dbContext.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);

                await publishEndpoint.PublishAsync(
                    new OutboxProbe("committed"),
                    context =>
                    {
                        context.MessageId = messageId;
                        context.ConversationId = conversationId;
                    },
                    cancellationToken);

                OutboxMessage pending = Assert.Single(dbContext.ChangeTracker.Entries<OutboxMessage>()).Entity;
                Assert.Equal(EntityState.Added, dbContext.Entry(pending).State);
                Assert.Equal(messageId, pending.MessageId);
                Assert.Equal(conversationId, pending.ConversationId);
                Assert.Equal(0, await TransportMessageCountAsync(database, messageId, cancellationToken));

                await dbContext.SaveChangesAsync(cancellationToken);
                Assert.Equal(0, await TransportMessageCountAsync(database, messageId, cancellationToken));
                await using var independent = new OutboxDbContext(
                    new DbContextOptionsBuilder<OutboxDbContext>().UseNpgsql(database.ConnectionString).Options);
                Assert.Empty(await independent.Set<OutboxMessage>().AsNoTracking().ToListAsync(cancellationToken));

                await transaction.CommitAsync(cancellationToken);
            }

            ConsumeContext<OutboxProbe> consumed = await delivery.Consumed
                .WaitAsync(database.OperationTimeout, cancellationToken);

            Assert.Equal(messageId, consumed.MessageId);
            Assert.Equal(conversationId, consumed.ConversationId);
            Assert.Equal("committed", consumed.Message.Value);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None)
                .WaitAsync(database.OperationTimeout, CancellationToken.None);
        }

        Assert.Equal(1, delivery.Count);
    }

    public sealed record OutboxProbe(string Value);

    public sealed class OutboxMessageConsumer(DeliveryProbe delivery) : IConsumer<OutboxProbe>
    {
        public Task ConsumeAsync(ConsumeContext<OutboxProbe> context)
        {
            delivery.Record(context);
            return Task.CompletedTask;
        }
    }

    public sealed class OutboxDbContext(DbContextOptions<OutboxDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddTransactionalOutboxEntities();
        }
    }

    public sealed class DeliveryProbe
    {
        private readonly TaskCompletionSource<ConsumeContext<OutboxProbe>> _consumed =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _count;

        public Task<ConsumeContext<OutboxProbe>> Consumed => _consumed.Task;

        public int Count => Volatile.Read(ref _count);

        public void Record(ConsumeContext<OutboxProbe> context)
        {
            Interlocked.Increment(ref _count);
            _consumed.TrySetResult(context);
        }
    }

    private static async Task<long> TransportMessageCountAsync(
        PostgreSqlTestDatabase database,
        Guid messageId,
        CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = database.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            $"SELECT COUNT(*) FROM \"{database.Schema}\".message WHERE message_id = @messageId",
            connection);
        command.Parameters.AddWithValue("messageId", messageId);
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
    }
}
