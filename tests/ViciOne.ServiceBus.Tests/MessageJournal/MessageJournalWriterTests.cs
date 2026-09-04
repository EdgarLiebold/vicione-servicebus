using System.Runtime.InteropServices;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.MessageJournal;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.MessageJournal;
using Xunit;

namespace ViciOne.ServiceBus.Tests.MessageJournal;

public sealed class MessageJournalWriterTests
{
    private static readonly DateTimeOffset ObservationTime =
        new(2030, 1, 2, 3, 4, 5, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-JOURNAL-SANITIZATION", "raw-data-never-reaches-store")]
    public async Task Policy_IsTheOnlyBoundaryBetweenRawCaptureAndStoredEntry()
    {
        byte[] rawBody = "raw-secret"u8.ToArray();
        var rawMetadata = new Dictionary<string, string>(StringComparer.Ordinal) { ["secret"] = "raw-secret" };
        var rawHeaders = new Dictionary<string, string>(StringComparer.Ordinal) { ["authorization"] = "raw-secret" };
        var store = new RecordingStore();
        var policy = new DelegatePolicy((capture, _) =>
        {
            Assert.Equal(rawBody, capture.Body.ToArray());
            Assert.Equal("raw-secret", capture.Metadata["secret"]);
            Assert.Equal("raw-secret", capture.Headers["authorization"]);

            return ValueTask.FromResult<MessageJournalProjection?>(new MessageJournalProjection(
                MessageJournalDataClassification.Confidential,
                "application/json",
                messageTypes: ["urn:message:sanitized"],
                metadata: new Dictionary<string, string> { ["tenant"] = "north" },
                headers: new Dictionary<string, string> { ["trace"] = "safe" },
                body: "redacted"u8.ToArray()));
        });
        var driver = CreateDriver(store, policy);

        await driver.ObserveAsync(
            MessageJournalOperation.Publish,
            MessageJournalOutcome.Succeeded,
            rawBody,
            rawMetadata,
            rawHeaders,
            TestContext.Current.CancellationToken);

        MessageJournalEntry entry = Assert.Single(store.Entries);
        Assert.Equal(MessageJournalOperation.Publish, entry.Operation);
        Assert.Equal(MessageJournalOutcome.Succeeded, entry.Outcome);
        Assert.Equal(MessageJournalDataClassification.Confidential, entry.DataClassification);
        Assert.Equal(["urn:message:sanitized"], entry.MessageTypes);
        Assert.Equal(new Dictionary<string, string> { ["tenant"] = "north" }, entry.Metadata);
        Assert.Equal(new Dictionary<string, string> { ["trace"] = "safe" }, entry.Headers);
        Assert.Equal("redacted"u8.ToArray(), entry.Body.ToArray());
        Assert.DoesNotContain(entry.Metadata.Values, value => value == "raw-secret");
        Assert.DoesNotContain(entry.Headers.Values, value => value == "raw-secret");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-JOURNAL-FILTER", "null-projection-excludes-observation")]
    public async Task NullProjection_FiltersTheObservationWithoutCallingTheStore()
    {
        var store = new RecordingStore();
        var driver = CreateDriver(store, new DelegatePolicy(static (_, _) =>
            ValueTask.FromResult<MessageJournalProjection?>(null)));

        await driver.ObserveAsync(
            MessageJournalOperation.Send,
            MessageJournalOutcome.Succeeded,
            "excluded"u8.ToArray(),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Empty(store.Entries);
        Assert.Equal(0, store.AppendAttempts);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-JOURNAL-BOUNDS", "oversized-entry-fails-closed")]
    public async Task OversizedSanitizedEntry_IsRejectedBeforePersistence()
    {
        var store = new RecordingStore(maximumEntryBytes: 128);
        var driver = CreateDriver(store, PassThroughPolicy());

        await driver.ObserveAsync(
            MessageJournalOperation.Send,
            MessageJournalOutcome.Succeeded,
            new byte[129],
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Empty(store.Entries);
        Assert.Equal(0, store.AppendAttempts);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-JOURNAL-FAILURE-ISOLATION", "capture-policy-and-store-failures-are-contained")]
    public async Task CapturePolicyAndStoreFailures_NeverEscapeTheJournalBoundary()
    {
        var expectedCaptureFailure = new ExpectedJournalException("capture");
        var captureDriver = CreateDriver(new RecordingStore(), PassThroughPolicy());
        await captureDriver.ObserveCaptureFailureAsync(expectedCaptureFailure);

        var policyStore = new RecordingStore();
        var policyDriver = CreateDriver(policyStore, new DelegatePolicy(static (_, _) =>
            ValueTask.FromException<MessageJournalProjection?>(new ExpectedJournalException("policy"))));
        await policyDriver.ObserveAsync(
            MessageJournalOperation.Send,
            MessageJournalOutcome.Succeeded,
            "message"u8.ToArray(),
            cancellationToken: TestContext.Current.CancellationToken);

        var storeFailure = new ExpectedJournalException("store");
        var failingStore = new RecordingStore(storeFailure: storeFailure);
        var storeDriver = CreateDriver(failingStore, PassThroughPolicy());
        await storeDriver.ObserveAsync(
            MessageJournalOperation.Consume,
            MessageJournalOutcome.Faulted,
            "message"u8.ToArray(),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Empty(policyStore.Entries);
        Assert.Equal(1, failingStore.AppendAttempts);
        Assert.Empty(failingStore.Entries);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-JOURNAL-FAILURE-ISOLATION", "caller-cancellation-is-contained")]
    public async Task CallerCancellation_IsObservedWithoutEscapingOrPersisting()
    {
        var store = new RecordingStore();
        var driver = CreateDriver(store, PassThroughPolicy());
        await driver.ObserveWithCanceledTokenAsync(
            MessageJournalOperation.Consume,
            MessageJournalOutcome.Succeeded,
            "message"u8.ToArray());

        Assert.Equal(1, store.AppendAttempts);
        Assert.Empty(store.Entries);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-JOURNAL-FAILURE-ISOLATION", "provider-timeout-uses-configured-clock")]
    public async Task ProviderTimeout_UsesTheConfiguredClockAndDoesNotWaitOnWallTime()
    {
        var timeProvider = new FakeTimeProvider(ObservationTime);
        var store = new BlockingStore();
        var driver = CreateDriver(store, PassThroughPolicy(), timeProvider, TimeSpan.FromMinutes(1));

        Task observation = driver.ObserveAsync(
            MessageJournalOperation.Send,
            MessageJournalOutcome.Succeeded,
            "message"u8.ToArray(),
            cancellationToken: TestContext.Current.CancellationToken);
        await store.Entered.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);

        timeProvider.Advance(TimeSpan.FromMinutes(1));
        await observation.WaitAsync(OperationTimeout, TestContext.Current.CancellationToken);

        Assert.True(store.CancellationObserved);
        Assert.Empty(store.Entries);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-JOURNAL-SNAPSHOT", "entry-identity-time-and-content-are-stable")]
    public async Task StoredEntry_HasStableVersionSevenIdentityTimeAndDetachedContent()
    {
        var timeProvider = new FakeTimeProvider(ObservationTime);
        var store = new RecordingStore();
        byte[] source = [1, 2, 3];
        var driver = CreateDriver(store, PassThroughPolicy(), timeProvider);

        await driver.ObserveAsync(
            MessageJournalOperation.Consume,
            MessageJournalOutcome.Faulted,
            source,
            cancellationToken: TestContext.Current.CancellationToken);
        source[0] = 9;

        MessageJournalEntry entry = Assert.Single(store.Entries);
        Assert.Equal(7, entry.EntryId.Version);
        Assert.Equal(ObservationTime, entry.ObservedAt);
        Assert.Equal(MessageJournalOperation.Consume, entry.Operation);
        Assert.Equal(MessageJournalOutcome.Faulted, entry.Outcome);
        Assert.Equal(new byte[] { 1, 2, 3 }, entry.Body.ToArray());
        Assert.True(entry.ContentSizeInBytes >= 131);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-JOURNAL-SNAPSHOT", "no-body-stage-exposes-mutable-backing-storage")]
    public async Task CaptureProjectionAndEntry_DoNotExposeMutableBodyBackingStorage()
    {
        var projection = new MessageJournalProjection(
            MessageJournalDataClassification.Internal,
            "application/octet-stream",
            body: new byte[] { 1, 2, 3 });
        MutateReturnedMemory(projection.Body);
        Assert.Equal(new byte[] { 1, 2, 3 }, projection.Body.ToArray());

        var store = new RecordingStore();
        var policy = new DelegatePolicy((capture, _) =>
        {
            MutateReturnedMemory(capture.Body);
            Assert.Equal(new byte[] { 1, 2, 3 }, capture.Body.ToArray());
            return ValueTask.FromResult<MessageJournalProjection?>(new MessageJournalProjection(
                MessageJournalDataClassification.Internal,
                capture.ContentType,
                body: capture.Body));
        });

        await CreateDriver(store, policy).ObserveAsync(
            MessageJournalOperation.Send,
            MessageJournalOutcome.Succeeded,
            new byte[] { 1, 2, 3 },
            cancellationToken: TestContext.Current.CancellationToken);

        MessageJournalEntry entry = Assert.Single(store.Entries);
        MutateReturnedMemory(entry.Body);
        Assert.Equal(new byte[] { 1, 2, 3 }, entry.Body.ToArray());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-JOURNAL-BOUNDS", "escaped-json-content-counted-before-persistence")]
    public async Task EscapedJsonContent_IsCountedBeforeTheStoreLimitIsApplied()
    {
        var store = new RecordingStore(maximumEntryBytes: 400);
        var policy = new DelegatePolicy(static (_, _) =>
            ValueTask.FromResult<MessageJournalProjection?>(new MessageJournalProjection(
                MessageJournalDataClassification.Internal,
                "application/json",
                metadata: new Dictionary<string, string>
                {
                    ["escaped"] = new string('\0', 100),
                })));

        await CreateDriver(store, policy).ObserveAsync(
            MessageJournalOperation.Send,
            MessageJournalOutcome.Succeeded,
            ReadOnlyMemory<byte>.Empty,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(0, store.AppendAttempts);
        Assert.Empty(store.Entries);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-JOURNAL-FAILURE-ISOLATION", "clock-failure-is-contained")]
    public async Task ThrowingTimeProvider_CannotEscapeTheJournalBoundary()
    {
        var store = new RecordingStore();
        var driver = CreateDriver(store, PassThroughPolicy(), new ThrowingTimestampTimeProvider());

        await driver.ObserveAsync(
            MessageJournalOperation.Publish,
            MessageJournalOutcome.Succeeded,
            "message"u8.ToArray(),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(0, store.AppendAttempts);
        Assert.Empty(store.Entries);
    }

    private static void MutateReturnedMemory(ReadOnlyMemory<byte> body)
    {
        Assert.True(MemoryMarshal.TryGetArray(body, out ArraySegment<byte> segment));
        segment.Array![segment.Offset] = 9;
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

    private class RecordingStore(
        int maximumEntryBytes = 4096,
        Exception? storeFailure = null) : IMessageJournalStore
    {
        private readonly List<MessageJournalEntry> _entries = [];
        private int _appendAttempts;

        public IReadOnlyList<MessageJournalEntry> Entries => _entries;

        public int AppendAttempts => Volatile.Read(ref _appendAttempts);

        public MessageJournalStoreLimits Limits { get; } =
            new(maximumEntryBytes, maximumEntries: 100, retentionPeriod: TimeSpan.FromDays(1));

        public virtual ValueTask AppendAsync(MessageJournalEntry entry, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _appendAttempts);
            cancellationToken.ThrowIfCancellationRequested();

            if (storeFailure is not null)
                return ValueTask.FromException(storeFailure);

            _entries.Add(entry);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class BlockingStore : RecordingStore
    {
        private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _cancellationObserved;

        public Task Entered => _entered.Task;

        public bool CancellationObserved => Volatile.Read(ref _cancellationObserved) == 1;

        public override async ValueTask AppendAsync(MessageJournalEntry entry, CancellationToken cancellationToken)
        {
            _entered.TrySetResult();
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                Interlocked.Exchange(ref _cancellationObserved, 1);
                throw;
            }
        }
    }

    private sealed class ExpectedJournalException(string message) : Exception(message);

    private sealed class ThrowingTimestampTimeProvider : TimeProvider
    {
        public override long GetTimestamp() => throw new ExpectedJournalException("clock");
    }
}
