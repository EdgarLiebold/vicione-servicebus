using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.DurableSend;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Transports.Fabric;
using Xunit;

namespace ViciOne.ServiceBus.Tests.ReliableMessaging;

public sealed class ReliableInMemoryIntegrationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-RELIABLE-INBOX", "delivery-canceled-during-payload-admission-rejects-outgoing-intent")]
    public async Task DeliveryCancellation_DuringPayloadAdmission_RejectsOutgoingIntentAsync(bool cancelDelivery)
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken testToken = TestContext.Current.CancellationToken;
        var probe = new SerializationCancellationProbe(cancelDelivery);
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(probe)
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                ConfigureReliableMessaging(configuration);
                configuration.AddConsumer<SerializationCancellationConsumer>();
            })
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: testToken).WaitAsync(timeout, testToken);
        Guid messageId = NewId.NextGuid();
        try
        {
            await harness.Bus.PublishAsync(new ReliableCommand(messageId, 0),
                send => send.MessageId = messageId, testToken);
            Exception? observed = await probe.Completed.Task.WaitAsync(timeout, testToken);
            OperationCanceledException failure = Assert.IsType<OperationCanceledException>(observed);
            Assert.Equal(cancelDelivery ? probe.Delivery.Token : probe.Operation.Token, failure.CancellationToken);
            Assert.True(probe.SerializationCalls > 0);
            Assert.Equal(0, probe.BufferedMessages);
            Assert.Equal(cancelDelivery, probe.Delivery.IsCancellationRequested);
            Assert.Equal(!cancelDelivery, probe.Operation.IsCancellationRequested);
            IInboxStore<IBus> inbox = provider.GetRequiredService<IInboxStore<IBus>>();
            ReliableInboxAcquireResult retained = await inbox.AcquireAsync(
                new ReliableInboxKey(messageId, probe.ConsumerId), DateTimeOffset.UtcNow,
                TimeSpan.FromMinutes(1), testToken);
            Assert.Equal(ReliableInboxAcquireDisposition.Busy, retained.Disposition);
            Assert.Equal(1, retained.Attempt);
            Assert.Empty((await inbox.GetQuarantineAsync(new ReliableInboxQuarantineQuery(), testToken)).Entries);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
            probe.Delivery.Dispose();
            probe.Operation.Dispose();
        }
        IOutboxStore<IBus> outbox = provider.GetRequiredService<IOutboxStore<IBus>>();
        DurableSendStoreSnapshot snapshot = await outbox.GetSnapshotAsync(testToken);
        Assert.Equal((0, 0L), (snapshot.StoredCount, snapshot.StoredBytes));
        Assert.Empty(await outbox.ClaimDueAsync(DateTimeOffset.UtcNow.AddDays(2), 10,
            TimeSpan.FromMinutes(1), testToken));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-RELIABLE-INBOX", "delivery-cancellation-rejects-or-discards-outgoing-intent")]
    public async Task DeliveryCancellation_BeforeOrAfterBuffering_DoesNotCommitOutgoingIntentAsync(bool cancelBeforePublish)
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken testToken = TestContext.Current.CancellationToken;
        var probe = new DistinctDeliveryCancellationProbe(cancelBeforePublish);
        var observation = new ReliableObservation();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(probe)
            .AddSingleton(observation)
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                ConfigureReliableMessaging(configuration);
                configuration.AddConsumer<DistinctDeliveryCancellationConsumer>();
                configuration.AddConsumer<ReliableEventConsumer>();
            })
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: testToken).WaitAsync(timeout, testToken);
        Guid messageId = NewId.NextGuid();
        try
        {
            await harness.Bus.PublishAsync(new ReliableCommand(messageId, 0),
                send => send.MessageId = messageId, testToken);
            Exception? observed = await probe.Completed.Task.WaitAsync(timeout, testToken);
            OperationCanceledException failure = Assert.IsType<OperationCanceledException>(observed);
            Assert.Equal(probe.Delivery.Token, failure.CancellationToken);
            Assert.Equal(cancelBeforePublish ? 0 : 1, probe.BufferedMessages);
            IInboxStore<IBus> inbox = provider.GetRequiredService<IInboxStore<IBus>>();
            ReliableInboxAcquireResult retained = await inbox.AcquireAsync(
                new ReliableInboxKey(messageId, probe.ConsumerId), DateTimeOffset.UtcNow,
                TimeSpan.FromMinutes(1), testToken);
            Assert.Equal(ReliableInboxAcquireDisposition.Busy, retained.Disposition);
            Assert.Equal(1, retained.Attempt);
            Assert.Empty((await inbox.GetQuarantineAsync(new ReliableInboxQuarantineQuery(), testToken)).Entries);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
            probe.Delivery.Dispose();
            probe.Operation.Dispose();
        }
        Assert.Empty(observation.Events);
        Assert.Empty(harness.Consumed.Snapshot<ReliableEvent>());
        IOutboxStore<IBus> outbox = provider.GetRequiredService<IOutboxStore<IBus>>();
        DurableSendStoreSnapshot snapshot = await outbox.GetSnapshotAsync(testToken);
        Assert.Equal((0, 0L), (snapshot.StoredCount, snapshot.StoredBytes));
        Assert.Empty(await outbox.ClaimDueAsync(DateTimeOffset.UtcNow.AddDays(2), 10,
            TimeSpan.FromMinutes(1), testToken));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [RequirementCoverage("REQ-VSB-RELIABLE-INBOX", "either-distinct-cancellation-token-retains-lease")]
    public async Task Cancellation_FromEitherDistinctToken_DoesNotCommitOrScheduleFailureAsync(bool cancelDelivery)
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken testToken = TestContext.Current.CancellationToken;
        using var delivery = new CancellationTokenSource();
        using var operation = new CancellationTokenSource();
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                ConfigureReliableMessaging(configuration);
            })
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: testToken).WaitAsync(timeout, testToken);
        Guid messageId = NewId.NextGuid();
        Guid consumerId = NewId.NextGuid();
        var key = new ReliableInboxKey(messageId, consumerId);
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        try
        {
            ConsumeContext<ReliableCommand> input = InMemoryOutboxTestContextFactory.Create(
                new ReliableCommand(messageId, 0), delivery.Token, messageId: messageId);
            OperationCanceledException actual = await Assert.ThrowsAsync<OperationCanceledException>(() =>
                InMemoryInboxPipelineTestDriver.SendAsync(scope.ServiceProvider, input, consumerId, context =>
                {
                    Assert.Equal(messageId, context.MessageId);
                    if (cancelDelivery)
                        delivery.Cancel();
                    else
                        operation.Cancel();
                    return Task.CompletedTask;
                }, operation.Token, completeConsumer: true));
            Assert.Equal(cancelDelivery ? delivery.Token : operation.Token, actual.CancellationToken);
            Assert.Equal(cancelDelivery, delivery.IsCancellationRequested);
            Assert.Equal(!cancelDelivery, operation.IsCancellationRequested);

            IInboxStore<IBus> inbox = provider.GetRequiredService<IInboxStore<IBus>>();
            ReliableInboxAcquireResult retained = await inbox.AcquireAsync(
                key, DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1), testToken);
            Assert.Equal(ReliableInboxAcquireDisposition.Busy, retained.Disposition);
            Assert.Equal(1, retained.Attempt);
            Assert.Empty((await inbox.GetQuarantineAsync(new ReliableInboxQuarantineQuery(), testToken)).Entries);

            using var waitingDelivery = new CancellationTokenSource();
            using var waitingOperation = new CancellationTokenSource();
            int waitingCallbacks = 0;
            ConsumeContext<ReliableCommand> waitingInput = InMemoryOutboxTestContextFactory.Create(
                new ReliableCommand(messageId, 0), waitingDelivery.Token, messageId: messageId);
            Task waiting = InMemoryInboxPipelineTestDriver.SendAsync(
                scope.ServiceProvider, waitingInput, consumerId, _ =>
                {
                    waitingCallbacks++;
                    return Task.CompletedTask;
                }, waitingOperation.Token);
            await Task.Delay(TimeSpan.FromMilliseconds(30), testToken);
            Assert.False(waiting.IsCompleted);
            if (cancelDelivery)
                waitingDelivery.Cancel();
            else
                waitingOperation.Cancel();
            OperationCanceledException waitFailure = await Assert.ThrowsAsync<OperationCanceledException>(
                () => waiting.WaitAsync(timeout, testToken));
            Assert.Equal(cancelDelivery ? waitingDelivery.Token : waitingOperation.Token, waitFailure.CancellationToken);
            Assert.Equal(0, waitingCallbacks);

            using var preDelivery = new CancellationTokenSource();
            using var preOperation = new CancellationTokenSource();
            if (cancelDelivery)
                preDelivery.Cancel();
            else
                preOperation.Cancel();
            Guid preMessageId = NewId.NextGuid();
            Guid preConsumerId = NewId.NextGuid();
            int callbackCount = 0;
            ConsumeContext<ReliableCommand> preInput = InMemoryOutboxTestContextFactory.Create(
                new ReliableCommand(preMessageId, 0), preDelivery.Token, messageId: preMessageId);
            OperationCanceledException preFailure = await Assert.ThrowsAsync<OperationCanceledException>(() =>
                InMemoryInboxPipelineTestDriver.SendAsync(scope.ServiceProvider, preInput, preConsumerId, _ =>
                {
                    callbackCount++;
                    return Task.CompletedTask;
                }, preOperation.Token, completeConsumer: true));
            Assert.Equal(cancelDelivery ? preDelivery.Token : preOperation.Token, preFailure.CancellationToken);
            Assert.Equal(0, callbackCount);
            ReliableInboxAcquireResult fresh = await inbox.AcquireAsync(
                new ReliableInboxKey(preMessageId, preConsumerId), DateTimeOffset.UtcNow,
                TimeSpan.FromMinutes(1), testToken);
            Assert.Equal(ReliableInboxAcquireDisposition.Acquired, fresh.Disposition);
            Assert.Equal(1, fresh.Attempt);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RELIABLE-INBOX", "admission-rejection-discards-buffered-sends-and-operator-retry-commits")]
    public async Task ConsumerAdmission_RejectionDiscardsBufferedSendsAndOperatorRetryCommitsAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken token = TestContext.Current.CancellationToken;
        var clock = new FixedInboxClock(new DateTimeOffset(2026, 9, 27, 12, 0, 0, TimeSpan.Zero));
        var probe = new AdmissionRecoveryProbe();
        var observation = new ReliableObservation();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton<TimeProvider>(clock)
            .AddSingleton(probe)
            .AddSingleton(observation)
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                ConfigureReliableMessaging(configuration, 1, new MessageLimits
                {
                    MaxBodyBytes = 256,
                    MaxEnvelopeBytes = 8192,
                    MaxJsonDepth = 32
                });
                configuration.AddConsumer<AdmissionRecoveryConsumer>();
                configuration.AddConsumer<ReliableEventConsumer>();
            })
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: token).WaitAsync(timeout, token);
        Guid messageId = NewId.NextGuid();
        var key = new ReliableInboxKey(messageId, probe.ConsumerId);
        IInboxStore<IBus> inbox = provider.GetRequiredService<IInboxStore<IBus>>();
        IOutboxStore<IBus> outbox = provider.GetRequiredService<IOutboxStore<IBus>>();
        try
        {
            await harness.Bus.PublishAsync(new ReliableCommand(messageId, 0), send => send.MessageId = messageId, token);
            Assert.Null(await probe.FirstCompleted.Task.WaitAsync(timeout, token));
            PayloadAdmissionException failure = Assert.IsType<PayloadAdmissionException>(probe.Rejected);
            Assert.Equal(PayloadAdmissionStage.SerializedBody, failure.Stage);
            Assert.Equal(256, failure.ConfiguredLimitBytes);
            Assert.True(failure.ActualBytes > failure.ConfiguredLimitBytes);
            Assert.Equal(1, probe.PreparedMessages);
            ReliableInboxQuarantineEntry quarantined = Assert.Single((await inbox.GetQuarantineAsync(
                new ReliableInboxQuarantineQuery(), token)).Entries);
            Assert.Equal(key, quarantined.Key);
            Assert.Equal(1, quarantined.Attempts);
            Assert.Equal(typeof(PayloadAdmissionException).FullName, quarantined.FailureType);
            Assert.Equal(ReliableInboxAcquireDisposition.Unavailable,
                (await inbox.AcquireAsync(key, clock.GetUtcNow(), TimeSpan.FromMinutes(1), token)).Disposition);
            DurableSendStoreSnapshot empty = await outbox.GetSnapshotAsync(token);
            Assert.Equal((0, 0L), (empty.StoredCount, empty.StoredBytes));
            Assert.Equal(new ReliableMessagingOperationResult(ReliableMessageReference.Inbox(key),
                ReliableMessagingOperationDisposition.Applied, "Quarantined", "RetryScheduled"),
                await inbox.RequeueAsync(key, clock.GetUtcNow(), token));

            probe.Recover.TrySetResult();
            Assert.Null(await probe.Completed.Task.WaitAsync(timeout, token));
            Assert.Equal(2, probe.PreparedMessages);
            ReliableInboxAcquireResult consumed = await inbox.AcquireAsync(key, clock.GetUtcNow(), TimeSpan.FromMinutes(1), token);
            Assert.Equal(ReliableInboxAcquireDisposition.AlreadyConsumed, consumed.Disposition);
            Assert.Equal(2, consumed.Attempt);
            Assert.DoesNotContain((await inbox.GetQuarantineAsync(new ReliableInboxQuarantineQuery(), token)).Entries,
                entry => entry.Key == key);
            Assert.Equal(1, (await outbox.GetSnapshotAsync(token)).StoredCount);
            DateTimeOffset dueAt = clock.GetUtcNow().AddDays(1);
            DurableSendDelivery delivery = Assert.Single(await outbox.ClaimDueAsync(dueAt, 10, TimeSpan.FromMinutes(1), token));
            Assert.Equal(new DurableSendId(probe.OutgoingId), delivery.Message.Id);
            Assert.Equal(dueAt, delivery.Message.DueAt);
            using System.Text.Json.JsonDocument envelope = System.Text.Json.JsonDocument.Parse(delivery.Message.Body);
            Assert.Equal("recovered", envelope.RootElement.GetProperty("message").GetProperty("text").GetString());
            Assert.Equal(messageId, envelope.RootElement.GetProperty("message").GetProperty("messageId").GetGuid());
            Assert.True(await outbox.MarkDeliveredAsync(delivery.Message.Id, delivery.Lease, dueAt, token));
            Assert.Equal(empty, await outbox.GetSnapshotAsync(token));
        }
        finally
        {
            probe.Recover.TrySetCanceled(token);
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
        // The nested factory shares pending sends with the enclosing transport consumer.
        // Its rejected send still faults that outer consumer after the inner recovery completes.
        ReliableInboxQuarantineEntry outerFailure = Assert.Single((await inbox.GetQuarantineAsync(
            new ReliableInboxQuarantineQuery(), token)).Entries);
        Assert.Equal(messageId, outerFailure.Key.MessageId);
        Assert.NotEqual(probe.ConsumerId, outerFailure.Key.ConsumerId);
        Assert.Equal(1, outerFailure.Attempts);
        Assert.Equal(typeof(PayloadAdmissionException).FullName, outerFailure.FailureType);
        Assert.Empty(observation.Events);
        Assert.Empty(harness.Consumed.Snapshot<ReliableEvent>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RELIABLE-INBOX", "consumer-commit-retains-delayed-envelope-until-exact-due-boundary")]
    public async Task ConsumerCommit_DelayedEnvelopeRemainsRetainedUntilExactDueBoundaryAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken token = TestContext.Current.CancellationToken;
        var clock = new FixedInboxClock(new DateTimeOffset(2026, 9, 27, 12, 0, 0, TimeSpan.Zero));
        var probe = new DelayedCommitProbe();
        var observation = new ReliableObservation();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton<TimeProvider>(clock)
            .AddSingleton(probe)
            .AddSingleton(observation)
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                ConfigureReliableMessaging(configuration);
                configuration.AddConsumer<DelayedCommitConsumer>();
                configuration.AddConsumer<ReliableEventConsumer>();
            })
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: token).WaitAsync(timeout, token);
        Guid messageId = NewId.NextGuid();
        IOutboxStore<IBus> outbox = provider.GetRequiredService<IOutboxStore<IBus>>();
        try
        {
            await harness.Bus.PublishAsync(new ReliableCommand(messageId, 0), send => send.MessageId = messageId, token);
            Assert.Null(await probe.Completed.Task.WaitAsync(timeout, token));
            IInboxStore<IBus> inbox = provider.GetRequiredService<IInboxStore<IBus>>();
            ReliableInboxAcquireResult consumed = await inbox.AcquireAsync(new ReliableInboxKey(messageId, probe.ConsumerId),
                clock.GetUtcNow(), TimeSpan.FromMinutes(1), token);
            Assert.Equal(ReliableInboxAcquireDisposition.AlreadyConsumed, consumed.Disposition);
            Assert.Equal(1, consumed.Attempt);
            DurableSendStoreSnapshot retained = await outbox.GetSnapshotAsync(token);
            Assert.Equal(1, retained.StoredCount);
            Assert.True(retained.StoredBytes > 0);
            DateTimeOffset dueAt = clock.GetUtcNow().Add(probe.Delay);
            Assert.Empty(await outbox.ClaimDueAsync(dueAt.AddTicks(-1), 10, TimeSpan.FromMinutes(1), token));
            Assert.Equal(retained, await outbox.GetSnapshotAsync(token));

            DurableSendDelivery delivery = Assert.Single(await outbox.ClaimDueAsync(dueAt, 10, TimeSpan.FromMinutes(1), token));
            Assert.Equal(new DurableSendId(probe.OutgoingId), delivery.Message.Id);
            Assert.Equal(probe.OutgoingId, delivery.Message.MessageId);
            Assert.Equal(probe.CorrelationId, delivery.Message.CorrelationId);
            Assert.Equal(dueAt, delivery.Message.DueAt);
            Assert.Equal(new MessageContractIdentity("reliable-event", 1), delivery.Message.ContractIdentity);
            Assert.Equal(0, delivery.DeliveryAttempts);
            using System.Text.Json.JsonDocument envelope = System.Text.Json.JsonDocument.Parse(delivery.Message.Body);
            Assert.Equal("delayed-buffer", envelope.RootElement.GetProperty("message").GetProperty("text").GetString());
            Assert.Equal(messageId, envelope.RootElement.GetProperty("message").GetProperty("messageId").GetGuid());
            Assert.True(await outbox.MarkDeliveredAsync(delivery.Message.Id, delivery.Lease, dueAt, token));
            DurableSendStoreSnapshot empty = await outbox.GetSnapshotAsync(token);
            Assert.Equal((0, 0L), (empty.StoredCount, empty.StoredBytes));
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
        Assert.Empty(observation.Events);
        Assert.Empty(harness.Consumed.Snapshot<ReliableEvent>());
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(3, false)]
    [InlineData(1, true)]
    [InlineData(3, true)]
    [RequirementCoverage("REQ-VSB-RELIABLE-INBOX", "consumer-failure-after-removal-or-takeover-preserves-current-state")]
    public async Task ConsumerFailure_AfterRemovalOrTakeoverPreservesOwnerAndBufferedMessagesAsync(int maximumAttempts, bool remove)
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken token = TestContext.Current.CancellationToken;
        var probe = new OwnershipFailureProbe(remove);
        var observation = new ReliableObservation();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(probe)
            .AddSingleton(observation)
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                ConfigureReliableMessaging(configuration, maximumAttempts);
                configuration.AddConsumer<OwnershipFailureConsumer>();
                configuration.AddConsumer<ReliableEventConsumer>();
            })
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: token).WaitAsync(timeout, token);
        Guid messageId = NewId.NextGuid();
        var key = new ReliableInboxKey(messageId, probe.ConsumerId);
        IInboxStore<IBus> inbox = provider.GetRequiredService<IInboxStore<IBus>>();
        var neighbor = new ReliableInboxKey(messageId, Guid.NewGuid());
        ReliableInboxAcquireResult acquired = await inbox.AcquireAsync(neighbor, DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1), token);
        Assert.True(await inbox.CompleteAsync(neighbor, Assert.IsType<ReliableInboxLease>(acquired.Lease), DateTimeOffset.UtcNow, token));
        try
        {
            await harness.Bus.PublishAsync(new ReliableCommand(messageId, 0), send => send.MessageId = messageId, token);
            Exception? observed = await probe.Completed.Task.WaitAsync(timeout, token);
            Assert.Equal(1, probe.PreparedMessages);
            if (remove)
            {
                Assert.Same(probe.Failure, observed);
                ReliableInboxAcquireResult fresh = await inbox.AcquireAsync(key, probe.TakeoverAt, TimeSpan.FromMinutes(1), token);
                Assert.Equal(ReliableInboxAcquireDisposition.Acquired, fresh.Disposition);
                Assert.Equal(1, fresh.Attempt);
                Assert.True(await inbox.CompleteAsync(key, Assert.IsType<ReliableInboxLease>(fresh.Lease), probe.TakeoverAt, token));
            }
            else
            {
                InvalidOperationException failure = Assert.IsType<InvalidOperationException>(observed);
                Assert.NotSame(probe.Failure, failure);
                Assert.Contains(key.ToString(), failure.Message, StringComparison.Ordinal);
                ReliableInboxAcquireResult busy = await inbox.AcquireAsync(key, probe.TakeoverAt, TimeSpan.FromMinutes(1), token);
                Assert.Equal(ReliableInboxAcquireDisposition.Busy, busy.Disposition);
                Assert.Equal(2, busy.Attempt);
                Assert.True(await inbox.CompleteAsync(key, Assert.IsType<ReliableInboxLease>(probe.CurrentLease), probe.TakeoverAt, token));
            }

            Assert.Equal(ReliableInboxAcquireDisposition.AlreadyConsumed,
                (await inbox.AcquireAsync(key, probe.TakeoverAt.AddMinutes(2), TimeSpan.FromMinutes(1), token)).Disposition);
            ReliableInboxAcquireResult retainedNeighbor = await inbox.AcquireAsync(neighbor, probe.TakeoverAt, TimeSpan.FromMinutes(1), token);
            Assert.Equal(ReliableInboxAcquireDisposition.AlreadyConsumed, retainedNeighbor.Disposition);
            Assert.Equal(1, retainedNeighbor.Attempt);
            Assert.Empty((await inbox.GetQuarantineAsync(new ReliableInboxQuarantineQuery(), token)).Entries);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Empty(observation.Events);
        Assert.Empty(harness.Consumed.Snapshot<ReliableEvent>());
        DurableSendStoreSnapshot snapshot = await provider.GetRequiredService<IOutboxStore<IBus>>().GetSnapshotAsync(token);
        Assert.Equal((0, 0L), (snapshot.StoredCount, snapshot.StoredBytes));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [RequirementCoverage("REQ-VSB-RELIABLE-INBOX", "active-cancellation-preserves-token-without-retry-quarantine-or-outbox-delivery")]
    public async Task ConsumerCancellation_PreservesTheOriginalTokenWithoutRetryQuarantineOrOutboxDeliveryAsync(int maximumAttempts)
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var cancellation = new CancellationTokenSource();
        var probe = new CancellationProbe(cancellation);
        var observation = new ReliableObservation();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(probe)
            .AddSingleton(observation)
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                ConfigureReliableMessaging(configuration, maximumAttempts);
                configuration.AddConsumer<CancellationConsumer>();
                configuration.AddConsumer<ReliableEventConsumer>();
            })
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: cancellationToken)
            .WaitAsync(timeout, cancellationToken);
        Guid messageId = NewId.NextGuid();
        try
        {
            await harness.Bus.PublishAsync(new ReliableCommand(messageId, 0),
                send => send.MessageId = messageId, cancellationToken);
            Exception? observed = await probe.Completed.Task.WaitAsync(timeout, cancellationToken);
            OperationCanceledException failure = Assert.IsType<OperationCanceledException>(observed);
            Assert.Same(probe.Failure, failure);
            Assert.Equal(cancellation.Token, failure.CancellationToken);
            Assert.Equal(1, probe.PreparedMessages);
            IInboxStore<IBus> inbox = provider.GetRequiredService<IInboxStore<IBus>>();
            ReliableInboxAcquireResult retained = await inbox.AcquireAsync(
                new ReliableInboxKey(messageId, probe.ConsumerId), probe.StartedAt,
                TimeSpan.FromMinutes(1), cancellationToken);
            Assert.Equal(ReliableInboxAcquireDisposition.Busy, retained.Disposition);
            Assert.Equal(1, retained.Attempt);
            Assert.Empty((await inbox.GetQuarantineAsync(new ReliableInboxQuarantineQuery(), cancellationToken)).Entries);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Empty(observation.Events);
        Assert.Empty(harness.Consumed.Snapshot<ReliableEvent>());
        DurableSendStoreSnapshot snapshot = await provider.GetRequiredService<IOutboxStore<IBus>>()
            .GetSnapshotAsync(cancellationToken);
        Assert.Equal(0, snapshot.StoredCount);
        Assert.Equal(0, snapshot.StoredBytes);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RELIABLE-INBOX", "concurrent-duplicate-message-id-executes-body-once")]
    public async Task InboxLock_AllowsExactlyOneOfThreeConcurrentDeliveriesToPublishTheHundredEventsAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observation = new InboxObservation();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(observation)
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                ConfigureReliableMessaging(configuration);
                configuration.AddConsumer<InboxConsumer, InboxConsumerDefinition>();
                configuration.AddConsumer<InboxEventConsumer>();
            })
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
        Guid messageId = NewId.NextGuid();

        try
        {
            Task[] deliveries = Enumerable.Range(0, 3)
                .Select(_ => harness.Bus.PublishAsync(
                    new InboxCommand(),
                    context => context.MessageId = messageId,
                    cancellationToken))
                .ToArray();
            await Task.WhenAll(deliveries).WaitAsync(timeout, cancellationToken);
            await observation.AllEvents.Task.WaitAsync(timeout, cancellationToken);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Equal(1, observation.ConsumerExecutions);
        Assert.Equal(100, observation.Events.Count);
        Assert.Equal(Enumerable.Range(0, 100).Select(index => $"{index:0000}"), observation.Events.Keys.Order());
        Assert.All(observation.Events.Values, count => Assert.Equal(1, count));
        Assert.Equal(100, harness.Consumed.Snapshot<InboxEvent>().Count());
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    [RequirementCoverage("REQ-VSB-RELIABLE-CONSUMER", "outbox-exactly-once-across-success-and-first-attempt-retry")]
    public async Task ConsumerOutbox_PublishesBothScopedEventsExactlyOnceWithTheirRoutingKeysAsync(
        int failuresBeforeSuccess,
        int expectedAttempts)
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observation = new ReliableObservation();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(observation)
            .AddScoped<IReliablePublisher, ReliablePublisher>()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                ConfigureReliableMessaging(configuration);
                configuration.AddConsumer<ReliableConsumer, ReliableConsumerDefinition>();
                configuration.AddConsumer<ReliableEventConsumer>();
            })
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
        Guid messageId = NewId.NextGuid();

        try
        {
            await harness.Bus.PublishAsync(
                new ReliableCommand(messageId, failuresBeforeSuccess),
                context => context.MessageId = messageId,
                cancellationToken);
            await observation.BothEvents.Task.WaitAsync(timeout, cancellationToken);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Equal(expectedAttempts, observation.ConsumerAttempts);
        Assert.Equal(new[] { "First", "Second" }, observation.Events.Keys.Order());
        Assert.All(observation.Events.Values, count => Assert.Equal(1, count));
        Assert.Equal(new[] { "alpha", "beta" }, observation.RoutingKeys.Order());
        ReliableEvent[] events = harness.Consumed.Snapshot<ReliableEvent>()
            .Select(message => message.Context.Message)
            .Where(message => message.MessageId == messageId)
            .ToArray();
        Assert.Equal(2, events.Length);
        Assert.Single(events, message => message.Text == "First");
        Assert.Single(events, message => message.Text == "Second");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RELIABLE-INBOX", "repeated-failures-advance-attempts-and-reach-quarantine")]
    public async Task ConsumerInbox_RepeatedFailuresAdvanceAttemptsAndReachQuarantineAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observation = new ReliableObservation();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(observation)
            .AddScoped<IReliablePublisher, ReliablePublisher>()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout);
                ConfigureReliableMessaging(configuration);
                configuration.AddConsumer<ReliableConsumer, ReliableConsumerDefinition>();
                configuration.AddConsumer<ReliableEventConsumer>();
            })
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: cancellationToken)
            .WaitAsync(timeout, cancellationToken);
        Guid messageId = NewId.NextGuid();

        try
        {
            await harness.Bus.PublishAsync(
                new ReliableCommand(messageId, int.MaxValue),
                context => context.MessageId = messageId,
                cancellationToken);
            await harness.InactivityTask.WaitAsync(timeout, cancellationToken);

            IInboxStore<IBus> inbox = provider.GetRequiredService<IInboxStore<IBus>>();
            ReliableInboxQuarantineEntry quarantined = Assert.Single((await inbox.GetQuarantineAsync(
                new ReliableInboxQuarantineQuery { PageSize = 1 },
                cancellationToken)).Entries);
            Assert.Equal(messageId, quarantined.Key.MessageId);
            Assert.Equal(ReliableInboxStatus.Quarantined, quarantined.Status);
            Assert.Equal(3, quarantined.Attempts);
            Assert.Equal(typeof(ExpectedReliableException).FullName, quarantined.FailureType);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Equal(3, observation.ConsumerAttempts);
        Assert.Empty(observation.Events);
        Assert.Empty(harness.Published.Snapshot<Fault<ReliableCommand>>());
    }

    [Theory]
    [InlineData(ReliableSagaFailure.None, 1)]
    [InlineData(ReliableSagaFailure.FirstConsumeAttempt, 2)]
    [InlineData(ReliableSagaFailure.FirstDeliveryAttempt, 1)]
    [RequirementCoverage("REQ-VSB-RELIABLE-SAGA", "success-consume-retry-and-delivery-redelivery-reach-verified")]
    public async Task SagaOutbox_ReachesVerifiedWithOneCommittedStateMessageAsync(
        ReliableSagaFailure failure,
        int expectedCreateAttempts)
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observation = new ReliableObservation();
        var services = new ServiceCollection();
        services.AddSingleton(observation);
        services.AddSingleton<ITransportSendFailureClassifier, ExpectedReliableFailureClassifier>();
        services.AddViciOneServiceBusTestHarness(configuration =>
        {
            configuration.SetTestTimeouts(timeout, timeout);
            ConfigureReliableMessaging(configuration);
            configuration.AddSagaStateMachine<ReliableMachine, ReliableState, ReliableStateDefinition>()
                .InMemoryRepository();
        });
        await using ServiceProvider provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
        ISagaStateMachineTestHarness<ReliableMachine, ReliableState> sagaHarness =
            harness.GetSagaStateMachineHarness<ReliableMachine, ReliableState>();
        Guid correlationId = NewId.NextGuid();
        Guid messageId = NewId.NextGuid();
        using ConnectHandle? deliveryFailure = failure == ReliableSagaFailure.FirstDeliveryAttempt
            ? harness.Bus.ConnectSendObserver(new FailFirstReliableStateVerifiedSendObserver(observation))
            : null;

        try
        {
            await harness.Bus.PublishAsync(
                new CreateReliableState(correlationId, failure),
                context => context.MessageId = messageId,
                cancellationToken);
            Assert.Equal(correlationId, await sagaHarness.WaitForSagaInStateAsync(correlationId, state => state.Verified, timeout, TestContext.Current.CancellationToken));
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Equal(expectedCreateAttempts, observation.SagaCreateAttempts);
        Assert.Equal(failure == ReliableSagaFailure.FirstDeliveryAttempt ? 1 : 0, observation.SagaDeliveryFailures);
        Assert.Single(harness.Consumed.Snapshot<ReliableStateVerified>());
        Assert.Empty(harness.Published.Snapshot<Fault<CreateReliableState>>());
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions().OperationTimeout!.Value;

    private static void ConfigureReliableMessaging(IBusRegistrationConfigurator configuration, int maximumAttempts = 3, MessageLimits? limits = null)
    {
        configuration.Limits(limits ?? MessageLimits.Conservative);
        configuration.UseReliableMessaging(reliable =>
        {
            reliable.UseInMemoryStore();
            reliable.Store(new ReliableStoreLimits
            {
                MaximumStoredCount = 1_000,
                MaximumStoredBytes = 16 * 1024 * 1024,
            });
            reliable.Delivery(delivery =>
            {
                delivery.MaximumAttempts = maximumAttempts;
                delivery.InitialRetryDelay = TimeSpan.FromMilliseconds(20);
                delivery.MaximumRetryDelay = TimeSpan.FromMilliseconds(50);
                delivery.RetryJitterFraction = 0;
                delivery.PollInterval = TimeSpan.FromMilliseconds(10);
            });
            reliable.Retention(TimeSpan.FromDays(1));
            reliable.AddMessageContract<InboxCommand>("inbox-command");
            reliable.AddMessageContract<InboxEvent>("inbox-event");
            reliable.AddMessageContract<ReliableCommand>("reliable-command");
            reliable.AddMessageContract<ReliableEvent>("reliable-event");
            reliable.AddMessageContract<SerializationCancellationEvent>("serialization-cancellation-event");
            reliable.AddMessageContract<CreateReliableState>("create-reliable-state");
            reliable.AddMessageContract<ReliableStateVerified>("reliable-state-verified");
        });
    }


    public sealed class AdmissionRecoveryProbe
    {
        public Guid ConsumerId { get; } = Guid.NewGuid();
        public Guid OutgoingId { get; } = Guid.NewGuid();
        public PayloadAdmissionException? Rejected { get; set; }
        public int PreparedMessages { get; set; }
        public TaskCompletionSource<Exception?> FirstCompleted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Recover { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<Exception?> Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public sealed class AdmissionRecoveryConsumer(IServiceProvider provider, AdmissionRecoveryProbe probe) : IConsumer<ReliableCommand>
    {
        public async Task ConsumeAsync(ConsumeContext<ReliableCommand> context)
        {
            try
            {
                await InMemoryInboxPipelineTestDriver.SendAsync(provider, context, probe.ConsumerId, async inner =>
                {
                    await PrepareAsync(inner, "staged-before-rejection");
                    try
                    {
                        await inner.Advanced().PublishAsync(new ReliableEvent(context.Message.MessageId, new string('x', 4096)),
                            context.CancellationToken);
                    }
                    catch (PayloadAdmissionException failure)
                    {
                        probe.Rejected = failure;
                        throw;
                    }
                }, context.CancellationToken, completeConsumer: true);
                probe.FirstCompleted.TrySetResult(null);
                await probe.Recover.Task.WaitAsync(context.CancellationToken);
                await InMemoryInboxPipelineTestDriver.SendAsync(provider, context, probe.ConsumerId,
                    inner => PrepareAsync(inner, "recovered"), context.CancellationToken, completeConsumer: true);
                probe.Completed.TrySetResult(null);
            }
            catch (Exception failure)
            {
                probe.FirstCompleted.TrySetResult(failure);
                probe.Completed.TrySetResult(failure);
            }

            async Task PrepareAsync(ConsumeContext<ReliableCommand> inner, string text)
            {
                await inner.Advanced().PublishAsync(new ReliableEvent(context.Message.MessageId, text), send =>
                {
                    send.MessageId = probe.OutgoingId;
                    send.Delay = TimeSpan.FromDays(1);
                }, context.CancellationToken);
                probe.PreparedMessages++;
            }
        }
    }

    public sealed class DeliveryCancellationContext(
        ConsumeContext<ReliableCommand> context,
        CancellationToken cancellationToken) : ConsumeContextProxy<ReliableCommand>(context)
    {
        public override CancellationToken CancellationToken => cancellationToken;
    }

    private sealed class FixedInboxClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    public sealed class DelayedCommitProbe
    {
        public Guid ConsumerId { get; } = Guid.NewGuid();
        public Guid OutgoingId { get; } = Guid.NewGuid();
        public Guid CorrelationId { get; } = Guid.NewGuid();
        public TimeSpan Delay { get; } = TimeSpan.FromDays(1);
        public TaskCompletionSource<Exception?> Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public sealed class DelayedCommitConsumer(IServiceProvider provider, DelayedCommitProbe probe) : IConsumer<ReliableCommand>
    {
        public async Task ConsumeAsync(ConsumeContext<ReliableCommand> context)
        {
            Exception? observed = null;
            try
            {
                await InMemoryInboxPipelineTestDriver.SendAsync(provider, context, probe.ConsumerId,
                    inner => inner.Advanced().PublishAsync(new ReliableEvent(context.Message.MessageId, "delayed-buffer"), send =>
                    {
                        send.MessageId = probe.OutgoingId;
                        send.CorrelationId = probe.CorrelationId;
                        send.Delay = probe.Delay;
                    }, context.CancellationToken), context.CancellationToken, completeConsumer: true);
            }
            catch (Exception exception)
            {
                observed = exception;
            }
            probe.Completed.TrySetResult(observed);
        }
    }

    public sealed class OwnershipFailureProbe(bool remove)
    {
        public bool Remove { get; } = remove;
        public Guid ConsumerId { get; } = Guid.NewGuid();
        public DateTimeOffset TakeoverAt { get; set; }
        public ReliableInboxLease? CurrentLease { get; set; }
        public int PreparedMessages { get; set; }
        public ExpectedReliableException Failure { get; } = new("failure after ownership changed");
        public TaskCompletionSource<Exception?> Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public sealed class OwnershipFailureConsumer(IServiceProvider provider, OwnershipFailureProbe probe) : IConsumer<ReliableCommand>
    {
        public async Task ConsumeAsync(ConsumeContext<ReliableCommand> context)
        {
            Exception? observed = null;
            try
            {
                await InMemoryInboxPipelineTestDriver.SendAsync(provider, context, probe.ConsumerId, async inner =>
                {
                    await inner.Advanced().PublishAsync(new ReliableEvent(context.Message.MessageId, "uncommitted-owner"),
                        context.CancellationToken);
                    probe.PreparedMessages++;
                    IInboxStore<IBus> inbox = provider.GetRequiredService<IInboxStore<IBus>>();
                    var key = new ReliableInboxKey(context.MessageId!.Value, probe.ConsumerId);
                    probe.TakeoverAt = DateTimeOffset.UtcNow.AddDays(1);
                    ReliableInboxAcquireResult takeover = await inbox.AcquireAsync(key, probe.TakeoverAt,
                        TimeSpan.FromMinutes(1), context.CancellationToken);
                    Assert.Equal(ReliableInboxAcquireDisposition.Acquired, takeover.Disposition);
                    Assert.Equal(2, takeover.Attempt);
                    ReliableInboxLease currentLease = Assert.IsType<ReliableInboxLease>(takeover.Lease);
                    probe.CurrentLease = currentLease;
                    if (probe.Remove)
                    {
                        Assert.True(await inbox.QuarantineAsync(key, currentLease, "Tests.OperatorRemoval",
                            probe.TakeoverAt, context.CancellationToken));
                        ReliableMessagingOperationResult discarded = await inbox.DiscardAsync(key, context.CancellationToken);
                        Assert.Equal(new ReliableMessagingOperationResult(ReliableMessageReference.Inbox(key),
                            ReliableMessagingOperationDisposition.Applied, "Quarantined", "Discarded"), discarded);
                    }
                    throw probe.Failure;
                }, context.CancellationToken);
            }
            catch (Exception exception)
            {
                observed = exception;
            }
            probe.Completed.TrySetResult(observed);
        }
    }

    public sealed class CancellationProbe(CancellationTokenSource cancellation)
    {
        public CancellationTokenSource Cancellation { get; } = cancellation;
        public Guid ConsumerId { get; } = Guid.NewGuid();
        public DateTimeOffset StartedAt { get; set; }
        public int PreparedMessages { get; set; }
        public OperationCanceledException Failure { get; } = new(cancellation.Token);
        public TaskCompletionSource<Exception?> Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public sealed class DistinctDeliveryCancellationProbe(bool cancelBeforePublish)
    {
        public bool CancelBeforePublish { get; } = cancelBeforePublish;
        public CancellationTokenSource Delivery { get; } = new();
        public CancellationTokenSource Operation { get; } = new();
        public Guid ConsumerId { get; } = NewId.NextGuid();
        public Guid OutgoingId { get; } = NewId.NextGuid();
        public int BufferedMessages { get; set; }
        public TaskCompletionSource<Exception?> Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public sealed class SerializationCancellationProbe(bool cancelDelivery)
    {
        public bool CancelDelivery { get; } = cancelDelivery;
        public CancellationTokenSource Delivery { get; } = new();
        public CancellationTokenSource Operation { get; } = new();
        public Guid ConsumerId { get; } = NewId.NextGuid();
        public Guid OutgoingId { get; } = NewId.NextGuid();
        public int BufferedMessages { get; set; }
        public int SerializationCalls;
        public TaskCompletionSource<Exception?> Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public sealed class SerializationCancellationEvent(SerializationCancellationProbe probe)
    {
        [System.Text.Json.Serialization.JsonIgnore]
        public SerializationCancellationProbe Probe { get; } = probe;

        public string Text
        {
            get
            {
                Interlocked.Increment(ref Probe.SerializationCalls);
                if (Probe.CancelDelivery)
                    Probe.Delivery.Cancel();
                else
                    Probe.Operation.Cancel();
                return "cancel-during-serialization";
            }
        }
    }

    public sealed class SerializationCancellationConsumer(IServiceProvider provider,
        SerializationCancellationProbe probe) : IConsumer<ReliableCommand>
    {
        public async Task ConsumeAsync(ConsumeContext<ReliableCommand> context)
        {
            Exception? observed = null;
            try
            {
                var deliveryContext = new DeliveryCancellationContext(context, probe.Delivery.Token);
                await InMemoryInboxPipelineTestDriver.SendAsync(provider, deliveryContext, probe.ConsumerId, async inner =>
                {
                    await inner.Advanced().PublishAsync(new SerializationCancellationEvent(probe), send =>
                    {
                        send.MessageId = probe.OutgoingId;
                        send.Delay = TimeSpan.FromDays(1);
                    }, probe.Operation.Token);
                    probe.BufferedMessages++;
                }, probe.Operation.Token, completeConsumer: true);
            }
            catch (Exception exception)
            {
                observed = exception;
            }
            probe.Completed.TrySetResult(observed);
        }
    }

    public sealed class DistinctDeliveryCancellationConsumer(
        IServiceProvider provider,
        DistinctDeliveryCancellationProbe probe) : IConsumer<ReliableCommand>
    {
        public async Task ConsumeAsync(ConsumeContext<ReliableCommand> context)
        {
            Exception? observed = null;
            try
            {
                var deliveryContext = new DeliveryCancellationContext(context, probe.Delivery.Token);
                await InMemoryInboxPipelineTestDriver.SendAsync(provider, deliveryContext, probe.ConsumerId, async inner =>
                {
                    if (probe.CancelBeforePublish)
                        probe.Delivery.Cancel();
                    await inner.Advanced().PublishAsync(
                        new ReliableEvent(context.Message.MessageId, "canceled-delivery"), send =>
                        {
                            send.MessageId = probe.OutgoingId;
                            send.Delay = TimeSpan.FromDays(1);
                        }, probe.Operation.Token);
                    probe.BufferedMessages++;
                    if (!probe.CancelBeforePublish)
                        probe.Delivery.Cancel();
                }, probe.Operation.Token, completeConsumer: true);
            }
            catch (Exception exception)
            {
                observed = exception;
            }
            probe.Completed.TrySetResult(observed);
        }
    }

    public sealed class CancellationConsumer(IServiceProvider provider, CancellationProbe probe) : IConsumer<ReliableCommand>
    {
        public async Task ConsumeAsync(ConsumeContext<ReliableCommand> context)
        {
            Exception? observed = null;
            try
            {
                probe.StartedAt = DateTimeOffset.UtcNow;
                await InMemoryInboxPipelineTestDriver.SendAsync(provider, context, probe.ConsumerId, async inner =>
                {
                    await inner.Advanced().PublishAsync(new ReliableEvent(context.Message.MessageId, "canceled-intent"),
                        probe.Cancellation.Token);
                    probe.PreparedMessages++;
                    probe.Cancellation.Cancel();
                    throw probe.Failure;
                }, probe.Cancellation.Token);
            }
            catch (Exception exception)
            {
                observed = exception;
            }
            probe.Completed.TrySetResult(observed);
        }
    }

    public sealed class InboxObservation
    {
        int _consumerExecutions;

        public int ConsumerExecutions => Volatile.Read(ref _consumerExecutions);

        public ConcurrentDictionary<string, int> Events { get; } = new(StringComparer.Ordinal);

        public TaskCompletionSource AllEvents { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void ConsumerExecuted() => Interlocked.Increment(ref _consumerExecutions);

        public void EventReceived(string text)
        {
            Events.AddOrUpdate(text, 1, static (_, count) => count + 1);
            if (Events.Count == 100)
                AllEvents.TrySetResult();
        }
    }

    public sealed class ReliableObservation
    {
        int _consumerAttempts;
        int _sagaCreateAttempts;
        int _sagaDeliveryFailures;

        public int ConsumerAttempts => Volatile.Read(ref _consumerAttempts);

        public int SagaCreateAttempts => Volatile.Read(ref _sagaCreateAttempts);

        public int SagaDeliveryFailures => Volatile.Read(ref _sagaDeliveryFailures);

        public ConcurrentDictionary<string, int> Events { get; } = new(StringComparer.Ordinal);

        public ConcurrentBag<string> RoutingKeys { get; } = [];

        public TaskCompletionSource BothEvents { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int ConsumerAttempt() => Interlocked.Increment(ref _consumerAttempts);

        public int SagaCreateAttempt() => Interlocked.Increment(ref _sagaCreateAttempts);

        public bool TryInjectSagaDeliveryFailure() =>
            Interlocked.CompareExchange(ref _sagaDeliveryFailures, 1, 0) == 0;

        public void EventReceived(string text, string routingKey)
        {
            Events.AddOrUpdate(text, 1, static (_, count) => count + 1);
            RoutingKeys.Add(routingKey);
            if (Events.Count == 2)
                BothEvents.TrySetResult();
        }
    }

    public sealed record InboxCommand;

    public sealed record InboxEvent(Guid MessageId, string Text);

    public sealed class InboxConsumer(InboxObservation observation) : IConsumer<InboxCommand>
    {
        public Task ConsumeAsync(ConsumeContext<InboxCommand> context)
        {
            observation.ConsumerExecuted();
            return Task.WhenAll(Enumerable.Range(0, 100).Select(index => context.Advanced().PublishAsync(
                new InboxEvent(context.MessageId!.Value, $"{index:0000}"),
                context.CancellationToken)));
        }
    }

    public sealed class InboxEventConsumer(InboxObservation observation) : IConsumer<InboxEvent>
    {
        public Task ConsumeAsync(ConsumeContext<InboxEvent> context)
        {
            observation.EventReceived(context.Message.Text);
            return Task.CompletedTask;
        }
    }

    public sealed class InboxConsumerDefinition : ConsumerDefinition<InboxConsumer>
    {
        protected override void ConfigureConsumer(
            IReceiveEndpointConfigurator endpointConfigurator,
            IConsumerConfigurator<InboxConsumer> consumerConfigurator,
            IRegistrationContext context)
        {
            _ = endpointConfigurator;
            _ = consumerConfigurator;
            _ = context;
        }
    }

    public sealed record ReliableCommand(Guid MessageId, int FailuresBeforeSuccess);

    public sealed record ReliableEvent(Guid MessageId, string Text);

    public interface IReliablePublisher
    {
        Task PublishSecondAsync(Guid messageId, CancellationToken cancellationToken);
    }

    public sealed class ReliablePublisher(IPublishEndpoint publishEndpoint) : IReliablePublisher
    {
        public Task PublishSecondAsync(Guid messageId, CancellationToken cancellationToken) => publishEndpoint.PublishAsync(
            new ReliableEvent(messageId, "Second"),
            context => context.SetRoutingKey("beta"),
            cancellationToken);
    }

    public sealed class ReliableConsumer(
        ReliableObservation observation,
        IReliablePublisher publisher) : IConsumer<ReliableCommand>
    {
        public async Task ConsumeAsync(ConsumeContext<ReliableCommand> context)
        {
            int attempt = observation.ConsumerAttempt();
            await context.Advanced().PublishAsync(
                new ReliableEvent(context.Message.MessageId, "First"),
                publish => publish.SetRoutingKey("alpha"));
            await publisher.PublishSecondAsync(context.Message.MessageId, context.CancellationToken);
            if (attempt <= context.Message.FailuresBeforeSuccess)
                throw new ExpectedReliableException("expected consumer failure");
        }
    }

    public sealed class ReliableEventConsumer(ReliableObservation observation) : IConsumer<ReliableEvent>
    {
        public Task ConsumeAsync(ConsumeContext<ReliableEvent> context)
        {
            observation.EventReceived(context.Message.Text, context.Advanced().GetRoutingKey() ?? string.Empty);
            return Task.CompletedTask;
        }
    }

    public sealed class ReliableConsumerDefinition : ConsumerDefinition<ReliableConsumer>
    {
        protected override void ConfigureConsumer(
            IReceiveEndpointConfigurator endpointConfigurator,
            IConsumerConfigurator<ReliableConsumer> consumerConfigurator,
            IRegistrationContext context)
        {
            _ = endpointConfigurator;
            _ = consumerConfigurator;
            _ = context;
        }
    }

    public enum ReliableSagaFailure
    {
        None,
        FirstConsumeAttempt,
        FirstDeliveryAttempt,
    }

    public sealed record CreateReliableState(Guid CorrelationId, ReliableSagaFailure Failure) : ICorrelatedBy<Guid>;

    public sealed record ReliableStateVerified(Guid CorrelationId) : ICorrelatedBy<Guid>;

    public sealed class ReliableState : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = string.Empty;
    }

    public sealed class ReliableMachine : ViciOneServiceBusStateMachine<ReliableState>
    {
        public ReliableMachine(ReliableObservation observation)
        {
            InstanceState(instance => instance.CurrentState);
            Event(() => Create, configuration =>
            {
                configuration.CorrelateById(context => context.Message.CorrelationId);
                configuration.SelectId(context => context.Message.CorrelationId);
                configuration.InsertOnInitial = true;
            });
            Initially(When(Create)
                .Then(context =>
                {
                    int attempt = observation.SagaCreateAttempt();
                    if (context.Message.Failure == ReliableSagaFailure.FirstConsumeAttempt && attempt == 1)
                        throw new ExpectedReliableException("first saga consume attempt");
                })
                .TransitionTo(Created)
                .Send(
                    context => context.ReceiveContext.InputAddress,
                    context => new ReliableStateVerified(context.Saga.CorrelationId)));
            During(Created, When(VerifiedEvent).TransitionTo(Verified));
        }

        public IState Created { get; private set; } = null!;

        public IState Verified { get; private set; } = null!;

        public IEvent<CreateReliableState> Create { get; private set; } = null!;

        public IEvent<ReliableStateVerified> VerifiedEvent { get; private set; } = null!;
    }

    public sealed class ReliableStateDefinition : SagaDefinition<ReliableState>
    {
        protected override void ConfigureSaga(
            IReceiveEndpointConfigurator endpointConfigurator,
            ISagaConfigurator<ReliableState> sagaConfigurator,
            IRegistrationContext context)
        {
            endpointConfigurator.UseMessageScope(context);
            _ = sagaConfigurator;
        }
    }

    private sealed class FailFirstReliableStateVerifiedSendObserver(ReliableObservation observation) : ISendObserver
    {
        public Task PreSendAsync<T>(SendContext<T> context)
            where T : class
        {
            if (context.SupportedMessageTypes.Contains(
                    MessageUrn.ForTypeString<ReliableStateVerified>(),
                    StringComparer.Ordinal)
                && observation.TryInjectSagaDeliveryFailure())
                return Task.FromException(new ExpectedReliableException("first saga delivery attempt"));

            return Task.CompletedTask;
        }

        public Task PostSendAsync<T>(SendContext<T> context)
            where T : class => Task.CompletedTask;

        public Task SendFaultAsync<T>(SendContext<T> context, Exception exception)
            where T : class => Task.CompletedTask;
    }

    public sealed class ExpectedReliableException(string message) : Exception(message);

    private sealed class ExpectedReliableFailureClassifier : ITransportSendFailureClassifier
    {
        public bool TryClassify(Exception exception, out TransportSendFailureKind kind)
        {
            ArgumentNullException.ThrowIfNull(exception);
            for (Exception? current = exception; current is not null; current = current.InnerException)
            {
                if (current is not ExpectedReliableException)
                    continue;

                kind = TransportSendFailureKind.Transient;
                return true;
            }

            kind = TransportSendFailureKind.Unclassified;
            return false;
        }
    }
}
