using System.Buffers;
using System.Net.Mime;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.Outbox;
using ViciOne.ServiceBus.Operations;
using ViciOne.ServiceBus.Providers.Persistence;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
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

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-METADATA", "productive-admission-rejects-serializer-message-id-change")]
    public async Task AddSend_RejectsSerializerIdentityChangeBeforeTrackingAsync(bool receiveOutbox, bool clearId)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        await using ClassicOutboxFixture fixture = await ClassicOutboxFixture.CreateAsync();
        Guid initialId = Guid.NewGuid();
        Guid replacementId = clearId ? Guid.Empty : Guid.NewGuid();
        MessageSendContext<OutboxProbe> outgoing = CreateSendContext(initialId, 1);
        outgoing.Serializer = new MessageIdChangingBoundedSerializer(replacementId);

        MessageException failure;
        if (receiveOutbox)
        {
            Guid inboxId = Guid.NewGuid();
            Guid consumerId = Guid.NewGuid();
            var inboxState = new InboxState
            {
                MessageId = inboxId,
                ConsumerId = consumerId,
                LockId = Guid.NewGuid(),
                Received = Now,
                ReceiveCount = 1,
            };
            var options = new OutboxConsumeOptions
            {
                ConsumerId = consumerId,
                ConsumerType = nameof(OutboxProbe),
                MessageDeliveryLimit = 1,
                MessageDeliveryTimeout = TimeSpan.FromMinutes(1),
            };
            ConsumeContext<OutboxProbe> input = InMemoryOutboxTestContextFactory.Create(
                new OutboxProbe(0), token, messageId: inboxId);
            await using var transaction = await fixture.DbContext.Database.BeginTransactionAsync(token);
            using var context = new DbContextOutboxConsumeContext<IBus, ClassicOutboxDbContext, OutboxProbe>(
                input, options, fixture.Services, fixture.DbContext, transaction, inboxState, fixture.TimeProvider);
            failure = await Assert.ThrowsAsync<MessageException>(() => context.AddSendAsync(outgoing, token));
            await transaction.RollbackAsync(token);
        }
        else
        {
            using EntityFrameworkTransactionalScopedBusContext<IBus, ClassicOutboxDbContext> context = fixture.CreateContext();
            try
            {
                failure = await Assert.ThrowsAsync<MessageException>(() => context.AddSendAsync(outgoing, token));
                Assert.False(context.HasActiveSession);
            }
            finally
            {
                if (context.HasActiveSession)
                    await context.AbortAsync(token);
            }
        }

        Assert.Contains("changed during serialization", failure.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(initialId, outgoing.MessageId);
        Assert.Empty(fixture.DbContext.ChangeTracker.Entries<OutboxMessage>());
        Assert.Empty(fixture.DbContext.ChangeTracker.Entries<OutboxState>());
        Assert.Empty(await fixture.DbContext.Set<OutboxMessage>().AsNoTracking().ToListAsync(token));
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
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "tracker-transition-rejects-unsaved-state-and-preserves-foreign-intent")]
    public async Task TrackerTransition_RejectsUnsavedStateAndAbortPreservesForeignIntentAsync()
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

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.AddSendAsync(CreateSendContext(Guid.NewGuid(), 2), TestContext.Current.CancellationToken));
        await context.AbortAsync(TestContext.Current.CancellationToken);
        await context.AbortAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, fixture.Notification.DeliveredCount);
        Assert.Equal(EntityState.Added, fixture.DbContext.Entry(foreign).State);
        Assert.Equal(EntityState.Added, fixture.DbContext.Entry(inbox).State);
        Assert.False(context.HasActiveSession);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "t101-accept-all-changes-cannot-signal-unsaved-classic-intent")]
    public async Task AcceptAllChanges_CannotSignalOrCommitUnsavedClassicIntentAsync()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        await using ClassicOutboxFixture fixture = await ClassicOutboxFixture.CreateAsync();
        using EntityFrameworkTransactionalScopedBusContext<IBus, ClassicOutboxDbContext> context = fixture.CreateContext();
        await context.AddSendAsync(CreateSendContext(Guid.NewGuid(), 1), token);
        fixture.DbContext.ChangeTracker.AcceptAllChanges();

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() => context.CommitAsync(token));

        Assert.Contains("staged", failure.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, fixture.Notification.DeliveredCount);
        Assert.True(context.HasActiveSession);
        await context.AbortAsync(token);
        await using ClassicOutboxDbContext persisted = fixture.CreateFreshContext();
        Assert.Empty(await persisted.Set<OutboxState>().AsNoTracking().ToArrayAsync(token));
        Assert.Empty(await persisted.Set<OutboxMessage>().AsNoTracking().ToArrayAsync(token));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "t101-detached-message-cannot-complete-classic-batch")]
    public async Task DetachedStagedMessage_CannotCompleteOrSignalPartialClassicBatchAsync()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        await using ClassicOutboxFixture fixture = await ClassicOutboxFixture.CreateAsync();
        using EntityFrameworkTransactionalScopedBusContext<IBus, ClassicOutboxDbContext> context = fixture.CreateContext();
        await context.AddSendAsync(CreateSendContext(Guid.NewGuid(), 1), token);
        OutboxMessage message = Assert.Single(fixture.DbContext.Set<OutboxMessage>().Local);
        fixture.DbContext.Entry(message).State = EntityState.Detached;

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() => context.CommitAsync(token));

        Assert.Contains("staged", failure.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, fixture.Notification.DeliveredCount);
        await context.AbortAsync(token);
        await using ClassicOutboxDbContext persisted = fixture.CreateFreshContext();
        Assert.Empty(await persisted.Set<OutboxState>().AsNoTracking().ToArrayAsync(token));
        Assert.Empty(await persisted.Set<OutboxMessage>().AsNoTracking().ToArrayAsync(token));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "t101-abort-uses-exact-message-ownership")]
    public async Task Abort_DetachesOwnedMessageEvenAfterIdChangeAndPreservesForeignMatchAsync()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        await using ClassicOutboxFixture fixture = await ClassicOutboxFixture.CreateAsync();
        using EntityFrameworkTransactionalScopedBusContext<IBus, ClassicOutboxDbContext> context = fixture.CreateContext();
        await context.AddSendAsync(CreateSendContext(Guid.NewGuid(), 1), token);
        OutboxMessage owned = Assert.Single(fixture.DbContext.Set<OutboxMessage>().Local);
        Guid sessionId = Assert.IsType<Guid>(owned.OutboxId);
        owned.OutboxId = Guid.NewGuid();
        var foreign = new OutboxMessage
        {
            OutboxId = sessionId,
            MessageId = Guid.NewGuid(),
            SentTime = Now,
            ContentType = "application/json",
            MessageType = "[]",
            Body = "{}",
        };
        fixture.DbContext.Add(foreign);

        await context.AbortAsync(token);

        Assert.Equal(EntityState.Detached, fixture.DbContext.Entry(owned).State);
        Assert.Equal(EntityState.Added, fixture.DbContext.Entry(foreign).State);
        Assert.Equal(0, fixture.Notification.DeliveredCount);
        Assert.False(context.HasActiveSession);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "t101-mutated-delivery-state-cannot-discard-unsent-intent")]
    public async Task MutatedDeliveryState_CannotCommitOrSignalUnsentIntentAsync(bool markDelivered)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        await using ClassicOutboxFixture fixture = await ClassicOutboxFixture.CreateAsync();
        using EntityFrameworkTransactionalScopedBusContext<IBus, ClassicOutboxDbContext> context = fixture.CreateContext();
        await context.AddSendAsync(CreateSendContext(Guid.NewGuid(), 1), token);
        OutboxState state = Assert.Single(fixture.DbContext.Set<OutboxState>().Local);
        if (markDelivered)
            state.Status = OutboxDeliveryStatus.Delivered;
        else
            state.LastSequenceNumber = long.MaxValue;

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() => context.CommitAsync(token));

        Assert.Contains("delivery", failure.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, fixture.Notification.DeliveredCount);
        await using ClassicOutboxDbContext persisted = fixture.CreateFreshContext();
        Assert.Empty(await persisted.Set<OutboxState>().AsNoTracking().ToArrayAsync(token));
        Assert.Empty(await persisted.Set<OutboxMessage>().AsNoTracking().ToArrayAsync(token));
        await context.AbortAsync(token);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "t101-rejected-first-send-leaves-classic-session-reusable")]
    public async Task RejectedFirstSend_LeavesClassicSessionReusableWithoutAbortAsync()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        await using ClassicOutboxFixture fixture = await ClassicOutboxFixture.CreateAsync();
        using EntityFrameworkTransactionalScopedBusContext<IBus, ClassicOutboxDbContext> context = fixture.CreateContext();

        await Assert.ThrowsAsync<MessageException>(() => context.AddSendAsync(CreateSendContext(Guid.Empty, 1), token));

        Assert.False(context.HasActiveSession);
        Assert.Empty(fixture.DbContext.Set<OutboxState>().Local);
        Guid validId = Guid.NewGuid();
        await context.AddSendAsync(CreateSendContext(validId, 2), token);
        await context.CommitAsync(token);
        await using ClassicOutboxDbContext persisted = fixture.CreateFreshContext();
        OutboxMessage stored = await persisted.Set<OutboxMessage>().AsNoTracking().SingleAsync(token);
        Assert.Equal(validId, stored.MessageId);
        Assert.Equal(1, fixture.Notification.DeliveredCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "t101-add-tracking-failure-leaves-no-orphan-and-allows-retry")]
    public async Task TrackingFailure_LeavesNoOrphanAndAllowsHealthyRetryAsync(bool throwOnState)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        await using ClassicOutboxFixture fixture = await ClassicOutboxFixture.CreateAsync();
        using EntityFrameworkTransactionalScopedBusContext<IBus, ClassicOutboxDbContext> context = fixture.CreateContext();
        var sentinel = new InvalidOperationException("tracked event failed");
        bool throwOnce = true;
        fixture.DbContext.ChangeTracker.Tracked += (_, args) =>
        {
            if (throwOnce && (throwOnState ? args.Entry.Entity is OutboxState : args.Entry.Entity is OutboxMessage))
            {
                throwOnce = false;
                throw sentinel;
            }
        };

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.AddSendAsync(CreateSendContext(Guid.NewGuid(), 1), token));

        Assert.Same(sentinel, failure);
        Assert.False(context.HasActiveSession);
        Assert.Empty(fixture.DbContext.Set<OutboxState>().Local);
        Assert.Empty(fixture.DbContext.Set<OutboxMessage>().Local);
        Guid validId = Guid.NewGuid();
        await context.AddSendAsync(CreateSendContext(validId, 2), token);
        await context.CommitAsync(token);
        await using ClassicOutboxDbContext persisted = fixture.CreateFreshContext();
        Assert.Equal(validId, (await persisted.Set<OutboxMessage>().AsNoTracking().SingleAsync(token)).MessageId);
        Assert.Equal(1, await persisted.Set<OutboxState>().AsNoTracking().CountAsync(token));
        Assert.Equal(1, fixture.Notification.DeliveredCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-TRANSACTIONAL-OUTBOX", "t101-external-save-without-acceptance-completes-classic-batch-once")]
    public async Task ExternalSaveWithoutAcceptance_CompletesClassicBatchOnceAndAllowsNextBatchAsync()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        await using ClassicOutboxFixture fixture = await ClassicOutboxFixture.CreateAsync();
        using EntityFrameworkTransactionalScopedBusContext<IBus, ClassicOutboxDbContext> context = fixture.CreateContext();
        var business = new BusinessRecord(Guid.NewGuid(), "caller owned");
        fixture.DbContext.Add(business);
        await context.AddSendAsync(CreateSendContext(Guid.NewGuid(), 1), token);

        await fixture.DbContext.SaveChangesAsync(acceptAllChangesOnSuccess: false, token);

        Assert.False(context.HasActiveSession);
        Assert.Equal(1, fixture.Notification.DeliveredCount);
        Assert.Equal(EntityState.Added, fixture.DbContext.Entry(business).State);
        fixture.DbContext.ChangeTracker.AcceptAllChanges();
        await context.CommitAsync(token);
        await context.AddSendAsync(CreateSendContext(Guid.NewGuid(), 2), token);
        await context.CommitAsync(token);
        Assert.Equal(2, fixture.Notification.DeliveredCount);
        await using ClassicOutboxDbContext persisted = fixture.CreateFreshContext();
        Assert.Equal(2, await persisted.Set<OutboxState>().AsNoTracking().CountAsync(token));
        Assert.Equal(2, await persisted.Set<OutboxMessage>().AsNoTracking().CountAsync(token));
        Assert.Equal(business, await persisted.Set<BusinessRecord>().AsNoTracking().SingleAsync(token));
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

    private sealed class MessageIdChangingBoundedSerializer(Guid replacementId) : IBoundedMessageSerializer
    {
        public ContentType ContentType { get; } = new("application/octet-stream");

        public SerializedTransportTextFormat TransportTextFormat => SerializedTransportTextFormat.Base64;

        public MessageBody GetMessageBody<T>(SendContext<T> context) where T : class =>
            throw new NotSupportedException("The bounded serializer path must be used.");

        public void WriteSerializedBody<T>(SendContext<T> context, IBufferWriter<byte> writer) where T : class
        {
            writer.GetSpan(1)[0] = 42;
            writer.Advance(1);
        }

        public void WriteTransportEnvelope<T>(SendContext<T> context, Stream serializedBody, IBufferWriter<byte> writer)
            where T : class
        {
            context.MessageId = replacementId;
            int value = serializedBody.ReadByte();
            if (value < 0)
                throw new InvalidOperationException("The admitted application body was empty.");
            writer.GetSpan(1)[0] = (byte)value;
            writer.Advance(1);
        }

        public bool TryLocateSerializedBody(ReadOnlySpan<byte> serializedEnvelope, out int offset, out int length)
        {
            offset = 0;
            length = serializedEnvelope.Length;
            return length == 1;
        }
    }

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

        public ClassicOutboxDbContext CreateFreshContext() => new(
            new DbContextOptionsBuilder<ClassicOutboxDbContext>().UseSqlite(_connection).Options);

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
