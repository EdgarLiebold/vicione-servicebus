using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.Outbox;
using ViciOne.ServiceBus.Operations;
using ViciOne.ServiceBus.Providers.Persistence;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Tests.Outbox;

public sealed class EntityFrameworkTransactionalScopedBusContextTests
{
    private static readonly DateTimeOffset Now = new(2044, 5, 6, 7, 8, 9, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "constructor-rejects-every-missing-dependency")]
    public async Task Constructor_RejectsEveryMissingDependencyAsync()
    {
        await using ClassicOutboxFixture fixture = await ClassicOutboxFixture.CreateAsync();

        Assert.Throws<ArgumentNullException>(() => new EntityFrameworkTransactionalScopedBusContext<IBus, ClassicOutboxDbContext>(
            null!, fixture.DbContext, fixture.Notification, fixture.ClientFactory, fixture.Services,
            fixture.TimeProvider, fixture.PersistenceIdentity));
        Assert.Throws<ArgumentNullException>(() => new EntityFrameworkTransactionalScopedBusContext<IBus, ClassicOutboxDbContext>(
            fixture.Bus, null!, fixture.Notification, fixture.ClientFactory, fixture.Services,
            fixture.TimeProvider, fixture.PersistenceIdentity));
        Assert.Throws<ArgumentNullException>(() => new EntityFrameworkTransactionalScopedBusContext<IBus, ClassicOutboxDbContext>(
            fixture.Bus, fixture.DbContext, null!, fixture.ClientFactory, fixture.Services,
            fixture.TimeProvider, fixture.PersistenceIdentity));
        Assert.Throws<ArgumentNullException>(() => new EntityFrameworkTransactionalScopedBusContext<IBus, ClassicOutboxDbContext>(
            fixture.Bus, fixture.DbContext, fixture.Notification, null!, fixture.Services,
            fixture.TimeProvider, fixture.PersistenceIdentity));
        Assert.Throws<ArgumentNullException>(() => new EntityFrameworkTransactionalScopedBusContext<IBus, ClassicOutboxDbContext>(
            fixture.Bus, fixture.DbContext, fixture.Notification, fixture.ClientFactory, null!,
            fixture.TimeProvider, fixture.PersistenceIdentity));
        Assert.Throws<ArgumentNullException>(() => new EntityFrameworkTransactionalScopedBusContext<IBus, ClassicOutboxDbContext>(
            fixture.Bus, fixture.DbContext, fixture.Notification, fixture.ClientFactory, fixture.Services,
            null!, fixture.PersistenceIdentity));
        Assert.Throws<ArgumentNullException>(() => new EntityFrameworkTransactionalScopedBusContext<IBus, ClassicOutboxDbContext>(
            fixture.Bus, fixture.DbContext, fixture.Notification, fixture.ClientFactory, fixture.Services,
            fixture.TimeProvider, null!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "lazy-scoped-endpoints-and-service-provider")]
    public async Task ScopedEndpoints_AreStableLazyViewsOverTheBoundBusAsync()
    {
        await using ClassicOutboxFixture fixture = await ClassicOutboxFixture.CreateAsync();
        using EntityFrameworkTransactionalScopedBusContext<IBus, ClassicOutboxDbContext> context = fixture.CreateContext();

        Assert.Same(context.SendEndpointProvider, context.SendEndpointProvider);
        Assert.Same(context.PublishEndpoint, context.PublishEndpoint);
        Assert.Same(context.ClientFactory, context.ClientFactory);
        Assert.Same(fixture.Marker, context.GetService(typeof(ServiceMarker)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "commit-persists-business-state-and-ordered-intent")]
    public async Task Commit_PersistsBusinessStateAndOrderedOutboxIntentAsync()
    {
        await using ClassicOutboxFixture fixture = await ClassicOutboxFixture.CreateAsync();
        using EntityFrameworkTransactionalScopedBusContext<IBus, ClassicOutboxDbContext> context = fixture.CreateContext();
        var business = new BusinessRecord(Guid.NewGuid(), "committed");
        Guid messageId = Guid.NewGuid();
        fixture.DbContext.Add(business);
        await context.AddSendAsync(CreateSendContext(messageId, 1), TestContext.Current.CancellationToken);

        await context.CommitAsync(TestContext.Current.CancellationToken);
        fixture.DbContext.ChangeTracker.Clear();

        Assert.Equal(business, await fixture.DbContext.Set<BusinessRecord>().SingleAsync(TestContext.Current.CancellationToken));
        OutboxState state = await fixture.DbContext.Set<OutboxState>().SingleAsync(TestContext.Current.CancellationToken);
        OutboxMessage message = await fixture.DbContext.Set<OutboxMessage>().SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("classic", state.BusKey);
        Assert.Equal(OutboxDeliveryStatus.Pending, state.Status);
        Assert.Equal(Now, state.Created);
        Assert.Equal(state.OutboxId, message.OutboxId);
        Assert.Equal(messageId, message.MessageId);
        Assert.Equal(1, fixture.Notification.DeliveredCount);
        Assert.False(context.HasActiveSession);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-METADATA", "admitted-proof-persists-replays-and-binds-body")]
    public async Task AdmittedOutbox_PersistsProofReplaysAndRejectsBodyTamperingAsync()
    {
        await using ClassicOutboxFixture fixture = await ClassicOutboxFixture.CreateAsync();
        using EntityFrameworkTransactionalScopedBusContext<IBus, ClassicOutboxDbContext> context = fixture.CreateContext();
        Guid messageId = Guid.Parse("39e984da-2ce2-454a-b3d9-f673a7a4e30b");
        MessageSendContext<OutboxProbe> original = CreateSendContext(messageId, 7);
        original.Headers.Set("tenant", "north");

        await context.AddSendAsync(original, TestContext.Current.CancellationToken);
        await context.CommitAsync(TestContext.Current.CancellationToken);
        fixture.DbContext.ChangeTracker.Clear();
        OutboxMessage stored = await fixture.DbContext.Set<OutboxMessage>()
            .AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(stored.Headers);
        Assert.StartsWith("VOSB-EF-OUTBOX-ADMISSION/1:", stored.Headers, StringComparison.Ordinal);

        stored.Deserialize(ServiceBusMetadataJson.ObjectDeserializer);
        ISerialization serialization = new SerializationConfiguration().CreateSerializerCollection();
        var replay = new MessageSendContext<SerializedTransportMessage>(SerializedTransportMessage.Instance)
        {
            Serialization = serialization,
        };

        await new OutboxMessageSendPipe(stored, stored.DestinationAddress).SendAsync(replay);

        Assert.Equal(messageId, replay.MessageId);
        Assert.Equal("north", replay.Headers.Get<string>("tenant"));
        Assert.Equal(Encoding.UTF8.GetBytes(stored.Body), replay.Serializer.GetMessageBody(replay).ToArray());

        stored.Body += " ";
        var tamperedReplay = new MessageSendContext<SerializedTransportMessage>(SerializedTransportMessage.Instance)
        {
            Serialization = serialization,
        };

        SerializationException failure = await Assert.ThrowsAsync<SerializationException>(
            () => new OutboxMessageSendPipe(stored, stored.DestinationAddress).SendAsync(tamperedReplay));
        Assert.Contains("does not match its payload admission proof", failure.Message, StringComparison.Ordinal);
        Assert.Empty(tamperedReplay.Headers.GetAll());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "commit-without-intent-persists-business-only")]
    public async Task Commit_WithoutIntentPersistsBusinessChangesOnlyAsync()
    {
        await using ClassicOutboxFixture fixture = await ClassicOutboxFixture.CreateAsync();
        using EntityFrameworkTransactionalScopedBusContext<IBus, ClassicOutboxDbContext> context = fixture.CreateContext();
        var business = new BusinessRecord(Guid.NewGuid(), "business-only");
        fixture.DbContext.Add(business);

        await context.CommitAsync(TestContext.Current.CancellationToken);

        Assert.Equal(business, await fixture.DbContext.Set<BusinessRecord>().SingleAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await fixture.DbContext.Set<OutboxState>().ToListAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await fixture.DbContext.Set<OutboxMessage>().ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0, fixture.Notification.DeliveredCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "abort-detaches-only-classic-session-intent")]
    public async Task Abort_DetachesOnlySessionIntentAndRetainsBusinessChangesAsync()
    {
        await using ClassicOutboxFixture fixture = await ClassicOutboxFixture.CreateAsync();
        using EntityFrameworkTransactionalScopedBusContext<IBus, ClassicOutboxDbContext> context = fixture.CreateContext();
        var business = new BusinessRecord(Guid.NewGuid(), "retained");
        fixture.DbContext.Add(business);
        await context.AddSendAsync(CreateSendContext(Guid.NewGuid(), 1), TestContext.Current.CancellationToken);

        await context.AbortAsync(TestContext.Current.CancellationToken);
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        fixture.DbContext.ChangeTracker.Clear();

        Assert.Equal(business, await fixture.DbContext.Set<BusinessRecord>().SingleAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await fixture.DbContext.Set<OutboxState>().ToListAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await fixture.DbContext.Set<OutboxMessage>().ToListAsync(TestContext.Current.CancellationToken));
        Assert.False(context.HasActiveSession);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "dispose-without-classic-commit-fails-and-detaches")]
    public async Task Dispose_WithoutCommitFailsAfterDetachingOnlySessionIntentAsync()
    {
        await using ClassicOutboxFixture fixture = await ClassicOutboxFixture.CreateAsync();
        EntityFrameworkTransactionalScopedBusContext<IBus, ClassicOutboxDbContext> context = fixture.CreateContext();
        var business = new BusinessRecord(Guid.NewGuid(), "survives");
        fixture.DbContext.Add(business);
        await context.AddSendAsync(CreateSendContext(Guid.NewGuid(), 1), TestContext.Current.CancellationToken);

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(context.Dispose);
        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        fixture.DbContext.ChangeTracker.Clear();

        Assert.Contains("disposed without commit", failure.Message, StringComparison.Ordinal);
        Assert.Equal(business, await fixture.DbContext.Set<BusinessRecord>().SingleAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await fixture.DbContext.Set<OutboxState>().ToListAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await fixture.DbContext.Set<OutboxMessage>().ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "external-save-completes-once-and-allows-disposal-order")]
    public async Task ExternalSave_CompletesOnceAndAllowsDbContextFirstDisposalAsync()
    {
        await using ClassicOutboxFixture fixture = await ClassicOutboxFixture.CreateAsync();
        EntityFrameworkTransactionalScopedBusContext<IBus, ClassicOutboxDbContext> context = fixture.CreateContext();
        await context.AddSendAsync(CreateSendContext(Guid.NewGuid(), 1), TestContext.Current.CancellationToken);

        await fixture.DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, fixture.Notification.DeliveredCount);
        Assert.False(context.HasActiveSession);
        await context.CommitAsync(TestContext.Current.CancellationToken);
        await context.CommitAsync(TestContext.Current.CancellationToken);
        await fixture.DbContext.DisposeAsync();
        Exception? disposalFailure = Record.Exception(context.Dispose);

        Assert.Equal(1, fixture.Notification.DeliveredCount);
        Assert.Null(disposalFailure);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "concurrent-classic-writes-share-one-state")]
    public async Task ConcurrentWrites_ShareOneStateAndRetainEveryMessageAsync()
    {
        await using ClassicOutboxFixture fixture = await ClassicOutboxFixture.CreateAsync();
        using EntityFrameworkTransactionalScopedBusContext<IBus, ClassicOutboxDbContext> context = fixture.CreateContext();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Guid[] messageIds = Enumerable.Range(0, 32).Select(_ => Guid.NewGuid()).ToArray();
        Task[] writes = messageIds.Select(async (messageId, index) =>
        {
            await release.Task;
            await context.AddSendAsync(CreateSendContext(messageId, index));
        }).ToArray();

        release.SetResult();
        await Task.WhenAll(writes);

        OutboxState state = Assert.Single(fixture.DbContext.Set<OutboxState>().Local);
        OutboxMessage[] messages = fixture.DbContext.Set<OutboxMessage>().Local.ToArray();
        Assert.Equal(messageIds.Length, messages.Length);
        Assert.Equal(messageIds.Order(), messages.Select(message => message.MessageId).Order());
        Assert.All(messages, message => Assert.Equal(state.OutboxId, message.OutboxId));
        await context.AbortAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "committed-classic-batches-roll-state-and-signal")]
    public async Task CommittedBatches_RollStateAndSignalOncePerBatchAsync()
    {
        await using ClassicOutboxFixture fixture = await ClassicOutboxFixture.CreateAsync();
        using EntityFrameworkTransactionalScopedBusContext<IBus, ClassicOutboxDbContext> context = fixture.CreateContext();
        await context.AddSendAsync(CreateSendContext(Guid.NewGuid(), 1), TestContext.Current.CancellationToken);
        Guid firstOutboxId = Assert.Single(fixture.DbContext.Set<OutboxState>().Local).OutboxId;
        await context.CommitAsync(TestContext.Current.CancellationToken);

        await context.AddSendAsync(CreateSendContext(Guid.NewGuid(), 2), TestContext.Current.CancellationToken);
        Guid secondOutboxId = Assert.Single(fixture.DbContext.Set<OutboxState>().Local,
            state => state.OutboxId != firstOutboxId).OutboxId;
        await context.CommitAsync(TestContext.Current.CancellationToken);

        Assert.NotEqual(firstOutboxId, secondOutboxId);
        Assert.Equal(2, await fixture.DbContext.Set<OutboxState>().CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(2, await fixture.DbContext.Set<OutboxMessage>().CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(2, fixture.Notification.DeliveredCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "tracker-transition-rolls-state-and-preserves-foreign-intent")]
    public async Task TrackerTransition_RollsAcceptedStateAndAbortPreservesForeignIntentAsync()
    {
        await using ClassicOutboxFixture fixture = await ClassicOutboxFixture.CreateAsync();
        using EntityFrameworkTransactionalScopedBusContext<IBus, ClassicOutboxDbContext> context = fixture.CreateContext();
        var foreign = new OutboxMessage
        {
            OutboxId = Guid.NewGuid(),
            MessageId = Guid.NewGuid(),
            SentTime = Now,
            ContentType = "application/json",
            MessageType = "[]",
            Body = "{}",
        };
        var inbox = new OutboxMessage
        {
            MessageId = Guid.NewGuid(),
            SentTime = Now,
            ContentType = "application/json",
            MessageType = "[]",
            Body = "{}",
        };
        fixture.DbContext.Add(foreign);
        fixture.DbContext.Add(inbox);
        await context.AddSendAsync(CreateSendContext(Guid.NewGuid(), 1), TestContext.Current.CancellationToken);
        OutboxState acceptedState = Assert.Single(fixture.DbContext.Set<OutboxState>().Local);
        fixture.DbContext.Entry(acceptedState).State = EntityState.Unchanged;

        await context.AddSendAsync(CreateSendContext(Guid.NewGuid(), 2), TestContext.Current.CancellationToken);
        await context.AbortAsync(TestContext.Current.CancellationToken);
        await context.AbortAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, fixture.Notification.DeliveredCount);
        Assert.Equal(EntityState.Added, fixture.DbContext.Entry(foreign).State);
        Assert.Equal(EntityState.Added, fixture.DbContext.Entry(inbox).State);
        Assert.False(context.HasActiveSession);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "cancellation-and-disposal-fail-before-state-change")]
    public async Task CancellationAndDisposal_FailBeforeChangingSessionStateAsync()
    {
        await using ClassicOutboxFixture fixture = await ClassicOutboxFixture.CreateAsync();
        EntityFrameworkTransactionalScopedBusContext<IBus, ClassicOutboxDbContext> context = fixture.CreateContext();
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            context.AddSendAsync(CreateSendContext(Guid.NewGuid(), 1), canceled.Token));
        Assert.False(context.HasActiveSession);

        context.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            context.AddSendAsync(CreateSendContext(Guid.NewGuid(), 2), TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            context.CommitAsync(TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            context.AbortAsync(TestContext.Current.CancellationToken));
        context.Dispose();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "ambient-consume-endpoints-retain-context")]
    public async Task AmbientConsumeContext_ProvidesScopedEndpointsAndClientFactoryAsync()
    {
        await using ClassicOutboxFixture fixture = await ClassicOutboxFixture.CreateAsync();
        ArgumentNullException missingContext = Assert.Throws<ArgumentNullException>(() =>
            new EntityFrameworkTransactionalConsumeContextScopedBusContext<IBus, ClassicOutboxDbContext>(
                null!, null!, null!, null!, null!, null!, null!, null!));
        Assert.Equal("consumeContext", missingContext.ParamName);
        ConsumeContext ambient = DispatchProxy.Create<ConsumeContext, PassiveConsumeContextProxy>();
        using var context = new EntityFrameworkTransactionalConsumeContextScopedBusContext<IBus, ClassicOutboxDbContext>(
            fixture.Bus,
            fixture.DbContext,
            fixture.Notification,
            fixture.ClientFactory,
            fixture.Services,
            ambient,
            fixture.TimeProvider,
            fixture.PersistenceIdentity);

        Assert.NotNull(context.SendEndpointProvider);
        Assert.NotNull(context.PublishEndpoint);
        Assert.NotNull(context.ClientFactory);
        Assert.Same(context.SendEndpointProvider, context.SendEndpointProvider);
        Assert.Same(context.PublishEndpoint, context.PublishEndpoint);
        Assert.Same(context.ClientFactory, context.ClientFactory);
    }

    private static MessageSendContext<OutboxProbe> CreateSendContext(Guid messageId, int sequence) => new(
        new OutboxProbe(sequence))
    {
        MessageId = messageId,
        Serializer = ServiceBusMetadataJson.MessageSerializer,
        DestinationAddress = new Uri("loopback://classic-transactional-outbox/probe"),
    };

    public sealed record OutboxProbe(int Sequence);
    public sealed record BusinessRecord(Guid Id, string Value);
    public sealed record ServiceMarker;

    public sealed class ClassicOutboxDbContext(DbContextOptions<ClassicOutboxDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddTransactionalOutboxEntities();
            modelBuilder.Entity<BusinessRecord>().HasKey(record => record.Id);
        }
    }

    private sealed class ClassicOutboxFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private ClassicOutboxFixture(
            SqliteConnection connection,
            ClassicOutboxDbContext dbContext,
            IBus bus,
            RecordingNotification notification,
            IClientFactory clientFactory,
            ServiceProvider services,
            FakeTimeProvider timeProvider,
            BusPersistenceIdentity<IBus> persistenceIdentity,
            ServiceMarker marker)
        {
            _connection = connection;
            DbContext = dbContext;
            Bus = bus;
            Notification = notification;
            ClientFactory = clientFactory;
            Services = services;
            TimeProvider = timeProvider;
            PersistenceIdentity = persistenceIdentity;
            Marker = marker;
        }

        public IBus Bus { get; }
        public IClientFactory ClientFactory { get; }
        public ClassicOutboxDbContext DbContext { get; }
        public ServiceMarker Marker { get; }
        public RecordingNotification Notification { get; }
        public BusPersistenceIdentity<IBus> PersistenceIdentity { get; }
        public ServiceProvider Services { get; }
        public FakeTimeProvider TimeProvider { get; }

        public static async Task<ClassicOutboxFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            var dbContext = new ClassicOutboxDbContext(
                new DbContextOptionsBuilder<ClassicOutboxDbContext>().UseSqlite(connection).Options);
            await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            var marker = new ServiceMarker();
            ServiceProvider services = new ServiceCollection()
                .AddSingleton(marker)
                .AddViciOnePayloadAdmission<IBus>(options =>
                {
                    options.MaximumSerializedBodyBytes = 1024 * 1024;
                    options.MaximumTransportEnvelopeBytes = 2 * 1024 * 1024;
                })
                .BuildServiceProvider();

            return new ClassicOutboxFixture(
                connection,
                dbContext,
                global::ViciOne.ServiceBus.Advanced.Bus.Factory.CreateUsingInMemory(_ => { }),
                new RecordingNotification(),
                DispatchProxy.Create<IClientFactory, ThrowingClientFactoryProxy>(),
                services,
                new FakeTimeProvider(Now),
                BusPersistenceIdentity<IBus>.Create("classic"),
                marker);
        }

        public EntityFrameworkTransactionalScopedBusContext<IBus, ClassicOutboxDbContext> CreateContext() => new(
            Bus,
            DbContext,
            Notification,
            ClientFactory,
            Services,
            TimeProvider,
            PersistenceIdentity);

        public async ValueTask DisposeAsync()
        {
            await DbContext.DisposeAsync();
            await _connection.DisposeAsync();
            await Services.DisposeAsync();
        }
    }

    public class ThrowingClientFactoryProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException("The classic outbox tests must not resolve a request client.");
    }

    private class PassiveConsumeContextProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.ReturnType.IsValueType == true
                ? Activator.CreateInstance(targetMethod.ReturnType)
                : null;
    }

    public sealed class RecordingNotification : IBusOutboxNotification<EntityFrameworkBusOutboxScope<IBus, ClassicOutboxDbContext>>
    {
        private int _deliveredCount;

        public int DeliveredCount => Volatile.Read(ref _deliveredCount);

        public void SignalDelivery() => Interlocked.Increment(ref _deliveredCount);

        public Task WaitForDeliveryAsync(CancellationToken cancellationToken) =>
            throw new InvalidOperationException("The classic outbox tests must not wait for delivery.");
    }
}
