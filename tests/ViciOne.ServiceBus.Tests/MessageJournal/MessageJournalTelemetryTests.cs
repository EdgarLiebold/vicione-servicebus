using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.MessageJournal;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.MessageJournal;
using ViciOne.ServiceBus.Tests.Testing;
using Xunit;
using ActivityContext = System.Diagnostics.ActivityContext;

namespace ViciOne.ServiceBus.Tests.MessageJournal;

[Collection(OpenTelemetryGlobalCollection.Name)]
public sealed class MessageJournalTelemetryTests
{
    private static readonly DateTimeOffset ObservationTime =
        new(2030, 1, 2, 3, 4, 5, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-JOURNAL-OBSERVABILITY", "exact-low-cardinality-otel-schema")]
    public async Task StoredFilteredAndFailedWrites_EmitOnlyTheExactLowCardinalitySchemaAsync()
    {
        var measurements = new ConcurrentQueue<MeasurementRecord>();
        var activities = new ConcurrentQueue<ActivityRecord>();
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == "ViciOne.ServiceBus"
                && instrument.Name.StartsWith("vicione.servicebus.message_journal.", StringComparison.Ordinal))
                listener.EnableMeasurementEvents(instrument);
        };
        meterListener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
            measurements.Enqueue(new MeasurementRecord(instrument.Name, instrument.Meter.Version, value, CopyTags(tags))));
        meterListener.SetMeasurementEventCallback<double>((instrument, value, tags, _) =>
            measurements.Enqueue(new MeasurementRecord(instrument.Name, instrument.Meter.Version, value, CopyTags(tags))));
        meterListener.Start();

        using var activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "ViciOne.ServiceBus",
            Sample = static (ref ActivityCreationOptions<ActivityContext> options) =>
                options.Name == "ViciOne.ServiceBus.MessageJournal.Write"
                    ? ActivitySamplingResult.AllData
                    : ActivitySamplingResult.None,
            ActivityStopped = activity => activities.Enqueue(new ActivityRecord(
                activity.OperationName,
                activity.Source.Version,
                activity.Kind,
                activity.TagObjects.ToArray())),
        };
        ActivitySource.AddActivityListener(activityListener);

        await CreateDriver(new RecordingStore(), PassThroughPolicy()).ObserveAsync(
            MessageJournalOperation.Send,
            MessageJournalOutcome.Succeeded,
            "stored"u8.ToArray(),
            cancellationToken: TestContext.Current.CancellationToken);
        await CreateDriver(new RecordingStore(), new DelegatePolicy(static (_, _) =>
            ValueTask.FromResult<MessageJournalProjection?>(null))).ObserveAsync(
            MessageJournalOperation.Publish,
            MessageJournalOutcome.Succeeded,
            "filtered"u8.ToArray(),
            cancellationToken: TestContext.Current.CancellationToken);
        await CreateDriver(new RecordingStore(new ExpectedStoreException()), PassThroughPolicy()).ObserveAsync(
            MessageJournalOperation.Consume,
            MessageJournalOutcome.Faulted,
            "failed"u8.ToArray(),
            cancellationToken: TestContext.Current.CancellationToken);

        string expectedVersion = Assert.IsType<string>(HostMetadataCache.Host.ViciOneServiceBusVersion);
        Assert.All(measurements, measurement => Assert.Equal(expectedVersion, measurement.SourceVersion));
        Assert.All(
            measurements.Where(measurement => measurement.Name.EndsWith(".operations", StringComparison.Ordinal)),
            measurement => Assert.Equal(1, measurement.Value));
        Assert.All(
            measurements.Where(measurement => measurement.Name.EndsWith(".duration", StringComparison.Ordinal)),
            measurement => Assert.Equal(0, measurement.Value));
        Assert.All(activities, activity =>
        {
            Assert.Equal(expectedVersion, activity.SourceVersion);
            Assert.Equal(ActivityKind.Internal, activity.Kind);
        });
        Assert.Equal(
            [
                "vicione.servicebus.message_journal.duration|message_journal.failure.reason=store,message_journal.operation=consume,message_journal.outcome=faulted,message_journal.result=failed",
                "vicione.servicebus.message_journal.duration|message_journal.operation=publish,message_journal.outcome=succeeded,message_journal.result=filtered",
                "vicione.servicebus.message_journal.duration|message_journal.operation=send,message_journal.outcome=succeeded,message_journal.result=stored",
                "vicione.servicebus.message_journal.operations|message_journal.failure.reason=store,message_journal.operation=consume,message_journal.outcome=faulted,message_journal.result=failed",
                "vicione.servicebus.message_journal.operations|message_journal.operation=publish,message_journal.outcome=succeeded,message_journal.result=filtered",
                "vicione.servicebus.message_journal.operations|message_journal.operation=send,message_journal.outcome=succeeded,message_journal.result=stored",
            ],
            measurements.Select(item => Describe(item.Name, item.Tags)).Order(StringComparer.Ordinal));
        Assert.Equal(
            [
                "ViciOne.ServiceBus.MessageJournal.Write|message_journal.failure.reason=store,message_journal.operation=consume,message_journal.outcome=faulted,message_journal.result=failed",
                "ViciOne.ServiceBus.MessageJournal.Write|message_journal.operation=publish,message_journal.outcome=succeeded,message_journal.result=filtered",
                "ViciOne.ServiceBus.MessageJournal.Write|message_journal.operation=send,message_journal.outcome=succeeded,message_journal.result=stored",
            ],
            activities.Select(item => Describe(item.Name, item.Tags)).Order(StringComparer.Ordinal));
        Assert.DoesNotContain(measurements.SelectMany(item => item.Tags), tag =>
            tag.Key.Contains("message.id", StringComparison.Ordinal)
            || tag.Key.Contains("payload", StringComparison.Ordinal)
            || tag.Key.Contains("address", StringComparison.Ordinal)
            || tag.Key.Contains("exception", StringComparison.Ordinal));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-JOURNAL-OBSERVABILITY", "throwing-listener-cannot-change-journal-semantics")]
    public async Task ThrowingOpenTelemetryListeners_CannotChangeStorageOrEscapeTheWriterAsync()
    {
        var store = new RecordingStore();
        using (var meterListener = new MeterListener())
        {
            meterListener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == "ViciOne.ServiceBus"
                    && instrument.Name.StartsWith("vicione.servicebus.message_journal.", StringComparison.Ordinal))
                    listener.EnableMeasurementEvents(instrument);
            };
            meterListener.SetMeasurementEventCallback<long>(static (_, _, _, _) => throw new TelemetryObserverException());
            meterListener.Start();

            await CreateDriver(store, PassThroughPolicy()).ObserveAsync(
                MessageJournalOperation.Send,
                MessageJournalOutcome.Succeeded,
                "stored"u8.ToArray(),
                cancellationToken: TestContext.Current.CancellationToken);
        }

        using var activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "ViciOne.ServiceBus",
            Sample = static (ref ActivityCreationOptions<ActivityContext> options) =>
            {
                if (options.Name == "ViciOne.ServiceBus.MessageJournal.Write")
                    throw new TelemetryObserverException();

                return ActivitySamplingResult.None;
            },
        };
        ActivitySource.AddActivityListener(activityListener);

        await CreateDriver(store, PassThroughPolicy()).ObserveAsync(
            MessageJournalOperation.Publish,
            MessageJournalOutcome.Succeeded,
            "stored-again"u8.ToArray(),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(2, store.Entries.Count);
    }

    private static MessageJournalWriterTestDriver CreateDriver(
        IMessageJournalStore store,
        IMessageJournalPolicy policy) =>
        new(
            store,
            policy,
            MessageJournalOptions.ContinueMessageFlow(
                TimeSpan.FromSeconds(5),
                new FakeTimeProvider(ObservationTime)));

    private static IMessageJournalPolicy PassThroughPolicy() => new DelegatePolicy(static (capture, _) =>
        ValueTask.FromResult<MessageJournalProjection?>(new MessageJournalProjection(
            MessageJournalDataClassification.Internal,
            capture.ContentType,
            capture.MessageTypes,
            capture.Metadata,
            capture.Headers,
            capture.Body)));

    private static KeyValuePair<string, object?>[] CopyTags(
        ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        var copy = new KeyValuePair<string, object?>[tags.Length];
        tags.CopyTo(copy);
        return copy;
    }

    private static string Describe(string name, IEnumerable<KeyValuePair<string, object?>> tags) =>
        $"{name}|{string.Join(',', tags.OrderBy(tag => tag.Key, StringComparer.Ordinal).Select(tag => $"{tag.Key}={tag.Value}"))}";

    private sealed class DelegatePolicy(
        Func<MessageJournalCapture, CancellationToken, ValueTask<MessageJournalProjection?>> project) : IMessageJournalPolicy
    {
        public ValueTask<MessageJournalProjection?> ProjectAsync(
            MessageJournalCapture capture,
            CancellationToken cancellationToken) => project(capture, cancellationToken);
    }

    private sealed class RecordingStore(Exception? failure = null) : IMessageJournalStore
    {
        private readonly List<MessageJournalEntry> _entries = [];

        public IReadOnlyList<MessageJournalEntry> Entries => _entries;

        public MessageJournalStoreLimits Limits { get; } =
            new(4096, maximumEntries: 100, retentionPeriod: TimeSpan.FromDays(1));

        public ValueTask AppendAsync(MessageJournalEntry entry, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (failure is not null)
                return ValueTask.FromException(failure);

            _entries.Add(entry);
            return ValueTask.CompletedTask;
        }
    }

    private sealed record MeasurementRecord(
        string Name,
        string? SourceVersion,
        double Value,
        KeyValuePair<string, object?>[] Tags);

    private sealed record ActivityRecord(
        string Name,
        string? SourceVersion,
        ActivityKind Kind,
        KeyValuePair<string, object?>[] Tags);

    private sealed class ExpectedStoreException : Exception;
    private sealed class TelemetryObserverException : Exception;
}
