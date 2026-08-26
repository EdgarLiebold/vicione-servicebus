namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration.LocalIntegration.Tests.EntityFrameworkCoreIntegration;

using System.Collections.Concurrent;
using System.Data.Common;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using ViciOne.ServiceBus.EntityFrameworkCoreIntegration.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Testing;
using Xunit;

public sealed class InboxOutboxConcurrencyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-EF-INBOX-DEDUPLICATION", "concurrent-redeliveries-lock-and-produce-effects-once")]
    public async Task ConcurrentRedeliveries_EnterTheConsumerOnceAndCommitOneEffectSet()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        TimeSpan operationTimeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        await using PostgreSqlTestDatabase database = await PostgreSqlTestDatabase.CreateAsync(
            "inbox-outbox-concurrency",
            cancellationToken);
        var consumer = new InboxConsumerProbe();
        var effects = new InboxEffectProbe();
        var databaseProbe = new InboxDatabaseProbe();
        var services = new ServiceCollection();
        services.AddSingleton(consumer);
        services.AddSingleton(effects);
        services.AddDbContext<InboxOutboxDbContext>(builder => builder
            .UseNpgsql(database.ConnectionString, options => options.EnableRetryOnFailure())
            .AddInterceptors(databaseProbe));
        services.AddViciOneServiceBusTestHarness(TextWriter.Null, configuration =>
        {
            configuration.SetTestTimeouts(operationTimeout, operationTimeout);
            configuration.AddEntityFrameworkOutbox<InboxOutboxDbContext>(outbox =>
            {
                outbox.UsePostgres();
                outbox.DisableInboxCleanupService();
            });
            configuration.AddConsumer<InboxCommandConsumer, InboxCommandConsumerDefinition>();
            configuration.AddConsumer<InboxEffectConsumer>();
            configuration.UsingInMemory((context, bus) => bus.ConfigureEndpoints(context));
        });

        await using (var context = new InboxOutboxDbContext(
                         new DbContextOptionsBuilder<InboxOutboxDbContext>()
                             .UseNpgsql(database.ConnectionString)
                             .Options))
        {
            Assert.True(await context.Database.EnsureCreatedAsync(cancellationToken));
        }

        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(operationTimeout, cancellationToken);
        Guid messageId = Guid.NewGuid();

        try
        {
            await harness.Bus.Publish(new InboxCommand(messageId), context => context.MessageId = messageId, cancellationToken);
            await consumer.Entered.WaitAsync(operationTimeout, cancellationToken);
            Guid originalContext = Assert.Single(
                await databaseProbe.WaitForDistinctLockContextsAsync(1, operationTimeout, cancellationToken));
            databaseProbe.ArmForcedRetry(originalContext, operationTimeout);
            await Task.WhenAll(
                harness.Bus.Publish(new InboxCommand(messageId), context => context.MessageId = messageId, cancellationToken),
                harness.Bus.Publish(new InboxCommand(messageId), context => context.MessageId = messageId, cancellationToken),
                harness.Bus.Publish(new InboxCommand(messageId), context => context.MessageId = messageId, cancellationToken));

            Guid[] firstDuplicateContexts =
                await databaseProbe.WaitForDistinctLockContextsAsync(2, operationTimeout, cancellationToken);
            consumer.Release();
            Guid finalDuplicateContext = Assert.Single(
                await databaseProbe.WaitForDistinctLockContextsAsync(1, operationTimeout, cancellationToken));
            Guid[] duplicateContexts = [.. firstDuplicateContexts, finalDuplicateContext];
            Assert.Equal(3, duplicateContexts.Distinct().Count());
            Assert.DoesNotContain(originalContext, duplicateContexts);
            Assert.Equal(1, consumer.InvocationCount);

            InboxEffect[] delivered = await effects.ReadManyAsync(16, operationTimeout, cancellationToken);
            await databaseProbe.OutboxDrainCommitted.WaitAsync(operationTimeout, cancellationToken);
            Guid[] committedContexts = await databaseProbe.WaitForCommittedLockContextsAsync(4, operationTimeout, cancellationToken);

            Assert.Equal(4, committedContexts.Distinct().Count());
            Assert.Equal(1, databaseProbe.ForcedTransientFailureCount);
            Assert.Equal(2, databaseProbe.CompetingCommitsBeforeRetry);
            Assert.True(databaseProbe.RetryResumedAfterCompetingCommits);
            Assert.Equal(1, consumer.InvocationCount);
            Assert.Equal(Enumerable.Range(0, 16), delivered.Select(effect => effect.Index).Order());
            Assert.All(delivered, effect => Assert.Equal(messageId, effect.SourceMessageId));
            Assert.Equal(16, effects.RecordedCount);
            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            var verification = scope.ServiceProvider.GetRequiredService<InboxOutboxDbContext>();
            InboxState inbox = Assert.Single(await verification.Set<InboxState>()
                .AsNoTracking()
                .ToListAsync(cancellationToken));
            Assert.Equal(messageId, inbox.MessageId);
            Assert.Equal(4, inbox.ReceiveCount);
            Assert.NotNull(inbox.Consumed);
            Assert.NotNull(inbox.Delivered);
            Assert.Empty(await verification.Set<OutboxMessage>().AsNoTracking().ToListAsync(cancellationToken));
        }
        finally
        {
            consumer.Release();
            await harness.Stop(CancellationToken.None).WaitAsync(operationTimeout, CancellationToken.None);
        }
    }

    public sealed record InboxCommand(Guid CorrelationId);
    public sealed record InboxEffect(Guid SourceMessageId, int Index);

    public sealed class InboxCommandConsumer(InboxConsumerProbe probe) : IConsumer<InboxCommand>
    {
        public async Task Consume(ConsumeContext<InboxCommand> context)
        {
            await probe.EnterAsync(context.CancellationToken);
            await Task.WhenAll(Enumerable.Range(0, 16).Select(index =>
                context.Publish(
                    new InboxEffect(context.MessageId!.Value, index),
                    context.CancellationToken)));
        }
    }

    public sealed class InboxEffectConsumer(InboxEffectProbe effects) : IConsumer<InboxEffect>
    {
        public Task Consume(ConsumeContext<InboxEffect> context)
        {
            effects.Record(context.Message);
            return Task.CompletedTask;
        }
    }

    private sealed class InboxCommandConsumerDefinition : ConsumerDefinition<InboxCommandConsumer>
    {
        public InboxCommandConsumerDefinition()
        {
            ConcurrentMessageLimit = 4;
        }

        protected override void ConfigureConsumer(
            IReceiveEndpointConfigurator endpointConfigurator,
            IConsumerConfigurator<InboxCommandConsumer> consumerConfigurator,
            IRegistrationContext context)
        {
            endpointConfigurator.ConcurrentMessageLimit = 4;
            endpointConfigurator.UseEntityFrameworkOutbox<InboxOutboxDbContext>(
                context,
                options => options.MessageDeliveryLimit = 100);
        }
    }

    public sealed class InboxOutboxDbContext(DbContextOptions<InboxOutboxDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddTransactionalOutboxEntities();
        }
    }

    public sealed class InboxConsumerProbe
    {
        private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _invocationCount;

        public Task Entered => _entered.Task;
        public int InvocationCount => Volatile.Read(ref _invocationCount);

        public async Task EnterAsync(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _invocationCount);
            _entered.TrySetResult();
            await _release.Task.WaitAsync(cancellationToken);
        }

        public void Release() => _release.TrySetResult();
    }

    public sealed class InboxEffectProbe
    {
        private readonly Channel<InboxEffect> _effects = Channel.CreateUnbounded<InboxEffect>();
        private int _recordedCount;

        public int RecordedCount => Volatile.Read(ref _recordedCount);

        public void Record(InboxEffect effect)
        {
            if (!_effects.Writer.TryWrite(effect))
                throw new InvalidOperationException("The inbox-effect observation channel rejected an event.");

            Interlocked.Increment(ref _recordedCount);
        }

        public Task<InboxEffect[]> ReadManyAsync(int count, TimeSpan timeout, CancellationToken cancellationToken) =>
            ReadManyCoreAsync(count, cancellationToken).WaitAsync(timeout, cancellationToken);

        private async Task<InboxEffect[]> ReadManyCoreAsync(int count, CancellationToken cancellationToken)
        {
            var effects = new InboxEffect[count];
            for (var index = 0; index < effects.Length; index++)
                effects[index] = await _effects.Reader.ReadAsync(cancellationToken);

            return effects;
        }
    }

    private sealed class InboxDatabaseProbe : DbCommandInterceptor, IDbTransactionInterceptor
    {
        private readonly ConcurrentDictionary<Guid, byte> _committedLockContexts = new();
        private readonly Channel<Guid> _committedLocks = Channel.CreateUnbounded<Guid>();
        private readonly Channel<Guid> _firstLockByContext = Channel.CreateUnbounded<Guid>();
        private readonly ConcurrentDictionary<Guid, byte> _lockContexts = new();
        private readonly ConcurrentDictionary<Guid, byte> _pendingDrainContexts = new();
        private readonly ConcurrentQueue<string> _transactionFailures = new();
        private readonly ConcurrentDictionary<Guid, int> _transactionCommitsByContext = new();
        private readonly TaskCompletionSource _competingCommitsObserved = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _outboxDrainCommitted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private Guid _forcedRetryContext;
        private Guid _originalContext;
        private TimeSpan _forcedRetryTimeout;
        private int _competingCommitsBeforeRetry;
        private int _forcedRetryArmed;
        private int _forcedTransientFailureCount;
        private int _retryResumedAfterCompetingCommits;
        private int _transactionCommitCallbacks;

        public int CompetingCommitsBeforeRetry => Volatile.Read(ref _competingCommitsBeforeRetry);
        public int ForcedTransientFailureCount => Volatile.Read(ref _forcedTransientFailureCount);
        public Task OutboxDrainCommitted => _outboxDrainCommitted.Task;
        public bool RetryResumedAfterCompetingCommits => Volatile.Read(ref _retryResumedAfterCompetingCommits) == 1;

        public void ArmForcedRetry(Guid originalContext, TimeSpan timeout)
        {
            if (originalContext == Guid.Empty)
                throw new ArgumentException("The original inbox context identity must not be empty.", nameof(originalContext));
            if (timeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "The forced retry timeout must be positive.");

            _originalContext = originalContext;
            _forcedRetryTimeout = timeout;
            Volatile.Write(ref _forcedRetryArmed, 1);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            ObserveOutboxDrain(command, eventData);
            ThrowForcedRetryIfRequired(command, eventData);

            if (eventData.Context is { } context
                && command.CommandText.Contains("InboxState", StringComparison.Ordinal)
                && command.CommandText.Contains("FOR UPDATE", StringComparison.OrdinalIgnoreCase)
                && _lockContexts.TryAdd(context.ContextId.InstanceId, 0)
                && !_firstLockByContext.Writer.TryWrite(context.ContextId.InstanceId))
            {
                throw new InvalidOperationException("The inbox-lock observation channel rejected an event.");
            }

            return ValueTask.FromResult(result);
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            ObserveOutboxDrain(command, eventData);
            ThrowForcedRetryIfRequired(command, eventData);

            return ValueTask.FromResult(result);
        }

        public Task TransactionCommittedAsync(
            DbTransaction transaction,
            TransactionEndEventData eventData,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _transactionCommitCallbacks);

            if (eventData.Context is not { } context)
                return Task.CompletedTask;

            Guid contextId = context.ContextId.InstanceId;
            _transactionCommitsByContext.AddOrUpdate(contextId, 1, static (_, count) => count + 1);
            bool firstLockCommit = _lockContexts.ContainsKey(contextId) && _committedLockContexts.TryAdd(contextId, 0);
            if (firstLockCommit && !_committedLocks.Writer.TryWrite(contextId))
            {
                throw new InvalidOperationException("The committed inbox-lock observation channel rejected an event.");
            }

            if (firstLockCommit
                && Volatile.Read(ref _forcedTransientFailureCount) == 1
                && contextId != _originalContext
                && contextId != _forcedRetryContext
                && Interlocked.Increment(ref _competingCommitsBeforeRetry) == 2)
            {
                _competingCommitsObserved.TrySetResult();
            }

            if (_pendingDrainContexts.TryRemove(contextId, out _))
                _outboxDrainCommitted.TrySetResult();

            return Task.CompletedTask;
        }

        public async Task TransactionRolledBackAsync(
            DbTransaction transaction,
            TransactionEndEventData eventData,
            CancellationToken cancellationToken = default)
        {
            RecordTransactionFailure(eventData.Context, "rolled back");

            if (eventData.Context?.ContextId.InstanceId == _forcedRetryContext
                && Volatile.Read(ref _forcedTransientFailureCount) == 1)
            {
                await _competingCommitsObserved.Task.WaitAsync(_forcedRetryTimeout, cancellationToken);
                Volatile.Write(ref _retryResumedAfterCompetingCommits, 1);
            }
        }

        public Task TransactionFailedAsync(
            DbTransaction transaction,
            TransactionErrorEventData eventData,
            CancellationToken cancellationToken = default)
        {
            RecordTransactionFailure(eventData.Context, $"failed: {eventData.Exception.GetType().Name}: {eventData.Exception.Message}");
            return Task.CompletedTask;
        }

        public Task<Guid[]> WaitForDistinctLockContextsAsync(int count, TimeSpan timeout, CancellationToken cancellationToken) =>
            ReadManyAsync(_firstLockByContext.Reader, count, cancellationToken).WaitAsync(timeout, cancellationToken);

        public async Task<Guid[]> WaitForCommittedLockContextsAsync(int count, TimeSpan timeout, CancellationToken cancellationToken)
        {
            try
            {
                return await ReadManyAsync(_committedLocks.Reader, count, cancellationToken).WaitAsync(timeout, cancellationToken);
            }
            catch (TimeoutException exception)
            {
                throw new TimeoutException(
                    $"Expected {count} committed inbox-lock contexts, observed {_committedLockContexts.Count} from {_lockContexts.Count} lock contexts "
                    + $"and {Volatile.Read(ref _transactionCommitCallbacks)} transaction-commit callbacks. "
                    + $"Commits by context: {string.Join(", ", _transactionCommitsByContext.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key:N}={pair.Value}"))}. "
                    + $"Lock-transaction failures: {string.Join(" | ", _transactionFailures)}.",
                    exception);
            }
        }

        private void ObserveOutboxDrain(DbCommand command, CommandEventData eventData)
        {
            if (eventData.Context is { } context
                && command.CommandText.Contains("DELETE FROM \"OutboxMessage\"", StringComparison.Ordinal))
            {
                _pendingDrainContexts.TryAdd(context.ContextId.InstanceId, 0);
            }
        }

        private void ThrowForcedRetryIfRequired(DbCommand command, CommandEventData eventData)
        {
            if (eventData.Context is not { } context
                || !command.CommandText.Contains("InboxState", StringComparison.Ordinal)
                || !command.CommandText.Contains("UPDATE", StringComparison.OrdinalIgnoreCase)
                || Volatile.Read(ref _forcedRetryArmed) != 1
                || context.ContextId.InstanceId == _originalContext
                || Interlocked.CompareExchange(ref _forcedTransientFailureCount, 1, 0) != 0)
            {
                return;
            }

            _forcedRetryContext = context.ContextId.InstanceId;
            throw new PostgresException(
                "Test-owned serialization failure after the inbox entity entered the change tracker.",
                "ERROR",
                "ERROR",
                PostgresErrorCodes.SerializationFailure);
        }

        private void RecordTransactionFailure(DbContext? context, string outcome)
        {
            if (context is { } dbContext && _lockContexts.ContainsKey(dbContext.ContextId.InstanceId))
                _transactionFailures.Enqueue($"{dbContext.ContextId.InstanceId:N} {outcome}");
        }

        private static async Task<Guid[]> ReadManyAsync(ChannelReader<Guid> reader, int count, CancellationToken cancellationToken)
        {
            var contexts = new Guid[count];
            for (var index = 0; index < contexts.Length; index++)
                contexts[index] = await reader.ReadAsync(cancellationToken);

            return contexts;
        }
    }

}
