using System.Threading.Channels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Providers.Persistence;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Tests.Outbox;

public sealed class EntityFrameworkReliableInboxPipelineTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-EF-RELIABLE-INBOX-PIPELINE", "failure-state-write-and-logger-fault-preserve-consumer-failure-and-rollback")]
    public async Task FailureStateWriteFailure_PreservesConsumerFailureAndRollsBackEveryEffectAsync(bool loggerThrows)
    {
        var persistenceFailure = new DbUpdateException("failure-state-write");
        var interceptor = new FailureStateWriteInterceptor(persistenceFailure);
        var logger = new FailureStateLogger(loggerThrows);
        await using ReliableInboxFixture fixture = await ReliableInboxFixture.CreateAsync(interceptor, logger);
        var command = new ReliableInboxCommand(Guid.NewGuid(), FailuresBeforeSuccess: 0);
        Guid messageId = Guid.NewGuid();
        var options = new OutboxConsumeOptions
        {
            ConsumerId = Guid.NewGuid(),
            ConsumerType = nameof(ReliableInboxCommandConsumer),
            MessageDeliveryLimit = 1,
            MessageDeliveryTimeout = TimeSpan.FromMinutes(1),
        };
        var consumerFailure = new ExpectedConsumerFailure();
        var invocations = 0;
        await using (AsyncServiceScope scope = fixture.Services.CreateAsyncScope())
        {
            ReliableInboxDbContext db = scope.ServiceProvider.GetRequiredService<ReliableInboxDbContext>();
            IPublishEndpoint publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
            var factory = scope.ServiceProvider.GetRequiredService<
                IOutboxContextFactory<EntityFrameworkReliableInboxScope<IBus, ReliableInboxDbContext>>>();
            ConsumeContext<ReliableInboxCommand> input = InMemoryOutboxTestContextFactory.Create(
                command, fixture.CancellationToken, messageId: messageId);

            ExpectedConsumerFailure actual = await Assert.ThrowsAsync<ExpectedConsumerFailure>(() =>
                factory.SendAsync(input, options, Pipe.ExecuteAwaited<OutboxConsumeContext<ReliableInboxCommand>>(
                    async context =>
                    {
                        invocations++;
                        db.BusinessRecords.Add(new ReliableBusinessRecord { Id = command.CorrelationId, Value = "rolled-back" });
                        await publishEndpoint.PublishAsync(new ReliableInboxEvent(command.CorrelationId), fixture.CancellationToken);
                        await context.SetConsumedAsync(fixture.CancellationToken);
                        Assert.True(await db.BusinessRecords.AnyAsync(fixture.CancellationToken));
                        Assert.True(await db.Set<DurableSendRecord>().AnyAsync(fixture.CancellationToken));
                        throw consumerFailure;
                    }), fixture.CancellationToken));

            Assert.Same(consumerFailure, actual);
            Assert.Empty(db.ChangeTracker.Entries());
        }

        Assert.Equal(1, invocations);
        Assert.Equal(1, interceptor.FailureWriteAttempts);
        Assert.Equal(CancellationToken.None, interceptor.FailureWriteToken);
        Assert.Equal(1, logger.Count);
        Assert.Same(persistenceFailure, logger.Exception);
        Assert.Equal(0, fixture.Events.Count);
        await using ReliableInboxDbContext verification = fixture.CreateContext();
        Assert.Empty(await verification.BusinessRecords.ToListAsync(fixture.CancellationToken));
        Assert.Empty(await verification.Set<ReliableInboxRecord>().ToListAsync(fixture.CancellationToken));
        Assert.Empty(await verification.Set<DurableSendRecord>().ToListAsync(fixture.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-RELIABLE-INBOX-PIPELINE", "business-inbox-outbox-commit-and-duplicate-suppression")]
    public async Task SuccessfulConsumer_CommitsBusinessInboxAndOutboxAtomicallyAndSuppressesDuplicateAsync()
    {
        await using ReliableInboxFixture fixture = await ReliableInboxFixture.CreateAsync();
        Guid messageId = Guid.NewGuid();
        var command = new ReliableInboxCommand(Guid.NewGuid(), FailuresBeforeSuccess: 0);

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
        var command = new ReliableInboxCommand(Guid.NewGuid(), FailuresBeforeSuccess: 1);

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
        Assert.Equal(typeof(ExpectedConsumerFailure).FullName, scheduled.FailureType);

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

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-RELIABLE-INBOX-PIPELINE", "repeated-failures-advance-attempts-and-reach-quarantine")]
    public async Task RepeatedConsumerFailure_AdvancesEveryAttemptAndQuarantinesAtTheConfiguredLimitAsync()
    {
        await using ReliableInboxFixture fixture = await ReliableInboxFixture.CreateAsync();
        Guid messageId = Guid.NewGuid();
        var command = new ReliableInboxCommand(Guid.NewGuid(), FailuresBeforeSuccess: int.MaxValue);

        await fixture.Harness.Bus.PublishAsync(
            command,
            send => send.MessageId = messageId,
            fixture.CancellationToken);
        await fixture.Attempts.FirstFailure.Task.WaitAsync(fixture.Timeout, fixture.CancellationToken);

        ReliableInboxRecord quarantined = await fixture.WaitForInboxAsync(
            messageId,
            ReliableInboxStatus.Quarantined,
            fixture.CancellationToken);

        Assert.Equal(3, quarantined.Attempts);
        Assert.Null(quarantined.DueAt);
        Assert.Null(quarantined.LeaseToken);
        Assert.Null(quarantined.LeaseExpiresAt);
        Assert.NotNull(quarantined.FailedAt);
        Assert.NotNull(quarantined.QuarantinedAt);
        Assert.Equal(typeof(ExpectedConsumerFailure).FullName, quarantined.FailureType);
        Assert.Equal(3, fixture.Attempts.Count);
        Assert.Equal(0, fixture.Events.Count);
        await using ReliableInboxDbContext verification = fixture.CreateContext();
        Assert.Empty(await verification.BusinessRecords.ToListAsync(fixture.CancellationToken));

        var duplicateOptions = new OutboxConsumeOptions
        {
            ConsumerId = quarantined.ConsumerId,
            ConsumerType = nameof(ReliableInboxCommandConsumer),
            MessageDeliveryLimit = 1,
            MessageDeliveryTimeout = TimeSpan.FromMinutes(1),
        };
        var duplicateInvocations = 0;
        await using (AsyncServiceScope scope = fixture.Services.CreateAsyncScope())
        {
            var factory = scope.ServiceProvider.GetRequiredService<
                IOutboxContextFactory<EntityFrameworkReliableInboxScope<IBus, ReliableInboxDbContext>>>();
            ConsumeContext<ReliableInboxCommand> duplicate = InMemoryOutboxTestContextFactory.Create(
                command, fixture.CancellationToken, messageId: messageId);
            await factory.SendAsync(
                duplicate,
                duplicateOptions,
                Pipe.ExecuteAwaited<OutboxConsumeContext<ReliableInboxCommand>>(_ =>
                {
                    duplicateInvocations++;
                    return Task.CompletedTask;
                }),
                fixture.CancellationToken);
        }

        ReliableInboxRecord stillQuarantined = await verification.Set<ReliableInboxRecord>().AsNoTracking()
            .SingleAsync(row => row.MessageId == messageId, fixture.CancellationToken);
        Assert.Equal(0, duplicateInvocations);
        Assert.Equal(ReliableInboxStatus.Quarantined, stillQuarantined.Status);
        Assert.Equal(3, stillQuarantined.Attempts);
        Assert.Equal(quarantined.FailedAt, stillQuarantined.FailedAt);
        Assert.Equal(quarantined.QuarantinedAt, stillQuarantined.QuarantinedAt);
        Assert.Equal(quarantined.FailureType, stillQuarantined.FailureType);
        Assert.Null(stillQuarantined.LeaseToken);
        Assert.Null(stillQuarantined.LeaseExpiresAt);
        Assert.Equal(3, fixture.Attempts.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-RELIABLE-INBOX-PIPELINE", "cancellation-after-save-rolls-back-business-and-consumed-fence-and-allows-first-attempt-retry")]
    public async Task CancellationAfterSave_RollsBackBusinessAndConsumedFenceWithoutChargingAnAttemptAsync()
    {
        await using ReliableInboxFixture fixture = await ReliableInboxFixture.CreateAsync();
        Guid messageId = Guid.NewGuid();
        var command = new ReliableInboxCommand(Guid.NewGuid(), FailuresBeforeSuccess: 0);
        var options = new OutboxConsumeOptions
        {
            ConsumerId = Guid.NewGuid(),
            ConsumerType = nameof(ReliableInboxCommandConsumer),
            MessageDeliveryLimit = 1,
            MessageDeliveryTimeout = TimeSpan.FromMinutes(1),
        };
        using var cancellation = new CancellationTokenSource();
        await using (AsyncServiceScope scope = fixture.Services.CreateAsyncScope())
        {
            ReliableInboxDbContext db = scope.ServiceProvider.GetRequiredService<ReliableInboxDbContext>();
            var factory = scope.ServiceProvider.GetRequiredService<
                IOutboxContextFactory<EntityFrameworkReliableInboxScope<IBus, ReliableInboxDbContext>>>();
            ConsumeContext<ReliableInboxCommand> input = InMemoryOutboxTestContextFactory.Create(
                command, cancellation.Token, messageId: messageId);
            int invocations = 0;
            IPipe<OutboxConsumeContext<ReliableInboxCommand>> next = Pipe.ExecuteAwaited<OutboxConsumeContext<ReliableInboxCommand>>(
                async context =>
                {
                    invocations++;
                    Assert.Equal(1, context.ReceiveCount);
                    db.BusinessRecords.Add(new ReliableBusinessRecord { Id = command.CorrelationId, Value = "canceled" });
                    await context.SetConsumedAsync(cancellation.Token);
                    Assert.Equal(1, await db.BusinessRecords.AsNoTracking().CountAsync(
                        row => row.Id == command.CorrelationId, cancellation.Token));
                    ReliableInboxRecord staged = await db.Set<ReliableInboxRecord>().AsNoTracking().SingleAsync(
                        row => row.MessageId == messageId && row.ConsumerId == options.ConsumerId,
                        cancellation.Token);
                    Assert.Equal(ReliableInboxStatus.Consumed, staged.Status);
                    cancellation.Cancel();
                    throw new OperationCanceledException(cancellation.Token);
                });

            OperationCanceledException failure = await Assert.ThrowsAsync<OperationCanceledException>(() =>
                factory.SendAsync(input, options, next, cancellation.Token));

            Assert.Equal(cancellation.Token, failure.CancellationToken);
            Assert.Equal(1, invocations);
            Assert.Empty(db.ChangeTracker.Entries());
        }

        await using (ReliableInboxDbContext verification = fixture.CreateContext())
        {
            Assert.False(await verification.BusinessRecords.AnyAsync(
                row => row.Id == command.CorrelationId, fixture.CancellationToken));
            Assert.False(await verification.Set<ReliableInboxRecord>().AnyAsync(
                row => row.MessageId == messageId && row.ConsumerId == options.ConsumerId,
                fixture.CancellationToken));
        }

        await using (AsyncServiceScope retryScope = fixture.Services.CreateAsyncScope())
        {
            ReliableInboxDbContext db = retryScope.ServiceProvider.GetRequiredService<ReliableInboxDbContext>();
            var factory = retryScope.ServiceProvider.GetRequiredService<
                IOutboxContextFactory<EntityFrameworkReliableInboxScope<IBus, ReliableInboxDbContext>>>();
            ConsumeContext<ReliableInboxCommand> input = InMemoryOutboxTestContextFactory.Create(
                command, fixture.CancellationToken, messageId: messageId);
            await factory.SendAsync(input, options, Pipe.ExecuteAwaited<OutboxConsumeContext<ReliableInboxCommand>>(
                async context =>
                {
                    Assert.Equal(1, context.ReceiveCount);
                    db.BusinessRecords.Add(new ReliableBusinessRecord { Id = command.CorrelationId, Value = "committed" });
                    await context.SetConsumedAsync(fixture.CancellationToken);
                }), fixture.CancellationToken);
        }

        await using ReliableInboxDbContext final = fixture.CreateContext();
        ReliableInboxRecord inbox = await final.Set<ReliableInboxRecord>().AsNoTracking().SingleAsync(
            row => row.MessageId == messageId && row.ConsumerId == options.ConsumerId,
            fixture.CancellationToken);
        ReliableBusinessRecord business = await final.BusinessRecords.AsNoTracking().SingleAsync(
            row => row.Id == command.CorrelationId, fixture.CancellationToken);
        Assert.Equal(ReliableInboxStatus.Consumed, inbox.Status);
        Assert.Equal(1, inbox.Attempts);
        Assert.Null(inbox.FailedAt);
        Assert.Null(inbox.FailureType);
        Assert.Equal("committed", business.Value);
    }

    public sealed record ReliableInboxCommand(Guid CorrelationId, int FailuresBeforeSuccess);

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

            if (attempt <= context.Message.FailuresBeforeSuccess)
            {
                if (attempt == 1)
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

        public static async Task<ReliableInboxFixture> CreateAsync(
            IInterceptor? interceptor = null,
            ILogger<EntityFrameworkReliableInboxContextFactory<IBus, ReliableInboxDbContext>>? failureLogger = null)
        {
            CancellationToken cancellationToken = TestContext.Current.CancellationToken;
            TimeSpan timeout = TimeSpan.FromSeconds(15);
            string path = Path.Combine(Path.GetTempPath(), $"vicione-reliable-inbox-{Guid.NewGuid():N}.db");
            string connectionString = $"Data Source={path};Default Timeout=30;Pooling=False";
            var attempts = new ConsumerAttemptProbe();
            var events = new EventProbe();
            var services = new ServiceCollection();
            services.AddLogging();
            if (failureLogger is not null)
                services.AddSingleton(failureLogger);
            services.AddSingleton(attempts);
            services.AddSingleton(events);
            services.AddSingleton<IEntityFrameworkDurableSendCommitDurabilityValidator<IBus>, NoOpDurabilityValidator>();
            services.AddPooledDbContextFactory<ReliableInboxDbContext>(builder =>
            {
                builder.UseSqlite(connectionString);
                if (interceptor is not null)
                    builder.AddInterceptors(interceptor);
            });
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
            ReliableInboxRecord? lastObserved = null;
            while (DateTimeOffset.UtcNow < deadline)
            {
                await using ReliableInboxDbContext db = CreateContext();
                lastObserved = await db.Set<ReliableInboxRecord>().AsNoTracking()
                    .SingleOrDefaultAsync(item => item.MessageId == messageId, cancellationToken);
                if (lastObserved?.Status == status)
                    return lastObserved;
                await Task.Delay(TimeSpan.FromMilliseconds(20), cancellationToken);
            }

            string observed = lastObserved is null
                ? "no persisted row"
                : $"status '{lastObserved.Status}', attempts {lastObserved.Attempts}, due '{lastObserved.DueAt:O}'";
            throw new TimeoutException(
                $"Inbox '{messageId}' did not reach '{status}'; last observed {observed}; consumer attempts {Attempts.Count}.");
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

    sealed class FailureStateWriteInterceptor(Exception failure) : SaveChangesInterceptor
    {
        public int FailureWriteAttempts { get; private set; }

        public CancellationToken FailureWriteToken { get; private set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<ReliableInboxRecord>()
                .Any(entry => entry.Entity.Status == ReliableInboxStatus.RetryScheduled))
            {
                FailureWriteAttempts++;
                FailureWriteToken = cancellationToken;
                throw failure;
            }

            return ValueTask.FromResult(result);
        }
    }

    sealed class FailureStateLogger(bool throws) : ILogger<EntityFrameworkReliableInboxContextFactory<IBus, ReliableInboxDbContext>>
    {
        public int Count { get; private set; }

        public Exception? Exception { get; private set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Count++;
            Exception = exception;
            if (throws)
                throw new InvalidOperationException("failure-logger");
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
