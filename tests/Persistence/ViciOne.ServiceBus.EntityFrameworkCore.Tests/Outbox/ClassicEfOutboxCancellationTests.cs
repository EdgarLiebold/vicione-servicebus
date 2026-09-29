using System.Data;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.Outbox;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Tests.Outbox;

public sealed class ClassicEfOutboxCancellationTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-EF-RECEIVE-OUTBOX", "in-flight-query-or-delete-reports-delivery-cancellation-source")]
    public async Task LoadOrRemove_CanceledDuringDatabaseCommandReportsDeliveryTokenAndRetainsMessageAsync(bool remove, bool cancelDelivery)
    {
        CancellationToken testToken = TestContext.Current.CancellationToken;
        var commandGate = new GatedCommandInterceptor();
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(testToken);
        var dbOptions = new DbContextOptionsBuilder<ClassicOutboxDbContext>()
            .UseSqlite(connection).AddInterceptors(commandGate).Options;
        await using var db = new ClassicOutboxDbContext(dbOptions);
        await db.Database.EnsureCreatedAsync(testToken);
        Guid messageId = Guid.NewGuid();
        Guid consumerId = Guid.NewGuid();
        var inbox = new InboxState
        {
            MessageId = messageId,
            ConsumerId = consumerId,
            LockId = Guid.NewGuid(),
            Received = DateTimeOffset.UtcNow,
            ReceiveCount = 1,
        };
        var outgoing = new OutboxMessage
        {
            InboxMessageId = messageId,
            InboxConsumerId = consumerId,
            MessageId = Guid.NewGuid(),
            ContentType = "application/json",
            MessageType = "urn:message:tests:Command",
            Body = "{}",
        };
        db.AddRange(inbox, outgoing);
        await db.SaveChangesAsync(testToken);
        await using var transaction = await db.Database.BeginTransactionAsync(testToken);
        using var delivery = new CancellationTokenSource();
        using var operation = new CancellationTokenSource();
        using ServiceProvider provider = new ServiceCollection().BuildServiceProvider();
        ConsumeContext<Command> input = InMemoryOutboxTestContextFactory.Create(
            new Command(messageId), delivery.Token, messageId: messageId);
        var options = new OutboxConsumeOptions
        {
            ConsumerId = consumerId,
            ConsumerType = nameof(Command),
            MessageDeliveryLimit = 10,
            MessageDeliveryTimeout = TimeSpan.FromMinutes(1),
        };
        using var context = new DbContextOutboxConsumeContext<IBus, ClassicOutboxDbContext, Command>(
            input, options, provider, db, transaction, inbox, TimeProvider.System);
        commandGate.Enable();

        Task pending = remove
            ? context.RemoveOutboxMessagesAsync(operation.Token)
            : context.LoadOutboxMessagesAsync(operation.Token);
        try
        {
            await commandGate.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10), testToken);
            Assert.False(pending.IsCompleted);
            if (cancelDelivery)
                delivery.Cancel();
            else
                operation.Cancel();
        }
        finally
        {
            commandGate.Release();
        }
        OperationCanceledException failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => pending.WaitAsync(TimeSpan.FromSeconds(10), testToken));
        Assert.Equal(cancelDelivery ? delivery.Token : operation.Token, failure.CancellationToken);
        Assert.Equal(cancelDelivery, delivery.IsCancellationRequested);
        Assert.Equal(!cancelDelivery, operation.IsCancellationRequested);
        await transaction.RollbackAsync(CancellationToken.None);
        db.ChangeTracker.Clear();
        Assert.Equal(outgoing.MessageId, (await db.Set<OutboxMessage>().AsNoTracking().SingleAsync(testToken)).MessageId);
        InboxState persisted = await db.Set<InboxState>().AsNoTracking().SingleAsync(testToken);
        Assert.Null(persisted.Consumed);
        Assert.Null(persisted.Delivered);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-EF-RECEIVE-OUTBOX", "in-flight-save-reports-delivery-cancellation-source")]
    public async Task SetConsumed_CanceledDuringSaveReportsDeliveryTokenAndLeavesFenceUnchangedAsync(bool cancelDelivery)
    {
        CancellationToken testToken = TestContext.Current.CancellationToken;
        var saveGate = new GatedSaveInterceptor();
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(testToken);
        var dbOptions = new DbContextOptionsBuilder<ClassicOutboxDbContext>()
            .UseSqlite(connection).AddInterceptors(saveGate).Options;
        await using var db = new ClassicOutboxDbContext(dbOptions);
        await db.Database.EnsureCreatedAsync(testToken);
        Guid messageId = Guid.NewGuid();
        Guid consumerId = Guid.NewGuid();
        var inbox = new InboxState
        {
            MessageId = messageId,
            ConsumerId = consumerId,
            LockId = Guid.NewGuid(),
            Received = DateTimeOffset.UtcNow,
            ReceiveCount = 1,
        };
        db.Add(inbox);
        await db.SaveChangesAsync(testToken);
        await using var transaction = await db.Database.BeginTransactionAsync(testToken);
        using var delivery = new CancellationTokenSource();
        using var operation = new CancellationTokenSource();
        using ServiceProvider provider = new ServiceCollection().BuildServiceProvider();
        ConsumeContext<Command> input = InMemoryOutboxTestContextFactory.Create(
            new Command(messageId), delivery.Token, messageId: messageId);
        var options = new OutboxConsumeOptions
        {
            ConsumerId = consumerId,
            ConsumerType = nameof(Command),
            MessageDeliveryLimit = 10,
            MessageDeliveryTimeout = TimeSpan.FromMinutes(1),
        };
        using var context = new DbContextOutboxConsumeContext<IBus, ClassicOutboxDbContext, Command>(
            input, options, provider, db, transaction, inbox, TimeProvider.System);
        saveGate.Enable();

        Task pending = context.SetConsumedAsync(operation.Token);
        try
        {
            await saveGate.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10), testToken);
            Assert.False(pending.IsCompleted);
            if (cancelDelivery)
                delivery.Cancel();
            else
                operation.Cancel();
        }
        finally
        {
            saveGate.Release();
        }
        OperationCanceledException failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => pending.WaitAsync(TimeSpan.FromSeconds(10), testToken));
        Assert.Equal(cancelDelivery ? delivery.Token : operation.Token, failure.CancellationToken);
        Assert.Equal(cancelDelivery, delivery.IsCancellationRequested);
        Assert.Equal(!cancelDelivery, operation.IsCancellationRequested);
        await transaction.RollbackAsync(CancellationToken.None);
        db.ChangeTracker.Clear();
        InboxState persisted = await db.Set<InboxState>().AsNoTracking().SingleAsync(testToken);
        Assert.Null(persisted.Consumed);
        Assert.Null(persisted.Delivered);
        Assert.Equal(1, persisted.ReceiveCount);
    }

    [Theory]
    [InlineData("consume")]
    [InlineData("deliver")]
    [InlineData("load")]
    [InlineData("checkpoint")]
    [InlineData("remove")]
    [InlineData("capture")]
    [RequirementCoverage("REQ-VSB-EF-RECEIVE-OUTBOX", "canceled-delivery-rejects-distinct-active-state-operation")]
    public async Task CanceledDelivery_RejectsEveryStateOperationWithoutMutatingTheDatabaseAsync(string operation)
    {
        CancellationToken testToken = TestContext.Current.CancellationToken;
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(testToken);
        var dbOptions = new DbContextOptionsBuilder<ClassicOutboxDbContext>()
            .UseSqlite(connection).Options;
        await using var db = new ClassicOutboxDbContext(dbOptions);
        await db.Database.EnsureCreatedAsync(testToken);
        Guid messageId = Guid.NewGuid();
        Guid consumerId = Guid.NewGuid();
        var inbox = new InboxState
        {
            MessageId = messageId,
            ConsumerId = consumerId,
            LockId = Guid.NewGuid(),
            Received = DateTimeOffset.UtcNow,
            ReceiveCount = 1,
        };
        var outgoing = new OutboxMessage
        {
            InboxMessageId = messageId,
            InboxConsumerId = consumerId,
            MessageId = Guid.NewGuid(),
            ContentType = "application/json",
            MessageType = "urn:message:tests:Command",
            Body = "{}",
        };
        db.AddRange(inbox, outgoing);
        await db.SaveChangesAsync(testToken);
        long sequence = outgoing.SequenceNumber;
        await using var transaction = await db.Database.BeginTransactionAsync(testToken);
        using var delivery = new CancellationTokenSource();
        using var explicitOperation = new CancellationTokenSource();
        using ServiceProvider provider = new ServiceCollection().BuildServiceProvider();
        ConsumeContext<Command> input = InMemoryOutboxTestContextFactory.Create(
            new Command(messageId), delivery.Token, messageId: messageId);
        var options = new OutboxConsumeOptions
        {
            ConsumerId = consumerId,
            ConsumerType = nameof(Command),
            MessageDeliveryLimit = 10,
            MessageDeliveryTimeout = TimeSpan.FromMinutes(1),
        };
        using var context = new DbContextOutboxConsumeContext<IBus, ClassicOutboxDbContext, Command>(
            input, options, provider, db, transaction, inbox, TimeProvider.System);
        var captured = new MessageSendContext<Command>(new Command(Guid.NewGuid()), explicitOperation.Token)
        {
            MessageId = Guid.NewGuid(),
        };
        delivery.Cancel();

        Func<Task> action = operation switch
        {
            "consume" => () => context.SetConsumedAsync(explicitOperation.Token),
            "deliver" => () => context.SetDeliveredAsync(explicitOperation.Token),
            "load" => () => context.LoadOutboxMessagesAsync(explicitOperation.Token),
            "checkpoint" => () => context.NotifyOutboxMessageDeliveredAsync(outgoing, explicitOperation.Token),
            "remove" => () => context.RemoveOutboxMessagesAsync(explicitOperation.Token),
            "capture" => () => context.AddSendAsync(captured, explicitOperation.Token),
            _ => throw new ArgumentOutOfRangeException(nameof(operation)),
        };
        OperationCanceledException failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(action);
        Assert.Equal(delivery.Token, failure.CancellationToken);
        Assert.False(explicitOperation.IsCancellationRequested);
        Assert.Null(inbox.Consumed);
        Assert.Null(inbox.Delivered);
        Assert.Null(inbox.LastSequenceNumber);
        db.ChangeTracker.Clear();
        Assert.Equal(sequence, (await db.Set<OutboxMessage>().AsNoTracking().SingleAsync(testToken)).SequenceNumber);
        InboxState persisted = await db.Set<InboxState>().AsNoTracking().SingleAsync(testToken);
        Assert.Null(persisted.Consumed);
        Assert.Null(persisted.Delivered);
        Assert.Null(persisted.LastSequenceNumber);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-EF-RECEIVE-OUTBOX", "distinct-delivery-cancellation-rolls-back-consumed-fence")]
    public async Task Factory_DistinctDeliveryCancellationRollsBackAndHealthyRetryConsumesAsync(bool cancelAfterCompletion)
    {
        CancellationToken testToken = TestContext.Current.CancellationToken;
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(testToken);
        var dbOptions = new DbContextOptionsBuilder<ClassicOutboxDbContext>()
            .UseSqlite(connection).Options;
        await using var db = new ClassicOutboxDbContext(dbOptions);
        await db.Database.EnsureCreatedAsync(testToken);
        using ServiceProvider provider = new ServiceCollection().BuildServiceProvider();
        var factory = new EntityFrameworkOutboxContextFactory<IBus, ClassicOutboxDbContext>(
            db, provider, Options.Create(new EntityFrameworkOutboxOptions<ClassicOutboxDbContext>
            {
                IsolationLevel = IsolationLevel.Serializable,
                LockStatementProvider = new SqliteLockStatementProvider(),
            }), TimeProvider.System);
        Guid messageId = Guid.NewGuid();
        Guid consumerId = Guid.NewGuid();
        var options = new OutboxConsumeOptions
        {
            ConsumerId = consumerId,
            ConsumerType = nameof(Command),
            MessageDeliveryLimit = 10,
            MessageDeliveryTimeout = TimeSpan.FromMinutes(1),
        };
        using var delivery = new CancellationTokenSource();
        using var operation = new CancellationTokenSource();
        ConsumeContext<Command> input = InMemoryOutboxTestContextFactory.Create(
            new Command(messageId), delivery.Token, messageId: messageId);
        int canceledExecutions = 0;

        OperationCanceledException failure = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            factory.SendAsync(input, options, Pipe.ExecuteAwaited<OutboxConsumeContext<Command>>(async context =>
            {
                canceledExecutions++;
                if (!cancelAfterCompletion)
                    delivery.Cancel();
                await context.SetConsumedAsync(operation.Token);
                if (cancelAfterCompletion)
                    delivery.Cancel();
                context.ContinueProcessing = false;
            }), operation.Token));
        Assert.Equal(delivery.Token, failure.CancellationToken);
        Assert.Equal(1, canceledExecutions);
        Assert.False(operation.IsCancellationRequested);

        db.ChangeTracker.Clear();
        InboxState retained = await db.Set<InboxState>().AsNoTracking().SingleAsync(testToken);
        Assert.Equal(messageId, retained.MessageId);
        Assert.Equal(consumerId, retained.ConsumerId);
        Assert.Equal(1, retained.ReceiveCount);
        Assert.Null(retained.Consumed);
        Assert.Null(retained.Delivered);
        Assert.Empty(await db.Set<OutboxMessage>().AsNoTracking().ToArrayAsync(testToken));

        ConsumeContext<Command> healthyInput = InMemoryOutboxTestContextFactory.Create(
            new Command(messageId), testToken, messageId: messageId);
        int healthyExecutions = 0;
        await factory.SendAsync(healthyInput, options, Pipe.ExecuteAwaited<OutboxConsumeContext<Command>>(async context =>
        {
            healthyExecutions++;
            await context.SetConsumedAsync(testToken);
            context.ContinueProcessing = false;
        }), testToken);
        Assert.Equal(1, healthyExecutions);
        db.ChangeTracker.Clear();
        InboxState consumed = await db.Set<InboxState>().AsNoTracking().SingleAsync(testToken);
        Assert.Equal(2, consumed.ReceiveCount);
        Assert.NotNull(consumed.Consumed);
        Assert.Null(consumed.Delivered);
        Assert.Empty(await db.Set<OutboxMessage>().AsNoTracking().ToArrayAsync(testToken));
    }

    public sealed record Command(Guid Id);

    public sealed class ClassicOutboxDbContext(DbContextOptions<ClassicOutboxDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddInboxStateEntity();
            modelBuilder.AddOutboxStateEntity();
            modelBuilder.AddOutboxMessageEntity();
        }
    }

    private sealed class GatedSaveInterceptor : SaveChangesInterceptor
    {
        bool _enabled;
        readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Enable() => _enabled = true;

        public void Release() => _release.TrySetResult();

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (_enabled)
            {
                Entered.TrySetResult();
                await _release.Task.ConfigureAwait(false);
            }
            return result;
        }
    }

    private sealed class GatedCommandInterceptor : DbCommandInterceptor
    {
        bool _enabled;
        readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Enable() => _enabled = true;

        public void Release() => _release.TrySetResult();

        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            await WaitAsync(cancellationToken).ConfigureAwait(false);
            return result;
        }

        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            await WaitAsync(cancellationToken).ConfigureAwait(false);
            return result;
        }

        async Task WaitAsync(CancellationToken cancellationToken)
        {
            if (!_enabled)
                return;
            Entered.TrySetResult();
            await _release.Task.ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
