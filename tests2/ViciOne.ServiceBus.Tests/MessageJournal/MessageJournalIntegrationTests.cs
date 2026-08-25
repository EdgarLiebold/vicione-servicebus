using System.Collections.Concurrent;
using System.Text.Json;
using ViciOne.ServiceBus.MessageJournal;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.MessageJournal;

public sealed class MessageJournalIntegrationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-JOURNAL-ACTIVATION", "default-off-has-no-observer-or-store-work")]
    public async Task WithoutExplicitConnection_TheJournalPerformsNoWork()
    {
        TimeSpan timeout = OperationTimeout;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness("journal-default-off", timeout);
        HandlerTestHarness<JournalMessage> handler = harness.Handler<JournalMessage>();
        var store = new RecordingStore(expectedEntries: 1);

        await harness.Start(cancellationToken);
        try
        {
            var message = new JournalMessage(NewId.NextGuid(), "not-journaled");

            await harness.InputQueueSendEndpoint.Send(message, cancellationToken);
            await handler.Consumed.SelectAsync(cancellationToken).First();

            Assert.Equal(0, store.AppendAttempts);
            Assert.Empty(store.Entries);
            Assert.False(store.ExpectedEntriesReached.IsCompleted);
        }
        finally
        {
            await harness.Stop().WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-JOURNAL-ACTIVATION", "explicit-outgoing-connection")]
    public async Task ExplicitOutgoingConnection_RecordsSendAndPublishTerminalEnvelopesExactlyOnce()
    {
        TimeSpan timeout = OperationTimeout;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness("journal-outgoing", timeout);
        harness.Handler<JournalMessage>();
        var store = new RecordingStore(expectedEntries: 2);

        await harness.Start(cancellationToken);
        try
        {
            using ConnectHandle journal = harness.Bus.ConnectOutgoingMessageJournal(
                store,
                PassThroughPolicy(),
                Options(timeout));
            var sent = new JournalMessage(NewId.NextGuid(), "sent-value");
            var published = new JournalMessage(NewId.NextGuid(), "published-value");

            await harness.InputQueueSendEndpoint.Send(sent, cancellationToken);
            await harness.Bus.Publish(published, cancellationToken);
            await store.ExpectedEntriesReached.WaitAsync(timeout, cancellationToken);

            MessageJournalEntry[] entries = store.Entries;
            Assert.Equal(2, entries.Length);
            Assert.Equal(
                [MessageJournalOperation.Send, MessageJournalOperation.Publish],
                entries.Select(entry => entry.Operation).Order());
            Assert.All(entries, entry => Assert.Equal(MessageJournalOutcome.Succeeded, entry.Outcome));
            AssertEnvelope(entries.Single(entry => entry.Operation == MessageJournalOperation.Send), sent);
            AssertEnvelope(entries.Single(entry => entry.Operation == MessageJournalOperation.Publish), published);
        }
        finally
        {
            await harness.Stop().WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-JOURNAL-OUTCOME", "consume-success-and-fault-are-terminal")]
    public async Task ConsumeJournal_RecordsTheExactTerminalSuccessAndFaultOutcomes()
    {
        TimeSpan timeout = OperationTimeout;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var expectedFailure = new ExpectedConsumerException();
        using var harness = CreateHarness("journal-consume-outcomes", timeout);
        harness.Handler<SuccessfulMessage>();
        harness.Handler<FaultingMessage>(_ => Task.FromException(expectedFailure));
        var store = new RecordingStore(expectedEntries: 2);

        await harness.Start(cancellationToken);
        try
        {
            using ConnectHandle journal = harness.Bus.ConnectConsumeMessageJournal(
                store,
                PassThroughPolicy(),
                Options(timeout));
            var successful = new SuccessfulMessage(NewId.NextGuid());
            var faulting = new FaultingMessage(NewId.NextGuid());

            await harness.InputQueueSendEndpoint.Send(successful, cancellationToken);
            await harness.InputQueueSendEndpoint.Send(faulting, cancellationToken);
            IPublishedMessage<Fault<FaultingMessage>> publishedFault = await harness.Published
                .SelectAsync<Fault<FaultingMessage>>(cancellationToken)
                .First();
            await store.ExpectedEntriesReached.WaitAsync(timeout, cancellationToken);

            MessageJournalEntry[] entries = store.Entries;
            MessageJournalEntry succeeded = Assert.Single(entries, entry =>
                entry.Metadata[MessageJournalMetadataKeys.CorrelationId] == successful.CorrelationId.ToString("D"));
            MessageJournalEntry faulted = Assert.Single(entries, entry =>
                entry.Metadata[MessageJournalMetadataKeys.CorrelationId] == faulting.CorrelationId.ToString("D"));
            Assert.Equal(MessageJournalOutcome.Succeeded, succeeded.Outcome);
            Assert.Equal(MessageJournalOutcome.Faulted, faulted.Outcome);
            Assert.Equal(TypeCache<ExpectedConsumerException>.ShortName,
                faulted.Metadata[MessageJournalMetadataKeys.FailureType]);
            Assert.Contains(publishedFault.Context.Message.Exceptions,
                exception => exception.ExceptionType == TypeCache<ExpectedConsumerException>.ShortName);
        }
        finally
        {
            await harness.Stop().WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-JOURNAL-FAILURE-ISOLATION", "store-failure-does-not-change-delivery")]
    public async Task StoreFailure_DoesNotChangeASuccessfulMessageDelivery()
    {
        TimeSpan timeout = OperationTimeout;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness("journal-store-failure", timeout);
        HandlerTestHarness<JournalMessage> handler = harness.Handler<JournalMessage>();
        var store = new RecordingStore(
            expectedEntries: 0,
            failure: new ExpectedStoreException(),
            expectedAttempts: 2);

        await harness.Start(cancellationToken);
        try
        {
            using ConnectHandle journal = harness.Bus.ConnectMessageJournal(
                store,
                PassThroughPolicy(),
                Options(timeout));
            var message = new JournalMessage(NewId.NextGuid(), "delivered");

            await harness.InputQueueSendEndpoint.Send(message, cancellationToken);
            IReceivedMessage<JournalMessage> consumed = await handler.Consumed
                .SelectAsync(cancellationToken)
                .First();
            await store.ExpectedAttemptsReached.WaitAsync(timeout, cancellationToken);

            Assert.Equal(message, consumed.Context.Message);
            Assert.Equal(2, store.AppendAttempts);
            Assert.Empty(store.Entries);
        }
        finally
        {
            await harness.Stop().WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-JOURNAL-OUTBOX", "deferred-message-uses-real-envelope-identity")]
    public async Task InMemoryOutbox_RecordsTheDeferredMessageEnvelopeInsteadOfAnInternalWrapper()
    {
        TimeSpan timeout = OperationTimeout;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness("journal-outbox-envelope", timeout);
        harness.OnConfigureInMemoryReceiveEndpoint += configurator =>
        {
            configurator.UseInMemoryOutbox();
            configurator.Handler<OutboxRequest>(context =>
                context.Publish(new DeferredMessage(context.Message.CorrelationId, "deferred")));
            configurator.Handler<DeferredMessage>(_ => Task.CompletedTask);
        };
        var store = new RecordingStore(expectedEntries: 1);
        IMessageJournalPolicy onlyDeferredMessages = new DelegatePolicy((capture, _) =>
        {
            if (!capture.MessageTypes.Any(type =>
                    type.Contains(nameof(DeferredMessage), StringComparison.Ordinal)))
                return ValueTask.FromResult<MessageJournalProjection?>(null);

            return ValueTask.FromResult<MessageJournalProjection?>(Project(capture));
        });

        await harness.Start(cancellationToken);
        try
        {
            using ConnectHandle journal = harness.Bus.ConnectOutgoingMessageJournal(
                store,
                onlyDeferredMessages,
                Options(timeout));
            var request = new OutboxRequest(NewId.NextGuid());

            await harness.InputQueueSendEndpoint.Send(request, cancellationToken);
            await store.ExpectedEntriesReached.WaitAsync(timeout, cancellationToken);

            MessageJournalEntry entry = Assert.Single(store.Entries);
            Assert.Contains(entry.MessageTypes,
                type => type.Contains(nameof(DeferredMessage), StringComparison.Ordinal));
            Assert.DoesNotContain(entry.MessageTypes,
                type => type.Contains(nameof(SerializedMessageBody), StringComparison.Ordinal));
            AssertEnvelope(entry, new DeferredMessage(request.CorrelationId, "deferred"));
        }
        finally
        {
            await harness.Stop().WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-JOURNAL-ACTIVATION", "disconnect-removes-all-journal-observers")]
    public async Task ConnectionHandle_DisconnectsEverySelectedJournalObserver()
    {
        TimeSpan timeout = OperationTimeout;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness("journal-disconnect", timeout);
        harness.Handler<JournalMessage>();
        var store = new RecordingStore(expectedEntries: 1);

        await harness.Start(cancellationToken);
        try
        {
            ConnectHandle journal = harness.Bus.ConnectOutgoingMessageJournal(
                store,
                PassThroughPolicy(),
                Options(timeout));
            await harness.InputQueueSendEndpoint.Send(
                new JournalMessage(NewId.NextGuid(), "before"),
                cancellationToken);
            await store.ExpectedEntriesReached.WaitAsync(timeout, cancellationToken);

            journal.Dispose();
            journal.Dispose();
            await harness.InputQueueSendEndpoint.Send(
                new JournalMessage(NewId.NextGuid(), "after"),
                cancellationToken);

            Assert.Single(store.Entries);
        }
        finally
        {
            await harness.Stop().WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static void AssertEnvelope<T>(MessageJournalEntry entry, T expected)
        where T : class
    {
        using JsonDocument document = JsonDocument.Parse(entry.Body);
        using JsonDocument expectedDocument = JsonDocument.Parse(
            JsonSerializer.Serialize(expected, SystemTextJsonMessageSerializer.Options));
        JsonElement message = document.RootElement.GetProperty("message");

        Assert.True(JsonElement.DeepEquals(expectedDocument.RootElement, message),
            $"Expected message {expectedDocument.RootElement.GetRawText()}, actual {message.GetRawText()}.");
        Assert.Contains(entry.MessageTypes,
            type => type.Contains(typeof(T).Name, StringComparison.Ordinal));
    }

    private static IMessageJournalPolicy PassThroughPolicy() => new DelegatePolicy(static (capture, _) =>
        ValueTask.FromResult<MessageJournalProjection?>(Project(capture)));

    private static MessageJournalProjection Project(MessageJournalCapture capture) => new(
        MessageJournalDataClassification.Internal,
        capture.ContentType,
        capture.MessageTypes,
        capture.Metadata,
        capture.Headers,
        capture.Body);

    private static MessageJournalOptions Options(TimeSpan timeout) =>
        MessageJournalOptions.ContinueMessageFlow(timeout, TimeProvider.System);

    private static InMemoryTestHarness CreateHarness(string name, TimeSpan timeout) =>
        new($"{name}-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };

    private static TimeSpan OperationTimeout => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private sealed class DelegatePolicy(
        Func<MessageJournalCapture, CancellationToken, ValueTask<MessageJournalProjection?>> project) : IMessageJournalPolicy
    {
        public ValueTask<MessageJournalProjection?> ProjectAsync(
            MessageJournalCapture capture,
            CancellationToken cancellationToken) => project(capture, cancellationToken);
    }

    private sealed class RecordingStore(
        int expectedEntries,
        Exception? failure = null,
        int? expectedAttempts = null) : IMessageJournalStore
    {
        private readonly ConcurrentQueue<MessageJournalEntry> _entries = new();
        private readonly TaskCompletionSource _expectedEntriesReached =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _expectedAttemptsReached =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _appendAttempts;

        public MessageJournalEntry[] Entries => _entries.ToArray();

        public int AppendAttempts => Volatile.Read(ref _appendAttempts);

        public Task ExpectedEntriesReached => _expectedEntriesReached.Task;

        public Task ExpectedAttemptsReached => _expectedAttemptsReached.Task;

        public MessageJournalStoreLimits Limits { get; } =
            new(1024 * 1024, maximumEntries: 100, retentionPeriod: TimeSpan.FromDays(1));

        public ValueTask AppendAsync(MessageJournalEntry entry, CancellationToken cancellationToken)
        {
            int attempts = Interlocked.Increment(ref _appendAttempts);
            if (expectedAttempts.HasValue && attempts >= expectedAttempts.Value)
                _expectedAttemptsReached.TrySetResult();
            cancellationToken.ThrowIfCancellationRequested();

            if (failure is not null)
                return ValueTask.FromException(failure);

            _entries.Enqueue(entry);
            if (_entries.Count >= expectedEntries)
                _expectedEntriesReached.TrySetResult();
            return ValueTask.CompletedTask;
        }
    }

    private sealed record JournalMessage(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;
    private sealed record SuccessfulMessage(Guid CorrelationId) : CorrelatedBy<Guid>;
    private sealed record FaultingMessage(Guid CorrelationId) : CorrelatedBy<Guid>;
    private sealed record OutboxRequest(Guid CorrelationId) : CorrelatedBy<Guid>;
    private sealed record DeferredMessage(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;
    private sealed class ExpectedConsumerException : Exception;
    private sealed class ExpectedStoreException : Exception;
}
