using System.Threading.Channels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Providers.Persistence;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Tests.Outbox;

public sealed class EntityFrameworkReliableInboxPipelineTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-EF-RELIABLE-INBOX-PIPELINE", "business-inbox-outbox-commit-and-duplicate-suppression")]
    public async Task SuccessfulConsumer_CommitsBusinessInboxAndOutboxAtomicallyAndSuppressesDuplicateAsync()
    {
        await using ReliableInboxFixture fixture = await ReliableInboxFixture.CreateAsync();
        Guid messageId = Guid.NewGuid();
        var command = new ReliableInboxCommand(Guid.NewGuid(), FailFirstAttempt: false);

        await fixture.Harness.Bus.PublishAsync(
            command,
            send => send.MessageId = messageId,
            fixture.CancellationToken);
        ReliableInboxEvent delivered = await fixture.Events.ReadAsync(fixture.Timeout, fixture.CancellationToken);
        Assert.Equal(command.CorrelationId, delivered.CorrelationId);

        await using (ReliableInboxDbContext verification = fixture.CreateContext())
        {
            ReliableBusinessRecord business = await verification.BusinessRecords.AsNoTracking()
                .SingleAsync(row => row.Id == command.CorrelationId, fixture.CancellationToken);
            ReliableInboxRecord inbox = await verification.Set<ReliableInboxRecord>().AsNoTracking()
                .SingleAsync(row => row.MessageId == messageId, fixture.CancellationToken);
            Assert.Equal("committed", business.Value);
            Assert.Equal(ReliableInboxStatus.Consumed, inbox.Status);
            Assert.Equal(1, inbox.Attempts);
            Assert.NotNull(inbox.CompletedAt);
        }

        await fixture.Harness.Bus.PublishAsync(
            command,
            send => send.MessageId = messageId,
            fixture.CancellationToken);
        await fixture.Harness.InactivityTask.WaitAsync(fixture.Timeout, fixture.CancellationToken);

        Assert.Equal(1, fixture.Attempts.Count);
        Assert.Equal(1, fixture.Events.Count);
        await using ReliableInboxDbContext duplicateVerification = fixture.CreateContext();
        Assert.Equal(1, await duplicateVerification.BusinessRecords.CountAsync(fixture.CancellationToken));
        Assert.Equal(1, (await duplicateVerification.Set<ReliableInboxRecord>().AsNoTracking()
            .SingleAsync(row => row.MessageId == messageId, fixture.CancellationToken)).Attempts);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-RELIABLE-INBOX-PIPELINE", "persistent-due-retry-rolls-back-first-attempt")]
    public async Task FailedConsumer_PersistsDueRetryAndOnlyTheSuccessfulAttemptCommitsAsync()
    {
        await using ReliableInboxFixture fixture = await ReliableInboxFixture.CreateAsync();
        Guid messageId = Guid.NewGuid();
        var command = new ReliableInboxCommand(Guid.NewGuid(), FailFirstAttempt: true);

        await fixture.Harness.Bus.PublishAsync(
            command,
            send => send.MessageId = messageId,
            fixture.CancellationToken);
        await fixture.Attempts.FirstFailure.Task.WaitAsync(fixture.Timeout, fixture.CancellationToken);

        ReliableInboxRecord scheduled = await fixture.WaitForInboxAsync(
            messageId,
            ReliableInboxStatus.RetryScheduled,
            fixture.CancellationToken);
        Assert.Equal(1, scheduled.Attempts);
        Assert.NotNull(scheduled.DueAt);
        Assert.NotNull(scheduled.FailedAt);
        Assert.Contains(nameof(ExpectedConsumerFailure), scheduled.FailureType, StringComparison.Ordinal);

        ReliableInboxEvent delivered = await fixture.Events.ReadAsync(fixture.Timeout, fixture.CancellationToken);
        Assert.Equal(command.CorrelationId, delivered.CorrelationId);
        await fixture.Harness.InactivityTask.WaitAsync(fixture.Timeout, fixture.CancellationToken);

        await using ReliableInboxDbContext verification = fixture.CreateContext();
        ReliableInboxRecord consumed = await verification.Set<ReliableInboxRecord>().AsNoTracking()
            .SingleAsync(row => row.MessageId == messageId, fixture.CancellationToken);
        Assert.Equal(ReliableInboxStatus.Consumed, consumed.Status);
        Assert.Equal(2, consumed.Attempts);
        Assert.Null(consumed.DueAt);
        Assert.Null(consumed.FailedAt);
        Assert.Null(consumed.FailureType);
        Assert.Equal(1, await verification.BusinessRecords.CountAsync(
            row => row.Id == command.CorrelationId,
            fixture.CancellationToken));
        Assert.Equal(2, fixture.Attempts.Count);
        Assert.Equal(1, fixture.Events.Count);
    }

    public sealed record ReliableInboxCommand(Guid CorrelationId, bool FailFirstAttempt);

    public sealed record ReliableInboxEvent(Guid CorrelationId);

    public sealed class ReliableInboxCommandConsumer(
        ReliableInboxDbContext dbContext,
        ConsumerAttemptProbe attempts) : IConsumer<ReliableInboxCommand>
    {
        public async Task ConsumeAsync(ConsumeContext<ReliableInboxCommand> context)
        {
            int attempt = attempts.Increment();
            dbContext.BusinessRecords.Add(new ReliableBusinessRecord
            {
                Id = context.Message.CorrelationId,
                Value = "committed",
            });
            await context.Advanced().PublishAsync(
                new ReliableInboxEvent(context.Message.CorrelationId),
                context.CancellationToken);

            if (context.Message.FailFirstAttempt && attempt == 1)
            {
                attempts.FirstFailure.TrySetResult();
                throw new ExpectedConsumerFailure();
            }
        }
    }

    public sealed class ReliableInboxEventConsumer(EventProbe events) : IConsumer<ReliableInboxEvent>
    {
        public Task ConsumeAsync(ConsumeContext<ReliableInboxEvent> context)
        {
            events.Record(context.Message);
            return Task.CompletedTask;
        }
    }

    public sealed class ReliableBusinessRecord
    {
        public Guid Id { get; set; }

        public required string Value { get; set; }
    }

    public sealed class ReliableInboxDbContext(DbContextOptions<ReliableInboxDbContext> options) : DbContext(options)
    {
        public DbSet<ReliableBusinessRecord> BusinessRecords => Set<ReliableBusinessRecord>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ReliableBusinessRecord>().HasKey(row => row.Id);
            modelBuilder.AddViciOneReliableMessaging();
        }
    }

    public sealed class ExpectedConsumerFailure : Exception;

    public sealed class ConsumerAttemptProbe
    {
        int _count;

        public int Count => Volatile.Read(ref _count);

        public TaskCompletionSource FirstFailure { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int Increment() => Interlocked.Increment(ref _count);
    }

    public sealed class EventProbe
    {
        readonly Channel<ReliableInboxEvent> _events = Channel.CreateUnbounded<ReliableInboxEvent>();
        int _count;

        public int Count => Volatile.Read(ref _count);

        public void Record(ReliableInboxEvent message)
        {
            Interlocked.Increment(ref _count);
            if (!_events.Writer.TryWrite(message))
                throw new InvalidOperationException("The reliable-inbox event probe rejected an event.");
        }

        public Task<ReliableInboxEvent> ReadAsync(TimeSpan timeout, CancellationToken cancellationToken) =>
            _events.Reader.ReadAsync(cancellationToken).AsTask().WaitAsync(timeout, cancellationToken);
    }

    sealed class ReliableInboxFixture : IAsyncDisposable
    {
        readonly string _connectionString;
        readonly string _path;

        ReliableInboxFixture(
            string path,
            string connectionString,
            ServiceProvider services,
            ITestHarness harness,
            ConsumerAttemptProbe attempts,
            EventProbe events,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            _path = path;
            _connectionString = connectionString;
            Services = services;
            Harness = harness;
            Attempts = attempts;
            Events = events;
            Timeout = timeout;
            CancellationToken = cancellationToken;
        }

        public ConsumerAttemptProbe Attempts { get; }

        public CancellationToken CancellationToken { get; }

        public EventProbe Events { get; }

        public ITestHarness Harness { get; }

        public ServiceProvider Services { get; }

        public TimeSpan Timeout { get; }

        public ReliableInboxDbContext CreateContext() => new(
            new DbContextOptionsBuilder<ReliableInboxDbContext>().UseSqlite(_connectionString).Options);

        public static async Task<ReliableInboxFixture> CreateAsync()
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            TimeSpan timeout = TimeSpan.FromSeconds(15);
            string path = Path.Combine(Path.GetTempPath(), $"vicione-reliable-inbox-{Guid.NewGuid():N}.db");
            string connectionString = $"Data Source={path};Default Timeout=30;Pooling=False";
            var attempts = new ConsumerAttemptProbe();
            var events = new EventProbe();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(attempts);
            services.AddSingleton(events);
            services.AddSingleton<IEntityFrameworkDurableSendCommitDurabilityValidator<IBus>, NoOpDurabilityValidator>();
            services.AddPooledDbContextFactory<ReliableInboxDbContext>(builder => builder.UseSqlite(connectionString));
            services.AddViciOneServiceBusTestHarness(TextWriter.Null, configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.Limits(MessageLimits.Conservative);
                configuration.AddConsumer<ReliableInboxCommandConsumer>();
                configuration.AddConsumer<ReliableInboxEventConsumer>();
                configuration.UseReliableMessaging(reliable =>
                {
                    reliable.UseEntityFramework<ReliableInboxDbContext>();
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
                    reliable.AddMessageContract<ReliableInboxCommand>("reliable-inbox-command");
                    reliable.AddMessageContract<ReliableInboxEvent>("reliable-inbox-event");
                });
            });

            ServiceProvider? provider = null;
            try
            {
                provider = services.BuildServiceProvider(new ServiceProviderOptions
                {
                    ValidateOnBuild = true,
                    ValidateScopes = true,
                });
                await using (AsyncServiceScope scope = provider.CreateAsyncScope())
                {
                    ReliableInboxDbContext db = scope.ServiceProvider.GetRequiredService<ReliableInboxDbContext>();
                    await db.Database.EnsureCreatedAsync(cancellationToken);
                }

                ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: cancellationToken)
                    .WaitAsync(timeout, cancellationToken);
                return new ReliableInboxFixture(
                    path,
                    connectionString,
                    provider,
                    harness,
                    attempts,
                    events,
                    timeout,
                    cancellationToken);
            }
            catch
            {
                if (provider is not null)
                    await provider.DisposeAsync();
                Delete(path);
                throw;
            }
        }

        public async Task<ReliableInboxRecord> WaitForInboxAsync(
            Guid messageId,
            ReliableInboxStatus status,
            CancellationToken cancellationToken)
        {
            DateTimeOffset deadline = DateTimeOffset.UtcNow + Timeout;
            while (DateTimeOffset.UtcNow < deadline)
            {
                await using ReliableInboxDbContext db = CreateContext();
                ReliableInboxRecord? row = await db.Set<ReliableInboxRecord>().AsNoTracking()
                    .SingleOrDefaultAsync(item => item.MessageId == messageId, cancellationToken);
                if (row?.Status == status)
                    return row;
                await Task.Delay(TimeSpan.FromMilliseconds(20), cancellationToken);
            }

            throw new TimeoutException($"Inbox '{messageId}' did not reach '{status}'.");
        }

        public async ValueTask DisposeAsync()
        {
            await Harness.StopAsync(CancellationToken.None).WaitAsync(Timeout, CancellationToken.None);
            await Services.DisposeAsync();
            Delete(_path);
        }

        static void Delete(string path)
        {
            File.Delete(path);
            File.Delete(path + "-wal");
            File.Delete(path + "-shm");
        }
    }

    sealed class NoOpDurabilityValidator : IEntityFrameworkDurableSendCommitDurabilityValidator<IBus>
    {
        public Task ValidateAsync(DbContext dbContext, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dbContext);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }
}
