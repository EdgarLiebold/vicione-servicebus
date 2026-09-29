using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.MessageJournal;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Monitoring;
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
        var instruments = new ConcurrentDictionary<string, InstrumentRecord>(StringComparer.Ordinal);
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == ServiceBusTelemetry.MeterName
                && (instrument.Name == ServiceBusTelemetry.Metrics.MessageJournalOperations
                    || instrument.Name == ServiceBusTelemetry.Metrics.MessageJournalDuration))
            {
                instruments[instrument.Name] = new InstrumentRecord(
                    instrument.GetType().GetGenericTypeDefinition(),
                    instrument.Unit,
                    instrument.Description);
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
            measurements.Enqueue(new MeasurementRecord(instrument.Name, instrument.Meter.Version, value, CopyTags(tags))));
        meterListener.SetMeasurementEventCallback<double>((instrument, value, tags, _) =>
            measurements.Enqueue(new MeasurementRecord(instrument.Name, instrument.Meter.Version, value, CopyTags(tags))));
        meterListener.Start();

        using var activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ServiceBusTelemetry.ActivitySourceName,
            Sample = static (ref ActivityCreationOptions<ActivityContext> options) =>
                options.Name == ServiceBusTelemetry.Activities.MessageJournalObserve
                    ? ActivitySamplingResult.AllData
                    : ActivitySamplingResult.None,
            ActivityStopped = activity => activities.Enqueue(new ActivityRecord(
                activity.OperationName,
                activity.Source.Version,
                activity.Kind,
                activity.Status,
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
        Assert.Equal(2, activities.Count(activity => activity.Status == ActivityStatusCode.Ok));
        Assert.Single(activities, activity => activity.Status == ActivityStatusCode.Error);
        Assert.Equal(
            new InstrumentRecord(
                typeof(Counter<>),
                "{operation}",
                "Completed message-journal observations."),
            instruments[ServiceBusTelemetry.Metrics.MessageJournalOperations]);
        Assert.Equal(
            new InstrumentRecord(
                typeof(Histogram<>),
                "s",
                "Duration of processing one message-journal observation."),
            instruments[ServiceBusTelemetry.Metrics.MessageJournalDuration]);
        Assert.Equal(
            [
                "vicione.servicebus.message_journal.duration|vicione.servicebus.message_journal.failure.reason=store,vicione.servicebus.message_journal.operation=consume,vicione.servicebus.message_journal.outcome=faulted,vicione.servicebus.message_journal.result=failed",
                "vicione.servicebus.message_journal.duration|vicione.servicebus.message_journal.operation=publish,vicione.servicebus.message_journal.outcome=succeeded,vicione.servicebus.message_journal.result=filtered",
                "vicione.servicebus.message_journal.duration|vicione.servicebus.message_journal.operation=send,vicione.servicebus.message_journal.outcome=succeeded,vicione.servicebus.message_journal.result=stored",
                "vicione.servicebus.message_journal.operations|vicione.servicebus.message_journal.failure.reason=store,vicione.servicebus.message_journal.operation=consume,vicione.servicebus.message_journal.outcome=faulted,vicione.servicebus.message_journal.result=failed",
                "vicione.servicebus.message_journal.operations|vicione.servicebus.message_journal.operation=publish,vicione.servicebus.message_journal.outcome=succeeded,vicione.servicebus.message_journal.result=filtered",
                "vicione.servicebus.message_journal.operations|vicione.servicebus.message_journal.operation=send,vicione.servicebus.message_journal.outcome=succeeded,vicione.servicebus.message_journal.result=stored",
            ],
            measurements.Select(item => Describe(item.Name, item.Tags)).Order(StringComparer.Ordinal));
        Assert.Equal(
            [
                "message journal observe|vicione.servicebus.message_journal.failure.reason=store,vicione.servicebus.message_journal.operation=consume,vicione.servicebus.message_journal.outcome=faulted,vicione.servicebus.message_journal.result=failed",
                "message journal observe|vicione.servicebus.message_journal.operation=publish,vicione.servicebus.message_journal.outcome=succeeded,vicione.servicebus.message_journal.result=filtered",
                "message journal observe|vicione.servicebus.message_journal.operation=send,vicione.servicebus.message_journal.outcome=succeeded,vicione.servicebus.message_journal.result=stored",
            ],
            activities.Select(item => Describe(item.Name, item.Tags)).Order(StringComparer.Ordinal));
        Assert.DoesNotContain(measurements.SelectMany(item => item.Tags), tag =>
            tag.Key.Contains("message.id", StringComparison.Ordinal)
            || tag.Key.Contains("payload", StringComparison.Ordinal)
            || tag.Key.Contains("address", StringComparison.Ordinal)
            || tag.Key.Contains("exception", StringComparison.Ordinal));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-JOURNAL-OBSERVABILITY", "activity-spans-policy-and-store-work")]
    public async Task Activity_SpansPolicyProjectionAndStorePersistenceAsync()
    {
        Activity? policyActivity = null;
        Activity? storeActivity = null;
        ActivitySpanId observedParentSpanId = default;
        var activities = new ConcurrentQueue<ActivityRecord>();
        var sampledTags = new ConcurrentQueue<KeyValuePair<string, object?>[]>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ServiceBusTelemetry.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) =>
            {
                if (options.Name != ServiceBusTelemetry.Activities.MessageJournalObserve)
                    return ActivitySamplingResult.None;

                sampledTags.Enqueue(options.Tags?.ToArray() ?? []);
                return ActivitySamplingResult.AllData;
            },
            ActivityStopped = activity =>
            {
                observedParentSpanId = activity.ParentSpanId;
                activities.Enqueue(new ActivityRecord(
                    activity.OperationName,
                    activity.Source.Version,
                    activity.Kind,
                    activity.Status,
                    activity.TagObjects.ToArray()));
            },
        };
        ActivitySource.AddActivityListener(listener);
        var store = new RecordingStore(onAppend: () => storeActivity = Activity.Current);
        var policy = new DelegatePolicy((capture, _) =>
        {
            policyActivity = Activity.Current;
            return ValueTask.FromResult<MessageJournalProjection?>(new MessageJournalProjection(
                MessageJournalDataClassification.Internal,
                capture.ContentType,
                capture.MessageTypes,
                capture.Metadata,
                capture.Headers,
                capture.Body));
        });

        using Activity parent = new Activity("message journal parent").Start();
        await CreateDriver(store, policy).ObserveAsync(
            MessageJournalOperation.Send,
            MessageJournalOutcome.Succeeded,
            "stored"u8.ToArray(),
            cancellationToken: TestContext.Current.CancellationToken);

        ActivityRecord completed = Assert.Single(activities);
        Assert.NotNull(policyActivity);
        Assert.Same(policyActivity, storeActivity);
        Assert.Equal(completed.Name, policyActivity.OperationName);
        Assert.Equal(ServiceBusTelemetry.Activities.MessageJournalObserve, completed.Name);
        Assert.Equal(ActivityStatusCode.Ok, completed.Status);
        Assert.Equal(parent.SpanId, observedParentSpanId);
        Assert.Equal(
            [
                new KeyValuePair<string, object?>(
                    ServiceBusTelemetry.Attributes.MessageJournalOperation,
                    "send"),
                new KeyValuePair<string, object?>(
                    ServiceBusTelemetry.Attributes.MessageJournalOutcome,
                    "succeeded"),
            ],
            Assert.Single(sampledTags));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-JOURNAL-OBSERVABILITY", "every-failure-path-has-one-stable-reason")]
    public async Task EveryFailurePath_EmitsExactlyOneStableReasonAsync()
    {
        var failureReasons = new ConcurrentQueue<string>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == ServiceBusTelemetry.MeterName
                && instrument.Name == ServiceBusTelemetry.Metrics.MessageJournalOperations)
                meterListener.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
        {
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                if (tag.Key == ServiceBusTelemetry.Attributes.MessageJournalFailureReason)
                    failureReasons.Enqueue(Assert.IsType<string>(tag.Value));
            }
        });
        listener.Start();

        await CreateDriver(
            new RecordingStore(),
            PassThroughPolicy(),
            new ThrowingTimestampTimeProvider()).ObserveAsync(
                MessageJournalOperation.Send,
                MessageJournalOutcome.Succeeded,
                "clock"u8.ToArray(),
                cancellationToken: TestContext.Current.CancellationToken);
        await CreateDriver(
            new RecordingStore(),
            PassThroughPolicy(),
            new ThrowingTimerTimeProvider()).ObserveAsync(
                MessageJournalOperation.Send,
                MessageJournalOutcome.Succeeded,
                "timer"u8.ToArray(),
                cancellationToken: TestContext.Current.CancellationToken);
        await CreateDriver(new RecordingStore(), PassThroughPolicy())
            .ObserveCaptureFailureAsync(new ExpectedJournalException());
        await CreateDriver(
            new RecordingStore(),
            new DelegatePolicy(static (_, _) =>
                ValueTask.FromException<MessageJournalProjection?>(new ExpectedJournalException())))
            .ObserveAsync(
                MessageJournalOperation.Send,
                MessageJournalOutcome.Succeeded,
                "policy"u8.ToArray(),
                cancellationToken: TestContext.Current.CancellationToken);
        await CreateDriver(new RecordingStore(new ExpectedJournalException()), PassThroughPolicy())
            .ObserveAsync(
                MessageJournalOperation.Send,
                MessageJournalOutcome.Succeeded,
                "store"u8.ToArray(),
                cancellationToken: TestContext.Current.CancellationToken);
        await CreateDriver(new RecordingStore(), PassThroughPolicy())
            .ObserveWithCanceledTokenAsync(
                MessageJournalOperation.Send,
                MessageJournalOutcome.Succeeded,
                "cancelled"u8.ToArray());
        await CreateDriver(
            new RecordingStore(maximumEntryBytes: 128),
            PassThroughPolicy()).ObserveAsync(
                MessageJournalOperation.Send,
                MessageJournalOutcome.Succeeded,
                new byte[129],
                cancellationToken: TestContext.Current.CancellationToken);

        var timeProvider = new FakeTimeProvider(ObservationTime);
        var blockingStore = new BlockingStore();
        Task timedOut = CreateDriver(
            blockingStore,
            PassThroughPolicy(),
            timeProvider,
            TimeSpan.FromMinutes(1)).ObserveAsync(
                MessageJournalOperation.Send,
                MessageJournalOutcome.Succeeded,
                "timeout"u8.ToArray(),
                cancellationToken: TestContext.Current.CancellationToken);
        await blockingStore.Entered.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        timeProvider.Advance(TimeSpan.FromMinutes(1));
        await timedOut.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.Equal(
            [
                "cancelled",
                "capture",
                "clock",
                "entry_too_large",
                "policy",
                "store",
                "timeout",
                "timeout_setup",
            ],
            failureReasons.Order(StringComparer.Ordinal));
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

        using (var activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "ViciOne.ServiceBus",
            Sample = static (ref ActivityCreationOptions<ActivityContext> options) =>
            {
                if (options.Name == ServiceBusTelemetry.Activities.MessageJournalObserve)
                    throw new TelemetryObserverException();

                return ActivitySamplingResult.None;
            },
        })
        {
            ActivitySource.AddActivityListener(activityListener);

            await CreateDriver(store, PassThroughPolicy()).ObserveAsync(
                MessageJournalOperation.Publish,
                MessageJournalOutcome.Succeeded,
                "stored-again"u8.ToArray(),
                cancellationToken: TestContext.Current.CancellationToken);
        }

        using (var parent = new Activity("journal business parent").Start())
        using (var startingListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ServiceBusTelemetry.ActivitySourceName,
            Sample = static (ref ActivityCreationOptions<ActivityContext> options) =>
                options.Name == ServiceBusTelemetry.Activities.MessageJournalObserve
                    ? ActivitySamplingResult.AllData
                    : ActivitySamplingResult.None,
            ActivityStarted = activity =>
            {
                if (activity.OperationName == ServiceBusTelemetry.Activities.MessageJournalObserve)
                    throw new TelemetryObserverException();
            },
        })
        {
            ActivitySource.AddActivityListener(startingListener);
            Activity? observedAtStore = null;
            var startingStore = new RecordingStore(onAppend: () => observedAtStore = Activity.Current);
            try
            {
                await CreateDriver(startingStore, PassThroughPolicy()).ObserveAsync(
                    MessageJournalOperation.Send,
                    MessageJournalOutcome.Succeeded,
                    "stored-after-start-failure"u8.ToArray(),
                    cancellationToken: TestContext.Current.CancellationToken);

                Assert.Single(startingStore.Entries);
                Assert.Same(parent, observedAtStore);
                Assert.Same(parent, Activity.Current);
            }
            finally
            {
                Activity.Current = parent;
            }
        }

        using var stopParent = new Activity("journal stop parent").Start();
        using var stoppingListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "ViciOne.ServiceBus",
            Sample = static (ref ActivityCreationOptions<ActivityContext> options) =>
                options.Name == ServiceBusTelemetry.Activities.MessageJournalObserve
                    ? ActivitySamplingResult.AllData
                    : ActivitySamplingResult.None,
            ActivityStopped = static _ => throw new TelemetryObserverException(),
        };
        ActivitySource.AddActivityListener(stoppingListener);

        try
        {
            await CreateDriver(store, PassThroughPolicy()).ObserveAsync(
                MessageJournalOperation.Consume,
                MessageJournalOutcome.Succeeded,
                "stored-after-stop-failure"u8.ToArray(),
                cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(3, store.Entries.Count);
            MessageJournalTelemetry.Scope stoppedActivity = MessageJournalTelemetry.StartActivity(
                MessageJournalOperation.Send, MessageJournalOutcome.Succeeded);
            Assert.NotNull(stoppedActivity.Activity);
            MessageJournalTelemetry.Stored(
                stoppedActivity, MessageJournalOperation.Send, MessageJournalOutcome.Succeeded, TimeSpan.Zero);
            Assert.Same(stopParent, Activity.Current);
        }
        finally
        {
            Activity.Current = stopParent;
        }
    }

    private static MessageJournalWriterTestDriver CreateDriver(
        IMessageJournalStore store,
        IMessageJournalPolicy policy,
        TimeProvider? timeProvider = null,
        TimeSpan? writeTimeout = null) =>
        new(
            store,
            policy,
            MessageJournalOptions.ContinueMessageFlow(
                writeTimeout ?? TimeSpan.FromSeconds(5),
                timeProvider ?? new FakeTimeProvider(ObservationTime)));

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

    private class RecordingStore(
        Exception? failure = null,
        Action? onAppend = null,
        int maximumEntryBytes = 4096) : IMessageJournalStore
    {
        private readonly List<MessageJournalEntry> _entries = [];

        public IReadOnlyList<MessageJournalEntry> Entries => _entries;

        public MessageJournalStoreLimits Limits { get; } =
            new(maximumEntryBytes, maximumEntries: 100, retentionPeriod: TimeSpan.FromDays(1));

        public virtual ValueTask AppendAsync(MessageJournalEntry entry, CancellationToken cancellationToken)
        {
            onAppend?.Invoke();
            cancellationToken.ThrowIfCancellationRequested();
            if (failure is not null)
                return ValueTask.FromException(failure);

            _entries.Add(entry);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class BlockingStore : RecordingStore
    {
        private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Entered => _entered.Task;

        public override async ValueTask AppendAsync(
            MessageJournalEntry entry,
            CancellationToken cancellationToken)
        {
            _entered.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }
    }

    private sealed class ThrowingTimerTimeProvider : TimeProvider
    {
        public override ITimer CreateTimer(
            TimerCallback callback,
            object? state,
            TimeSpan dueTime,
            TimeSpan period) => throw new ExpectedJournalException();
    }

    private sealed class ThrowingTimestampTimeProvider : TimeProvider
    {
        public override long GetTimestamp() => throw new ExpectedJournalException();
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
        ActivityStatusCode Status,
        KeyValuePair<string, object?>[] Tags);

    private sealed record InstrumentRecord(Type Type, string? Unit, string? Description);

    private sealed class ExpectedJournalException : Exception;
    private sealed class ExpectedStoreException : Exception;
    private sealed class TelemetryObserverException : Exception;
}
