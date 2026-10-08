using System.Data;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Tests.Outbox;

public sealed class ClassicEfOutboxClockIsolationTests
{
    [Theory]
    [InlineData(Outcome.Success, ClockFault.None)]
    [InlineData(Outcome.Success, ClockFault.Timestamp)]
    [InlineData(Outcome.Success, ClockFault.Frequency)]
    [InlineData(Outcome.SaveFailure, ClockFault.None)]
    [InlineData(Outcome.SaveFailure, ClockFault.Timestamp)]
    [InlineData(Outcome.SaveFailure, ClockFault.Frequency)]
    [InlineData(Outcome.CommitFailure, ClockFault.None)]
    [InlineData(Outcome.CommitFailure, ClockFault.Timestamp)]
    [InlineData(Outcome.CommitFailure, ClockFault.Frequency)]
    [RequirementCoverage("REQ-VSB-EF-RECEIVE-OUTBOX", "optional-duration-failure-preserves-real-transaction-and-store-outcome")]
    public async Task DiagnosticClockPreservesTransactionAndRequiredFaultNotification(Outcome outcome, ClockFault fault)
    {
        await using Fixture fixture = await Fixture.CreateAsync(outcome, fault);
        long before = TimeProvider.System.GetTimestamp();
        Exception? failure = await Record.ExceptionAsync(() => fixture.ExecuteAsync()
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
        await fixture.AssertDatabaseStateAsync(outcome == Outcome.Success && failure is null);

        // An original timestamp failure happens before a transaction exists. Record that
        // physical boundary before the unconditional contract oracle below.
        if (ReferenceEquals(failure, fixture.Clock.Failure))
        {
            if (fixture.PipelineCalls == 0)
                Assert.Equal(0, fixture.Transactions.Started);
            else
                Assert.Equal(1, fixture.Transactions.RolledBack);
        }
        if (fixture.PipelineCalls != 0)
        {
            Assert.True(fixture.PipelineTimestamp > 0);
            Assert.Equal(outcome == Outcome.SaveFailure ? 1 : 0, fixture.SaveFailureCalls);
            Assert.Equal(outcome == Outcome.CommitFailure ? 1 : 0, fixture.Transactions.FailureCalls);
            if (outcome != Outcome.Success)
                Assert.True(fixture.FailureTimestamp >= fixture.PipelineTimestamp);
        }
        if (outcome == Outcome.Success)
            Assert.Null(failure);
        else
        {
            Assert.Same(fixture.StoreFailure, failure);
            Notification notification = Assert.Single(fixture.Context.Notifications);
            Assert.Same(fixture.StoreFailure, notification.Failure);
            Assert.Equal(TypeCache<Command>.ShortName, notification.ConsumerType);
            Assert.Equal(TestContext.Current.CancellationToken, notification.Token);
            if (fault == ClockFault.None)
                Assert.Equal(TimeSpan.FromSeconds(7), notification.Duration);
            else
            {
                // The fallback must measure this attempt, rather than inventing zero or
                // using receive age. Both bounds are actual ordered System timestamps.
                TimeSpan lower = TimeProvider.System.GetElapsedTime(fixture.PipelineTimestamp, fixture.FailureTimestamp);
                TimeSpan upper = TimeProvider.System.GetElapsedTime(before, notification.Timestamp);
                Assert.True(lower > TimeSpan.Zero);
                Assert.InRange(notification.Duration, lower, upper);
            }
        }
        Assert.Equal(1, fixture.PipelineCalls);
        Assert.Equal(1, fixture.Transactions.Started);
        Assert.Equal(outcome == Outcome.Success ? 1 : 0, fixture.Transactions.Committed);
        Assert.Equal(outcome == Outcome.Success ? 0 : 1, fixture.Transactions.RolledBack);
        if (outcome == Outcome.Success)
            Assert.Empty(fixture.Context.Notifications);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-RECEIVE-OUTBOX", "required-fault-notification-failure-still-rolls-back-and-propagates")]
    public async Task RequiredFaultNotificationFailureRemainsAuthoritative()
    {
        await using Fixture fixture = await Fixture.CreateAsync(Outcome.SaveFailure, ClockFault.None);
        var notificationFailure = new InvalidOperationException("chosen required notification failure");
        fixture.Context.NotificationTask = Task.FromException(notificationFailure);
        Exception? failure = await Record.ExceptionAsync(() => fixture.ExecuteAsync()
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
        await fixture.AssertDatabaseStateAsync(false);
        Assert.Equal(1, fixture.Transactions.RolledBack);
        Assert.Same(fixture.StoreFailure, Assert.Single(fixture.Context.Notifications).Failure);
        Assert.Same(notificationFailure, failure);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-RECEIVE-OUTBOX", "commit-fault-notification-is-awaited-before-original-failure-and-rollback")]
    public async Task CommitFaultNotificationIsActuallyAwaited()
    {
        await using Fixture fixture = await Fixture.CreateAsync(Outcome.CommitFailure, ClockFault.None);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Context.NotificationTask = release.Task;
        Task operation = fixture.ExecuteAsync();
        try
        {
            await Task.WhenAny(fixture.Context.Entered.Task, operation)
                .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            Assert.True(fixture.Context.Entered.Task.IsCompletedSuccessfully);
            Assert.False(operation.IsCompleted);
            Assert.Equal(0, fixture.Transactions.RolledBack);
            Assert.NotNull(fixture.Db.Database.CurrentTransaction);
        }
        finally
        {
            release.TrySetResult();
            await ObserveOwnedOperationAsync(operation);
        }
        Exception? failure = await Record.ExceptionAsync(() => operation);
        await fixture.AssertDatabaseStateAsync(false);
        Assert.Equal(1, fixture.Transactions.RolledBack);
        Assert.Same(fixture.StoreFailure, Assert.Single(fixture.Context.Notifications).Failure);
        Assert.Same(fixture.StoreFailure, failure);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-RECEIVE-OUTBOX", "required-consumption-utc-failure-remains-authoritative")]
    public async Task AuthoritativeUtcFailureStillRollsBack()
    {
        await using Fixture fixture = await Fixture.CreateAsync(Outcome.Success, ClockFault.None);
        fixture.Clock.FailUtc = true;
        Exception? failure = await Record.ExceptionAsync(() => fixture.ExecuteAsync()
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
        await fixture.AssertDatabaseStateAsync(false);
        Assert.Equal(1, fixture.Transactions.Started);
        Assert.Equal(1, fixture.PipelineCalls);
        Assert.Equal(1, fixture.Transactions.RolledBack);
        Assert.Empty(fixture.Context.Notifications);
        Assert.Same(fixture.Clock.Failure, failure);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-RECEIVE-OUTBOX", "precanceled-delivery-retains-original-token-without-transaction")]
    public async Task PrecanceledDeliveryDoesNotStartTransactionOrDiagnostics()
    {
        await using Fixture fixture = await Fixture.CreateAsync(Outcome.Success, ClockFault.Timestamp);
        using var delivery = new CancellationTokenSource();
        delivery.Cancel();
        var input = new RecordingContext(InMemoryOutboxTestContextFactory.Create(
            new Command(fixture.MessageId), delivery.Token, messageId: fixture.MessageId));
        Exception? failure = await Record.ExceptionAsync(() => fixture.ExecuteAsync(input)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
        await fixture.AssertDatabaseStateAsync(false);
        Assert.Equal(0, fixture.Transactions.Started);
        Assert.Equal(0, fixture.PipelineCalls);
        Assert.Equal(0, fixture.Clock.TimestampReads);
        Assert.Empty(input.Notifications);
        Assert.Equal(delivery.Token, Assert.IsAssignableFrom<OperationCanceledException>(failure).CancellationToken);
    }

    static async Task ObserveOwnedOperationAsync(Task task)
    {
        try { await task.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None); }
        catch (Exception exception) when (exception is not TimeoutException && task.IsCompleted) { }
    }

    public enum Outcome { Success, SaveFailure, CommitFailure }
    public enum ClockFault { None, Timestamp, Frequency }
    public sealed record Command(Guid Id);
    sealed record Notification(TimeSpan Duration, string ConsumerType, Exception Failure, CancellationToken Token, long Timestamp);

    sealed class RecordingContext(ConsumeContext<Command> inner) : ConsumeContextProxy<Command>(inner)
    {
        public List<Notification> Notifications { get; } = [];
        public Task NotificationTask { get; set; } = Task.CompletedTask;
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override Task NotifyFaultedAsync<T>(ConsumeContext<T> context, TimeSpan duration,
            string consumerType, Exception exception, CancellationToken cancellationToken = default)
        {
            Assert.Same(this, context);
            Notifications.Add(new(duration, consumerType, exception, cancellationToken, TimeProvider.System.GetTimestamp()));
            Entered.TrySetResult();
            return NotificationTask;
        }
    }

    sealed class Clock(ClockFault fault) : TimeProvider
    {
        readonly FakeTimeProvider _fake = new(new DateTimeOffset(2042, 3, 4, 5, 6, 7, TimeSpan.Zero));
        public Exception Failure { get; } = new InvalidOperationException("chosen optional clock failure");
        public bool FailUtc { get; set; }
        public int TimestampReads { get; private set; }
        public override long GetTimestamp()
        {
            TimestampReads++;
            if (fault == ClockFault.Timestamp) throw Failure;
            return _fake.GetTimestamp();
        }
        public override long TimestampFrequency => fault == ClockFault.Frequency ? throw Failure : _fake.TimestampFrequency;
        public override DateTimeOffset GetUtcNow() => FailUtc ? throw Failure : _fake.GetUtcNow();
        public void Advance() => _fake.Advance(TimeSpan.FromSeconds(7));
    }

    sealed class SaveFailureInterceptor : SaveChangesInterceptor
    {
        public Exception? Failure { get; set; }
        public Action? BeforeFailure { get; set; }
        public int FailureCalls { get; private set; }
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (Failure is not null)
            {
                FailureCalls++;
                BeforeFailure?.Invoke();
                return ValueTask.FromException<InterceptionResult<int>>(Failure);
            }
            return ValueTask.FromResult(result);
        }
    }

    sealed class TransactionObserver : DbTransactionInterceptor
    {
        public int Started { get; private set; }
        public int Committed { get; private set; }
        public int RolledBack { get; private set; }
        public Exception? Failure { get; set; }
        public Action? BeforeFailure { get; set; }
        public int FailureCalls { get; private set; }
        public override ValueTask<DbTransaction> TransactionStartedAsync(DbConnection connection,
            TransactionEndEventData eventData, DbTransaction result, CancellationToken cancellationToken = default)
        { Started++; return ValueTask.FromResult(result); }
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction,
            TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
        {
            if (Failure is not null)
            {
                FailureCalls++;
                BeforeFailure?.Invoke();
                return ValueTask.FromException<InterceptionResult>(Failure);
            }
            return ValueTask.FromResult(result);
        }
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData,
            CancellationToken cancellationToken = default)
        { Committed++; return Task.CompletedTask; }
        public override Task TransactionRolledBackAsync(DbTransaction transaction, TransactionEndEventData eventData,
            CancellationToken cancellationToken = default)
        { RolledBack++; return Task.CompletedTask; }
        public void Reset() { Started = 0; Committed = 0; RolledBack = 0; }
    }

    sealed class ClockDbContext(DbContextOptions<ClockDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddInboxStateEntity();
            modelBuilder.AddOutboxStateEntity();
            modelBuilder.AddOutboxMessageEntity();
        }
    }

    sealed class Fixture : IAsyncDisposable
    {
        readonly SqliteConnection _connection;
        readonly ServiceProvider _provider;
        readonly SaveFailureInterceptor _save;
        readonly Outcome _outcome;
        readonly EntityFrameworkOutboxContextFactory<IBus, ClockDbContext> _factory;
        readonly Guid _consumerId = Guid.NewGuid();
        public Guid MessageId { get; } = Guid.NewGuid();
        public ClockDbContext Db { get; }
        public Clock Clock { get; }
        public TransactionObserver Transactions { get; }
        public RecordingContext Context { get; }
        public Exception StoreFailure { get; } = new InvalidOperationException("chosen database operation failure");
        public int SaveFailureCalls => _save.FailureCalls;
        public int PipelineCalls { get; private set; }
        public long PipelineTimestamp { get; private set; }
        public long FailureTimestamp { get; private set; }

        Fixture(SqliteConnection connection, ServiceProvider provider, ClockDbContext db,
            SaveFailureInterceptor save, TransactionObserver transactions, Outcome outcome, Clock clock)
        {
            _connection = connection;
            _outcome = outcome;
            _provider = provider;
            Db = db;
            Clock = clock;
            _save = save;
            Transactions = transactions;
            Context = new(InMemoryOutboxTestContextFactory.Create(new Command(MessageId),
                TestContext.Current.CancellationToken, messageId: MessageId));
            _factory = new(Db, _provider, Options.Create(new EntityFrameworkOutboxOptions<ClockDbContext>
            { IsolationLevel = IsolationLevel.Serializable, LockStatementProvider = new SqliteLockStatementProvider() }), Clock);
            _save.BeforeFailure = Transactions.BeforeFailure = () => FailureTimestamp = TimeProvider.System.GetTimestamp();
        }

        public static async Task<Fixture> CreateAsync(Outcome outcome, ClockFault fault)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            ServiceProvider? provider = null;
            ClockDbContext? db = null;
            try
            {
                await connection.OpenAsync(TestContext.Current.CancellationToken);
                provider = new ServiceCollection().BuildServiceProvider();
                var clock = new Clock(fault);
                var save = new SaveFailureInterceptor();
                var transactions = new TransactionObserver();
                db = new ClockDbContext(new DbContextOptionsBuilder<ClockDbContext>().UseSqlite(connection)
                    .AddInterceptors(save, transactions).Options);
                var fixture = new Fixture(connection, provider, db, save, transactions, outcome, clock);
                await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
                db.Add(new InboxState
                {
                    MessageId = fixture.MessageId, ConsumerId = fixture._consumerId, LockId = Guid.NewGuid(),
                    Received = clock.GetUtcNow(), ReceiveCount = 1,
                });
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
                db.ChangeTracker.Clear();
                transactions.Reset();
                // Successful return transfers all three owners to this fixture.
                return fixture;
            }
            catch (Exception creationFailure)
            {
                try { await ReleaseOwnersAsync(db, provider, connection); }
                catch (Exception cleanupFailure)
                { throw new AggregateException("EF clock fixture creation and cleanup failed.", creationFailure, cleanupFailure); }
                throw;
            }
        }

        public Task ExecuteAsync(RecordingContext? context = null) => _factory.SendAsync(context ?? Context,
            new OutboxConsumeOptions { ConsumerId = _consumerId, ConsumerType = nameof(Command),
                MessageDeliveryLimit = 10, MessageDeliveryTimeout = TimeSpan.FromMinutes(1) },
            Pipe.ExecuteAwaited<OutboxConsumeContext<Command>>(async input =>
            {
                PipelineCalls++;
                PipelineTimestamp = TimeProvider.System.GetTimestamp();
                Clock.Advance();
                await input.SetConsumedAsync(TestContext.Current.CancellationToken);
                input.ContinueProcessing = false;
                if (_outcome == Outcome.SaveFailure) _save.Failure = StoreFailure;
                if (_outcome == Outcome.CommitFailure) Transactions.Failure = StoreFailure;
            }), TestContext.Current.CancellationToken);

        public async Task AssertDatabaseStateAsync(bool consumed)
        {
            Assert.Null(Db.Database.CurrentTransaction);
            Db.ChangeTracker.Clear();
            InboxState state = await Db.Set<InboxState>().AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
            Assert.Equal(MessageId, state.MessageId);
            Assert.Equal(_consumerId, state.ConsumerId);
            Assert.Equal(consumed ? 2 : 1, state.ReceiveCount);
            Assert.Null(state.Delivered);
            Assert.Null(state.LastSequenceNumber);
            if (consumed) Assert.Equal(Clock.GetUtcNow(), state.Consumed);
            else Assert.Null(state.Consumed);
            Assert.Empty(await Db.Set<OutboxMessage>().AsNoTracking().ToArrayAsync(TestContext.Current.CancellationToken));
        }

        public async ValueTask DisposeAsync()
        {
            try { await ObserveOwnedOperationAsync(Context.NotificationTask); }
            finally { await ReleaseOwnersAsync(Db, _provider, _connection); }
        }

        static async Task ReleaseOwnersAsync(ClockDbContext? db, ServiceProvider? provider, SqliteConnection connection)
        {
            var failures = new List<Exception>();
            try
            {
                if (db is not null)
                {
                    try { await db.DisposeAsync(); }
                    catch (Exception failure) { failures.Add(failure); }
                }
            }
            finally
            {
                try
                {
                    if (provider is not null)
                    {
                        try { await provider.DisposeAsync(); }
                        catch (Exception failure) { failures.Add(failure); }
                    }
                }
                finally
                {
                    try { await connection.DisposeAsync(); }
                    catch (Exception failure) { failures.Add(failure); }
                }
            }
            if (failures.Count == 1)
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failures[0]).Throw();
            if (failures.Count > 1)
                throw new AggregateException("EF clock fixture owner cleanup failed.", failures);
        }
    }
}
