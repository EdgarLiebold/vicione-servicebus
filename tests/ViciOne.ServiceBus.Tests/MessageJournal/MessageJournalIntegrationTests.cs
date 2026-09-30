using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Serialization;
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
    public async Task WithoutExplicitConnection_TheJournalPerformsNoWorkAsync()
    {
        TimeSpan timeout = OperationTimeout;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness("journal-default-off", timeout);
        HandlerTestHarness<JournalMessage> handler = harness.AddHandler<JournalMessage>();
        var store = new RecordingStore(expectedEntries: 1);

        await harness.StartAsync(cancellationToken);
        try
        {
            var message = new JournalMessage(NewId.NextGuid(), "not-journaled");

            await harness.InputQueueSendEndpoint.SendAsync(message, cancellationToken);
            await handler.Consumed.SelectAsync(cancellationToken).FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(0, store.AppendAttempts);
            Assert.Empty(store.Entries);
            Assert.False(store.ExpectedEntriesReached.IsCompleted);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-JOURNAL-ACTIVATION", "explicit-outgoing-connection")]
    public async Task ExplicitOutgoingConnection_RecordsSendAndPublishTerminalEnvelopesExactlyOnceAsync()
    {
        TimeSpan timeout = OperationTimeout;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness("journal-outgoing", timeout);
        harness.AddHandler<JournalMessage>();
        var store = new RecordingStore(expectedEntries: 2);

        await harness.StartAsync(cancellationToken);
        try
        {
            using ConnectHandle journal = harness.Bus.ConnectOutgoingMessageJournal(
                store,
                PassThroughPolicy(),
                Options(timeout));
            var sent = new JournalMessage(NewId.NextGuid(), "sent-value");
            var published = new JournalMessage(NewId.NextGuid(), "published-value");
            Guid scheduledMessageId = NewId.NextGuid();
            TimeSpan timeToLive = TimeSpan.FromMinutes(7);

            await harness.InputQueueSendEndpoint.SendAsync(
                sent,
                Pipe.Execute<SendContext<JournalMessage>>(context =>
                {
                    context.ScheduledMessageId = scheduledMessageId;
                    context.TimeToLive = timeToLive;
                    context.Headers.Set("journal-test-number", 12.5m);
                }),
                cancellationToken);
            await harness.Bus.PublishAsync(published, cancellationToken);
            await store.ExpectedEntriesReached.WaitAsync(timeout, cancellationToken);

            MessageJournalEntry[] entries = store.Entries;
            Assert.Equal(2, entries.Length);
            Assert.Equal(
                [MessageJournalOperation.Send, MessageJournalOperation.Publish],
                entries.Select(entry => entry.Operation).Order());
            Assert.All(entries, entry => Assert.Equal(MessageJournalOutcome.Succeeded, entry.Outcome));
            MessageJournalEntry sentEntry = entries.Single(entry => entry.Operation == MessageJournalOperation.Send);
            AssertEnvelope(sentEntry, sent);
            AssertEnvelope(entries.Single(entry => entry.Operation == MessageJournalOperation.Publish), published);
            Assert.Equal(
                scheduledMessageId.ToString("D", CultureInfo.InvariantCulture),
                sentEntry.Metadata[MessageJournalMetadataKeys.ScheduledMessageId]);
            Assert.Equal(
                timeToLive.ToString("c", CultureInfo.InvariantCulture),
                sentEntry.Metadata[MessageJournalMetadataKeys.TimeToLive]);
            Assert.Equal("12.5", sentEntry.Headers["journal-test-number"]);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-JOURNAL-OUTCOME", "consume-success-and-fault-are-terminal")]
    public async Task ConsumeJournal_RecordsTheExactTerminalSuccessAndFaultOutcomesAsync()
    {
        TimeSpan timeout = OperationTimeout;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var expectedFailure = new ExpectedConsumerException();
        using var harness = CreateHarness("journal-consume-outcomes", timeout);
        harness.AddHandler<SuccessfulMessage>();
        harness.AddHandler<FaultingMessage>(_ => Task.FromException(expectedFailure));
        var store = new RecordingStore(expectedEntries: 2);

        await harness.StartAsync(cancellationToken);
        try
        {
            using ConnectHandle journal = harness.Bus.ConnectConsumeMessageJournal(
                store,
                PassThroughPolicy(),
                Options(timeout));
            var successful = new SuccessfulMessage(NewId.NextGuid());
            var faulting = new FaultingMessage(NewId.NextGuid());
            TimeSpan timeToLive = TimeSpan.FromMinutes(9);

            await harness.InputQueueSendEndpoint.SendAsync(
                successful,
                Pipe.Execute<SendContext<SuccessfulMessage>>(context => context.TimeToLive = timeToLive),
                cancellationToken);
            await harness.InputQueueSendEndpoint.SendAsync(faulting, cancellationToken);
            IPublishedMessage<Fault<FaultingMessage>> publishedFault = await harness.Published
                .SelectAsync<Fault<FaultingMessage>>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            await store.ExpectedEntriesReached.WaitAsync(timeout, cancellationToken);

            MessageJournalEntry[] entries = store.Entries;
            MessageJournalEntry succeeded = Assert.Single(entries, entry =>
                entry.Metadata[MessageJournalMetadataKeys.CorrelationId] == successful.CorrelationId.ToString("D"));
            MessageJournalEntry faulted = Assert.Single(entries, entry =>
                entry.Metadata[MessageJournalMetadataKeys.CorrelationId] == faulting.CorrelationId.ToString("D"));
            Assert.Equal(MessageJournalOutcome.Succeeded, succeeded.Outcome);
            Assert.Equal(MessageJournalOutcome.Faulted, faulted.Outcome);
            DateTimeOffset sentAt = DateTimeOffset.ParseExact(
                succeeded.Metadata[MessageJournalMetadataKeys.SentAt],
                "O",
                CultureInfo.InvariantCulture);
            DateTimeOffset expiresAt = DateTimeOffset.ParseExact(
                succeeded.Metadata[MessageJournalMetadataKeys.ExpiresAt],
                "O",
                CultureInfo.InvariantCulture);
            Assert.InRange(
                expiresAt - sentAt,
                timeToLive,
                timeToLive.Add(TimeSpan.FromSeconds(1)));
            Assert.Equal(TypeCache<ExpectedConsumerException>.ShortName,
                faulted.Metadata[MessageJournalMetadataKeys.FailureType]);
            Assert.Contains(publishedFault.Context.Message.Exceptions,
                exception => exception.ExceptionType == TypeCache<ExpectedConsumerException>.ShortName);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-JOURNAL-OUTCOME", "send-and-publish-faults-are-terminal")]
    public async Task OutgoingJournal_RecordsTheExactSendAndPublishFaultOutcomesAsync()
    {
        TimeSpan timeout = OperationTimeout;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness("journal-outgoing-faults", timeout);
        harness.AddHandler<JournalMessage>();
        var store = new RecordingStore(expectedEntries: 2);
        var sendFailure = new ExpectedSendException();
        var publishFailure = new ExpectedPublishException();

        await harness.StartAsync(cancellationToken);
        try
        {
            using ConnectHandle journal = harness.Bus.ConnectOutgoingMessageJournal(
                store,
                PassThroughPolicy(),
                Options(timeout));
            ISendEndpoint endpoint = await harness.Bus.GetSendEndpointAsync(
                harness.InputQueueAddress,
                cancellationToken);

            ExpectedSendException actualSend;
            using (harness.Bus.ConnectSendObserver(new ThrowingSendObserver(sendFailure)))
            {
                actualSend = await Assert.ThrowsAsync<ExpectedSendException>(() =>
                    endpoint.SendAsync(
                        new JournalMessage(NewId.NextGuid(), "send-fault"),
                        cancellationToken));
            }

            ExpectedPublishException actualPublish;
            using (harness.Bus.ConnectPublishObserver(new ThrowingPublishObserver(publishFailure)))
            {
                actualPublish = await Assert.ThrowsAsync<ExpectedPublishException>(() =>
                    harness.Bus.PublishAsync(
                        new JournalMessage(NewId.NextGuid(), "publish-fault"),
                        cancellationToken));
            }
            await store.ExpectedEntriesReached.WaitAsync(timeout, cancellationToken);

            Assert.Same(sendFailure, actualSend);
            Assert.Same(publishFailure, actualPublish);
            MessageJournalEntry[] entries = store.Entries;
            MessageJournalEntry sent = Assert.Single(entries, entry => entry.Operation == MessageJournalOperation.Send);
            MessageJournalEntry published = Assert.Single(entries, entry => entry.Operation == MessageJournalOperation.Publish);
            Assert.Equal(MessageJournalOutcome.Faulted, sent.Outcome);
            Assert.Equal(MessageJournalOutcome.Faulted, published.Outcome);
            Assert.Equal(TypeCache<ExpectedSendException>.ShortName,
                sent.Metadata[MessageJournalMetadataKeys.FailureType]);
            Assert.Equal(TypeCache<ExpectedPublishException>.ShortName,
                published.Metadata[MessageJournalMetadataKeys.FailureType]);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-JOURNAL-OUTCOME", "faulted-send-rejects-lazy-body-identity-change")]
    public async Task FaultedSend_JournalRejectsLazyIdentityChangeDuringCaptureAsync()
    {
        TimeSpan timeout = OperationTimeout;
        CancellationToken token = TestContext.Current.CancellationToken;
        using var harness = CreateHarness("journal-lazy-fault", timeout);
        harness.AddHandler<JournalMessage>();
        var store = new RecordingStore(expectedEntries: 1);
        var expectedFailure = new ExpectedSendException();
        Guid initialId = NewId.NextGuid();
        Guid serializedId = NewId.NextGuid();
        var serializer = new DeferredIdentitySerializer(serializedId);
        SendContext<JournalMessage>? capturedContext = null;

        await harness.StartAsync(token);
        try
        {
            using ConnectHandle journal = harness.Bus.ConnectOutgoingMessageJournal(
                store, PassThroughPolicy(), Options(timeout));
            ISendEndpoint endpoint = await harness.Bus.GetSendEndpointAsync(harness.InputQueueAddress, token);
            using ConnectHandle failingObserver = harness.Bus.ConnectSendObserver(
                new ThrowingSendObserver(expectedFailure));

            ExpectedSendException actual = await Assert.ThrowsAsync<ExpectedSendException>(() =>
                endpoint.SendAsync(new JournalMessage(NewId.NextGuid(), "lazy-fault"),
                    Pipe.Execute<SendContext<JournalMessage>>(context =>
                    {
                        capturedContext = context;
                        context.MessageId = initialId;
                        context.Serializer = serializer;
                    }), token));
            Assert.Same(expectedFailure, actual);
            Assert.Equal(1, serializer.MaterializationCalls);
            Assert.Equal(initialId, Assert.IsAssignableFrom<SendContext<JournalMessage>>(capturedContext).MessageId);
            Assert.Empty(store.Entries);
            Assert.Equal(0, store.AppendAttempts);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-JOURNAL-OUTCOME", "untrusted-exception-data-cannot-suppress-fault-capture")]
    public async Task FaultedSend_UntrustedExceptionDataCannotSuppressJournalAsync()
    {
        TimeSpan timeout = OperationTimeout;
        CancellationToken token = TestContext.Current.CancellationToken;
        using var harness = CreateHarness("journal-untrusted-fault-data", timeout);
        harness.AddHandler<JournalMessage>();
        var store = new RecordingStore(expectedEntries: 1);
        var expectedFailure = new ExpectedSendException();
        expectedFailure.Data["ViciOne.ServiceBus.TransportBodyMetadataChanged"] = true;
        var message = new JournalMessage(NewId.NextGuid(), "untrusted-fault-data");

        await harness.StartAsync(token);
        try
        {
            using ConnectHandle journal = harness.Bus.ConnectOutgoingMessageJournal(
                store, PassThroughPolicy(), Options(timeout));
            using ConnectHandle failingObserver = harness.Bus.ConnectSendObserver(
                new ThrowingSendObserver(expectedFailure));

            ExpectedSendException actual = await Assert.ThrowsAsync<ExpectedSendException>(() =>
                harness.InputQueueSendEndpoint.SendAsync(message, token));

            Assert.Same(expectedFailure, actual);
            MessageJournalEntry entry = Assert.Single(store.Entries);
            Assert.Equal(MessageJournalOutcome.Faulted, entry.Outcome);
            Assert.Equal(TypeCache<ExpectedSendException>.ShortName,
                entry.Metadata[MessageJournalMetadataKeys.FailureType]);
            AssertEnvelope(entry, message);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-MESSAGE-JOURNAL-OUTCOME", "faulted-send-rejects-body-metadata-mutation")]
    public async Task FaultedSend_BodyMutationCannotWriteContradictoryEntryOrLeakContextAsync(
        bool changeContentType, bool throwAfterMutation)
    {
        TimeSpan timeout = OperationTimeout;
        CancellationToken token = TestContext.Current.CancellationToken;
        using var harness = CreateHarness("journal-faulted-body-metadata", timeout);
        harness.AddHandler<JournalMessage>();
        var store = new RecordingStore(expectedEntries: 1);
        var expectedFailure = new ExpectedSendException();
        Guid initialId = NewId.NextGuid();
        Guid changedId = NewId.NextGuid();
        SendContext<JournalMessage>? capturedContext = null;
        PostBytesMutationSerializer? serializer = null;

        await harness.StartAsync(token);
        try
        {
            using ConnectHandle journal = harness.Bus.ConnectOutgoingMessageJournal(
                store, PassThroughPolicy(), Options(timeout));
            using ConnectHandle failingObserver = harness.Bus.ConnectSendObserver(
                new ThrowingSendObserver(expectedFailure));

            ExpectedSendException actual = await Assert.ThrowsAsync<ExpectedSendException>(() =>
                harness.InputQueueSendEndpoint.SendAsync(
                    new JournalMessage(NewId.NextGuid(), "faulted-body-metadata"),
                    Pipe.Execute<SendContext<JournalMessage>>(context =>
                    {
                        capturedContext = context;
                        context.MessageId = initialId;
                        serializer = new PostBytesMutationSerializer(
                            initialId, changedId, changeContentType, throwAfterMutation);
                        context.Serializer = serializer;
                    }), token));

            Assert.Same(expectedFailure, actual);
            SendContext<JournalMessage> context = Assert.IsAssignableFrom<SendContext<JournalMessage>>(capturedContext);
            Assert.Equal(initialId, context.MessageId);
            Assert.Equal("application/json", context.ContentType?.ToString());
            Assert.Equal(1, Assert.IsType<PostBytesMutationSerializer>(serializer).MaterializationCalls);
            Assert.Empty(store.Entries);
            Assert.Equal(0, store.AppendAttempts);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-JOURNAL-OUTCOME", "successful-send-rejects-post-dispatch-identity-mutation")]
    public async Task SuccessfulSend_JournalDoesNotRecordAnIdentityChangedAfterDeliveryAsync()
    {
        TimeSpan timeout = OperationTimeout;
        CancellationToken token = TestContext.Current.CancellationToken;
        using var harness = CreateHarness("journal-post-send-identity", timeout);
        HandlerTestHarness<JournalMessage> handler = harness.AddHandler<JournalMessage>();
        var store = new RecordingStore(expectedEntries: 1);
        Guid deliveredId = NewId.NextGuid();
        Guid laterId = NewId.NextGuid();
        MaterializationIdentitySerializer? serializer = null;
        SendContext<JournalMessage>? sendContext = null;

        await harness.StartAsync(token);
        try
        {
            using ConnectHandle journal = harness.Bus.ConnectOutgoingMessageJournal(
                store, PassThroughPolicy(), Options(timeout));

            var message = new JournalMessage(NewId.NextGuid(), "post-send-identity");
            await harness.InputQueueSendEndpoint.SendAsync(message,
                Pipe.Execute<SendContext<JournalMessage>>(context =>
                {
                    sendContext = context;
                    context.MessageId = deliveredId;
                    serializer = new MaterializationIdentitySerializer(context.Serializer, laterId, mutationCall: 2);
                    context.Serializer = serializer;
                }), token);
            IConsumedMessage<JournalMessage> consumed = await handler.Consumed
                .SelectAsync(token).FirstObservedAsync(cancellationToken: token);

            Assert.Equal(message, consumed.Context.Message);
            Assert.Equal(deliveredId, consumed.Context.MessageId);
            Assert.Equal(deliveredId, Assert.IsAssignableFrom<SendContext<JournalMessage>>(sendContext).MessageId);
            Assert.Equal(2, Assert.IsType<MaterializationIdentitySerializer>(serializer).MaterializationCalls);
            Assert.Empty(store.Entries);
            Assert.Equal(0, store.AppendAttempts);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-JOURNAL-OUTCOME", "successful-send-rejects-post-dispatch-content-type-mutation")]
    public async Task SuccessfulSend_JournalDoesNotRecordContentTypeChangedAfterDeliveryAsync()
    {
        TimeSpan timeout = OperationTimeout;
        CancellationToken token = TestContext.Current.CancellationToken;
        using var harness = CreateHarness("journal-post-send-content-type", timeout);
        HandlerTestHarness<JournalMessage> handler = harness.AddHandler<JournalMessage>();
        var store = new RecordingStore(expectedEntries: 1);
        MaterializationContentTypeSerializer? serializer = null;
        SendContext<JournalMessage>? sendContext = null;
        string? deliveredContentType = null;

        await harness.StartAsync(token);
        try
        {
            using ConnectHandle journal = harness.Bus.ConnectOutgoingMessageJournal(
                store, PassThroughPolicy(), Options(timeout));

            var message = new JournalMessage(NewId.NextGuid(), "post-send-content-type");
            await harness.InputQueueSendEndpoint.SendAsync(message,
                Pipe.Execute<SendContext<JournalMessage>>(context =>
                {
                    sendContext = context;
                    serializer = new MaterializationContentTypeSerializer(
                        context.Serializer, "application/vnd.vicione.changed", mutationCall: 2);
                    context.Serializer = serializer;
                    deliveredContentType = context.ContentType?.ToString();
                }), token);
            IConsumedMessage<JournalMessage> consumed = await handler.Consumed
                .SelectAsync(token).FirstObservedAsync(cancellationToken: token);

            Assert.Equal(message, consumed.Context.Message);
            Assert.Equal(deliveredContentType, consumed.Context.Advanced().ReceiveContext.ContentType.ToString());
            Assert.Equal(deliveredContentType, Assert.IsAssignableFrom<SendContext<JournalMessage>>(sendContext).ContentType?.ToString());
            Assert.Equal(2, Assert.IsType<MaterializationContentTypeSerializer>(serializer).MaterializationCalls);
            Assert.Empty(store.Entries);
            Assert.Equal(0, store.AppendAttempts);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-JOURNAL-OUTCOME", "successful-send-preserves-delivery-after-throwing-content-type-mutation")]
    public async Task SuccessfulSend_ThrowingJournalBodyRestoresDeliveredContentTypeAsync()
    {
        TimeSpan timeout = OperationTimeout;
        CancellationToken token = TestContext.Current.CancellationToken;
        using var harness = CreateHarness("journal-throwing-post-send-content-type", timeout);
        HandlerTestHarness<JournalMessage> handler = harness.AddHandler<JournalMessage>();
        var store = new RecordingStore(expectedEntries: 1);
        var expectedFailure = new ExpectedSendException();
        ThrowingSecondContentTypeSerializer? serializer = null;
        SendContext<JournalMessage>? sendContext = null;
        string? deliveredContentType = null;

        await harness.StartAsync(token);
        try
        {
            using ConnectHandle journal = harness.Bus.ConnectOutgoingMessageJournal(
                store, PassThroughPolicy(), Options(timeout));

            var message = new JournalMessage(NewId.NextGuid(), "throwing-post-send-content-type");
            await harness.InputQueueSendEndpoint.SendAsync(message,
                Pipe.Execute<SendContext<JournalMessage>>(context =>
                {
                    sendContext = context;
                    serializer = new ThrowingSecondContentTypeSerializer(
                        context.Serializer, "application/vnd.vicione.changed", expectedFailure);
                    context.Serializer = serializer;
                    deliveredContentType = context.ContentType?.ToString();
                }), token);
            IConsumedMessage<JournalMessage> consumed = await handler.Consumed
                .SelectAsync(token).FirstObservedAsync(cancellationToken: token);

            Assert.Equal(message, consumed.Context.Message);
            Assert.Equal(deliveredContentType, consumed.Context.Advanced().ReceiveContext.ContentType.ToString());
            Assert.Equal(deliveredContentType, Assert.IsAssignableFrom<SendContext<JournalMessage>>(sendContext).ContentType?.ToString());
            Assert.Equal(2, Assert.IsType<ThrowingSecondContentTypeSerializer>(serializer).MaterializationCalls);
            Assert.Empty(store.Entries);
            Assert.Equal(0, store.AppendAttempts);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-TRANSPORT-ISOLATION", "serialization-must-not-change-dispatch-identity")]
    public async Task InMemorySend_RejectsIdentityChangedWhileMaterializingBodyAsync()
    {
        TimeSpan timeout = OperationTimeout;
        CancellationToken token = TestContext.Current.CancellationToken;
        using var harness = CreateHarness("send-serialized-identity", timeout);
        harness.AddHandler<JournalMessage>();
        Guid initialId = NewId.NextGuid();
        Guid serializedId = NewId.NextGuid();
        MaterializationIdentitySerializer? serializer = null;

        await harness.StartAsync(token);
        try
        {
            MessageException failure = await Assert.ThrowsAsync<MessageException>(() =>
                harness.InputQueueSendEndpoint.SendAsync(
                    new JournalMessage(NewId.NextGuid(), "identity-mutation"),
                    Pipe.Execute<SendContext<JournalMessage>>(context =>
                    {
                        context.MessageId = initialId;
                        serializer = new MaterializationIdentitySerializer(context.Serializer, serializedId, mutationCall: 1);
                        context.Serializer = serializer;
                    }), token));

            Assert.Contains("MessageId changed during serialization", failure.Message, StringComparison.Ordinal);
            Assert.Equal(1, Assert.IsType<MaterializationIdentitySerializer>(serializer).MaterializationCalls);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-JOURNAL-OUTCOME", "rejected-transport-body-mutation-has-no-contradictory-fault-entry")]
    public async Task RejectedSend_JournalDoesNotRecordMutatedFaultMetadataAsync()
    {
        TimeSpan timeout = OperationTimeout;
        CancellationToken token = TestContext.Current.CancellationToken;
        using var harness = CreateHarness("journal-rejected-body-identity", timeout);
        harness.AddHandler<JournalMessage>();
        var store = new RecordingStore(expectedEntries: 1);
        Guid initialId = NewId.NextGuid();
        Guid changedId = NewId.NextGuid();
        MaterializationIdentitySerializer? serializer = null;
        SendContext<JournalMessage>? capturedContext = null;

        await harness.StartAsync(token);
        try
        {
            using ConnectHandle journal = harness.Bus.ConnectOutgoingMessageJournal(
                store, PassThroughPolicy(), Options(timeout));

            MessageException failure = await Assert.ThrowsAsync<MessageException>(() =>
                harness.InputQueueSendEndpoint.SendAsync(
                    new JournalMessage(NewId.NextGuid(), "rejected-body"),
                    Pipe.Execute<SendContext<JournalMessage>>(context =>
                    {
                        capturedContext = context;
                        context.MessageId = initialId;
                        serializer = new MaterializationIdentitySerializer(
                            context.Serializer, changedId, mutationCall: 0);
                        context.Serializer = serializer;
                    }), token));

            Assert.Contains("MessageId changed during serialization", failure.Message, StringComparison.Ordinal);
            Assert.Equal(initialId, Assert.IsAssignableFrom<SendContext<JournalMessage>>(capturedContext).MessageId);
            Assert.Equal(1, Assert.IsType<MaterializationIdentitySerializer>(serializer).MaterializationCalls);
            Assert.Equal(0, store.AppendAttempts);
            Assert.Empty(store.Entries);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-JOURNAL-OUTCOME", "throwing-body-mutation-preserves-original-fault-without-false-entry")]
    public async Task ThrowingBodyMutation_PreservesOriginalSendFaultWithoutContradictoryJournalAsync()
    {
        TimeSpan timeout = OperationTimeout;
        CancellationToken token = TestContext.Current.CancellationToken;
        using var harness = CreateHarness("journal-throwing-body-identity", timeout);
        harness.AddHandler<JournalMessage>();
        var store = new RecordingStore(expectedEntries: 1);
        var expectedFailure = new ExpectedSendException();
        Guid initialId = NewId.NextGuid();
        Guid changedId = NewId.NextGuid();
        ThrowingFirstBodySerializer? serializer = null;
        SendContext<JournalMessage>? capturedContext = null;

        await harness.StartAsync(token);
        try
        {
            using ConnectHandle journal = harness.Bus.ConnectOutgoingMessageJournal(
                store, PassThroughPolicy(), Options(timeout));

            ExpectedSendException actual = await Assert.ThrowsAsync<ExpectedSendException>(() =>
                harness.InputQueueSendEndpoint.SendAsync(
                    new JournalMessage(NewId.NextGuid(), "throwing-body"),
                    Pipe.Execute<SendContext<JournalMessage>>(context =>
                    {
                        capturedContext = context;
                        context.MessageId = initialId;
                        serializer = new ThrowingFirstBodySerializer(context.Serializer, changedId, expectedFailure);
                        context.Serializer = serializer;
                    }), token));

            Assert.Same(expectedFailure, actual);
            Assert.Equal(initialId, Assert.IsAssignableFrom<SendContext<JournalMessage>>(capturedContext).MessageId);
            Assert.Equal(1, Assert.IsType<ThrowingFirstBodySerializer>(serializer).MaterializationCalls);
            Assert.Equal(0, store.AppendAttempts);
            Assert.Empty(store.Entries);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-JOURNAL-FAILURE-ISOLATION", "store-failure-does-not-change-delivery")]
    public async Task StoreFailure_DoesNotChangeASuccessfulMessageDeliveryAsync()
    {
        TimeSpan timeout = OperationTimeout;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness("journal-store-failure", timeout);
        HandlerTestHarness<JournalMessage> handler = harness.AddHandler<JournalMessage>();
        var store = new RecordingStore(
            expectedEntries: 0,
            failure: new ExpectedStoreException(),
            expectedAttempts: 2);

        await harness.StartAsync(cancellationToken);
        try
        {
            using ConnectHandle journal = harness.Bus.ConnectMessageJournal(
                store,
                PassThroughPolicy(),
                Options(timeout));
            var message = new JournalMessage(NewId.NextGuid(), "delivered");

            await harness.InputQueueSendEndpoint.SendAsync(message, cancellationToken);
            IConsumedMessage<JournalMessage> consumed = await handler.Consumed
                .SelectAsync(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            await store.ExpectedAttemptsReached.WaitAsync(timeout, cancellationToken);

            Assert.Equal(message, consumed.Context.Message);
            Assert.Equal(2, store.AppendAttempts);
            Assert.Empty(store.Entries);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-JOURNAL-OUTBOX", "deferred-message-uses-real-envelope-identity")]
    public async Task InMemoryOutbox_RecordsTheDeferredMessageEnvelopeInsteadOfAnInternalWrapperAsync()
    {
        TimeSpan timeout = OperationTimeout;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness("journal-outbox-envelope", timeout);
        harness.InMemoryReceiveEndpointConfiguring += configurator =>
        {
            configurator.UseVolatileOutbox();
            configurator.Handler<OutboxRequest>(context =>
                context.Advanced().PublishAsync(new DeferredMessage(context.Message.CorrelationId, "deferred")));
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

        await harness.StartAsync(cancellationToken);
        try
        {
            using ConnectHandle journal = harness.Bus.ConnectOutgoingMessageJournal(
                store,
                onlyDeferredMessages,
                Options(timeout));
            var request = new OutboxRequest(NewId.NextGuid());

            await harness.InputQueueSendEndpoint.SendAsync(request, cancellationToken);
            await store.ExpectedEntriesReached.WaitAsync(timeout, cancellationToken);

            MessageJournalEntry entry = Assert.Single(store.Entries);
            Assert.Contains(entry.MessageTypes,
                type => type.Contains(nameof(DeferredMessage), StringComparison.Ordinal));
            Assert.DoesNotContain(entry.MessageTypes,
                type => type.Contains(nameof(SerializedTransportMessage), StringComparison.Ordinal));
            AssertEnvelope(entry, new DeferredMessage(request.CorrelationId, "deferred"));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-JOURNAL-ACTIVATION", "disconnect-removes-all-journal-observers")]
    public async Task ConnectionHandle_DisconnectsEverySelectedJournalObserverAsync()
    {
        TimeSpan timeout = OperationTimeout;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness("journal-disconnect", timeout);
        harness.AddHandler<JournalMessage>();
        var store = new RecordingStore(expectedEntries: 1);

        await harness.StartAsync(cancellationToken);
        try
        {
            ConnectHandle journal = harness.Bus.ConnectOutgoingMessageJournal(
                store,
                PassThroughPolicy(),
                Options(timeout));
            await harness.InputQueueSendEndpoint.SendAsync(
                new JournalMessage(NewId.NextGuid(), "before"),
                cancellationToken);
            await store.ExpectedEntriesReached.WaitAsync(timeout, cancellationToken);

            journal.Dispose();
            journal.Dispose();
            await harness.InputQueueSendEndpoint.SendAsync(
                new JournalMessage(NewId.NextGuid(), "after"),
                cancellationToken);

            Assert.Single(store.Entries);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static void AssertEnvelope<T>(MessageJournalEntry entry, T expected)
        where T : class
    {
        using JsonDocument document = JsonDocument.Parse(entry.Body);
        using JsonDocument expectedDocument = JsonDocument.Parse(
            JsonSerializer.Serialize(expected, ServiceBusMetadataJson.Options));
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

    private sealed record JournalMessage(Guid CorrelationId, string Value) : ICorrelatedBy<Guid>;
    private sealed record SuccessfulMessage(Guid CorrelationId) : ICorrelatedBy<Guid>;
    private sealed record FaultingMessage(Guid CorrelationId) : ICorrelatedBy<Guid>;
    private sealed record OutboxRequest(Guid CorrelationId) : ICorrelatedBy<Guid>;
    private sealed record DeferredMessage(Guid CorrelationId, string Value) : ICorrelatedBy<Guid>;

    private sealed class DeferredIdentitySerializer(Guid serializedId) : IMessageSerializer
    {
        public ContentType ContentType { get; } = new("application/json");

        public int MaterializationCalls { get; private set; }

        public MessageBody GetMessageBody<T>(SendContext<T> context) where T : class =>
            new DeferredIdentityBody(serializedId, () =>
            {
                MaterializationCalls++;
                context.MessageId = serializedId;
            });
    }

    private sealed class PostBytesMutationSerializer(Guid initialId, Guid changedId, bool changeContentType,
        bool throwAfterMutation) : IMessageSerializer
    {
        public ContentType ContentType { get; } = new("application/json");

        public int MaterializationCalls { get; private set; }

        public MessageBody GetMessageBody<T>(SendContext<T> context) where T : class =>
            new PostBytesMutationBody(initialId, () =>
            {
                MaterializationCalls++;
                if (changeContentType)
                    context.ContentType = new ContentType("application/vnd.vicione.changed");
                else
                    context.MessageId = changedId;
                if (throwAfterMutation)
                    throw new InvalidOperationException("Body callback failed after changing metadata.");
            });
    }

    private sealed class PostBytesMutationBody(Guid initialId, Action afterBytes) : MessageBody
    {
        private readonly byte[] _bytes = Encoding.UTF8.GetBytes($"{{\"messageId\":\"{initialId:D}\"}}");

        public long Length => _bytes.Length;

        public byte[] ToArray()
        {
            byte[] bytes = (byte[])_bytes.Clone();
            afterBytes();
            return bytes;
        }

        public Stream OpenReadStream() => new MemoryStream(ToArray(), writable: false);

        public bool TryGetTransportText([NotNullWhen(true)] out string? text)
        {
            text = Encoding.UTF8.GetString(ToArray());
            return true;
        }
    }

    private sealed class DeferredIdentityBody : MessageBody
    {
        private readonly byte[] _body;
        private readonly Action _onMaterialize;

        public DeferredIdentityBody(Guid serializedId, Action onMaterialize)
        {
            _body = Encoding.UTF8.GetBytes($"{{\"messageId\":\"{serializedId:D}\"}}");
            _onMaterialize = onMaterialize;
        }

        public long Length => _body.Length;

        public byte[] ToArray()
        {
            _onMaterialize();
            return (byte[])_body.Clone();
        }

        public Stream OpenReadStream() => new MemoryStream(ToArray(), writable: false);

        public bool TryGetTransportText([NotNullWhen(true)] out string? text)
        {
            text = Encoding.UTF8.GetString(ToArray());
            return true;
        }
    }

    private sealed class MaterializationIdentitySerializer(IMessageSerializer inner, Guid laterId, int mutationCall) : IMessageSerializer
    {
        public ContentType ContentType => inner.ContentType;

        public int MaterializationCalls { get; private set; }

        public MessageBody GetMessageBody<T>(SendContext<T> context) where T : class =>
            new MaterializationIdentityBody(inner.GetMessageBody(context), () =>
            {
                MaterializationCalls++;
                if (mutationCall == 0 || MaterializationCalls == mutationCall)
                    context.MessageId = laterId;
            });
    }

    private sealed class MaterializationContentTypeSerializer(IMessageSerializer inner, string laterContentType, int mutationCall)
        : IMessageSerializer
    {
        public ContentType ContentType => inner.ContentType;

        public int MaterializationCalls { get; private set; }

        public MessageBody GetMessageBody<T>(SendContext<T> context) where T : class =>
            new MaterializationIdentityBody(inner.GetMessageBody(context), () =>
            {
                MaterializationCalls++;
                if (MaterializationCalls == mutationCall)
                    context.ContentType = new ContentType(laterContentType);
            });
    }

    private sealed class ThrowingSecondContentTypeSerializer(IMessageSerializer inner, string laterContentType, Exception failure)
        : IMessageSerializer
    {
        public ContentType ContentType => inner.ContentType;

        public int MaterializationCalls { get; private set; }

        public MessageBody GetMessageBody<T>(SendContext<T> context) where T : class =>
            new MaterializationIdentityBody(inner.GetMessageBody(context), () =>
            {
                MaterializationCalls++;
                if (MaterializationCalls == 2)
                {
                    context.ContentType = new ContentType(laterContentType);
                    throw failure;
                }
            });
    }

    private sealed class MaterializationIdentityBody(MessageBody inner, Action onMaterialize) : MessageBody
    {
        public long Length => inner.Length;

        public byte[] ToArray()
        {
            onMaterialize();
            return inner.ToArray();
        }

        public Stream OpenReadStream() => inner.OpenReadStream();

        public bool TryGetTransportText([NotNullWhen(true)] out string? text) =>
            inner.TryGetTransportText(out text);
    }

    private sealed class ThrowingFirstBodySerializer(IMessageSerializer inner, Guid changedId, Exception failure)
        : IMessageSerializer
    {
        public ContentType ContentType => inner.ContentType;

        public int MaterializationCalls { get; private set; }

        public MessageBody GetMessageBody<T>(SendContext<T> context) where T : class =>
            new ThrowingFirstBody(inner.GetMessageBody(context), () =>
            {
                MaterializationCalls++;
                if (MaterializationCalls == 1)
                {
                    context.MessageId = changedId;
                    throw failure;
                }
            });
    }

    private sealed class ThrowingFirstBody(MessageBody inner, Action onMaterialize) : MessageBody
    {
        public long Length => inner.Length;

        public byte[] ToArray()
        {
            onMaterialize();
            return inner.ToArray();
        }

        public Stream OpenReadStream() => new MemoryStream(ToArray(), writable: false);

        public bool TryGetTransportText([NotNullWhen(true)] out string? text) =>
            inner.TryGetTransportText(out text);
    }

    private sealed class ThrowingSendObserver(Exception failure) : ISendObserver
    {
        public Task PreSendAsync<T>(SendContext<T> context)
            where T : class => Task.FromException(failure);

        public Task PostSendAsync<T>(SendContext<T> context)
            where T : class => Task.CompletedTask;

        public Task SendFaultAsync<T>(SendContext<T> context, Exception exception)
            where T : class => Task.CompletedTask;
    }

    private sealed class ThrowingPublishObserver(Exception failure) : IPublishObserver
    {
        public Task PrePublishAsync<T>(PublishContext<T> context)
            where T : class => Task.FromException(failure);

        public Task PostPublishAsync<T>(PublishContext<T> context)
            where T : class => Task.CompletedTask;

        public Task PublishFaultAsync<T>(PublishContext<T> context, Exception exception)
            where T : class => Task.CompletedTask;
    }

    private sealed class ExpectedConsumerException : Exception;
    private sealed class ExpectedPublishException : Exception;
    private sealed class ExpectedSendException : Exception;
    private sealed class ExpectedStoreException : Exception;
}
