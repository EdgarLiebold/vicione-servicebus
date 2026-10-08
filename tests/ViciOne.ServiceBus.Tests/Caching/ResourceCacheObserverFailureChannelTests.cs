using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Caching;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Caching;

public sealed class ResourceCacheObserverFailureChannelTests
{
    public enum Change { Added, Removed, Cleared }
    public enum Failure { None, Synchronous, Asynchronous, Cancellation }
    public enum Diagnostics { Recording, Absent, Disabled, GetterThrows, IsEnabledThrows, LogThrows }

    public static TheoryData<Change, Failure, Diagnostics> FailureCases
    {
        get
        {
            var data = new TheoryData<Change, Failure, Diagnostics>();
            foreach (Change change in Enum.GetValues<Change>())
            foreach (Failure failure in new[] { Failure.Synchronous, Failure.Asynchronous, Failure.Cancellation })
            foreach (Diagnostics diagnostics in Enum.GetValues<Diagnostics>())
                data.Add(change, failure, diagnostics);
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(FailureCases))]
    [RequirementCoverage("REQ-VSB-CACHE-OBSERVER", "complete-fanout-and-best-effort-failure-channel")]
    public Task ObserverFailures_PreserveCommittedStateCompleteFanoutAndBestEffortDiagnosticsAsync(
        Change change, Failure failure, Diagnostics diagnostics) => RunAsync(change, failure, diagnostics);

    [Theory]
    [InlineData(Change.Added)]
    [InlineData(Change.Removed)]
    [InlineData(Change.Cleared)]
    [RequirementCoverage("REQ-VSB-CACHE-OBSERVER", "healthy-fanout-emits-no-failure-diagnostic")]
    public Task HealthyObservers_CompleteEachCommittedChangeWithoutFailureDiagnosticsAsync(Change change) =>
        RunAsync(change, Failure.None, Diagnostics.Recording);

    private static async Task RunAsync(Change change, Failure failure, Diagnostics diagnostics)
    {
        var time = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
        await using var cache = new ResourceCache<Resource>(new ResourceCacheOptions(capacity: 1,
            minAge: TimeSpan.Zero, maxAge: TimeSpan.FromHours(1), timeProvider: time,
            cleanupInterval: TimeSpan.FromMinutes(1)));
        IResourceCacheIndex<string, Resource> index = cache.AddIndex("id", value => value.Id);
        var resource = new Resource("owned");
        using var caller = new CancellationTokenSource();
        using var observerCancellation = new CancellationTokenSource();
        observerCancellation.Cancel();
        Exception expected = failure == Failure.Cancellation
            ? new OperationCanceledException("observer cancellation", observerCancellation.Token)
            : new ObserverException("observer fault");
        if (change != Change.Added)
            await cache.AddAsync(resource, caller.Token);

        var calls = new List<Call>();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task? callbackTask = null;
        Exception? callbackError = null;
        Exception? operationError = null;
        bool? completedBeforeRelease = null;
        string[]? fanoutBeforeRelease = null;
        int? committedCountBeforeRelease = null;
        Resource[]? valuesBeforeRelease = null;
        int? disposalsBeforeRelease = null;
        Task<bool>? removal = null;
        var logger = new ProbeLogger(diagnostics);
        var getter = new ThrowingWarningContext(logger);
        using var logging = new LoggingScope(diagnostics, logger, getter);
        using ConnectHandle before = cache.Connect(new Observer((kind, value, token) => Observe("before", kind, value, token)));
        using ConnectHandle middle = cache.Connect(new Observer((kind, value, token) => Observe("middle", kind, value, token)));
        using ConnectHandle after = cache.Connect(new Observer((kind, value, token) => Observe("after", kind, value, token)));

        Task operation = change switch
        {
            Change.Added => cache.AddAsync(resource, caller.Token).AsTask(),
            Change.Removed => removal = index.RemoveAsync(resource.Id, caller.Token).AsTask(),
            Change.Cleared => cache.ClearAsync(caller.Token).AsTask(),
            _ => throw new ArgumentOutOfRangeException(nameof(change))
        };
        try
        {
            if (failure == Failure.Asynchronous)
            {
                await entered.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
                completedBeforeRelease = operation.IsCompleted;
                fanoutBeforeRelease = calls.Select(call => call.Observer).ToArray();
                committedCountBeforeRelease = cache.Statistics.Count;
                valuesBeforeRelease = cache.GetValues(caller.Token).ToArray();
                disposalsBeforeRelease = resource.DisposeCount;
            }
        }
        finally
        {
            // Observe and join both actual tasks before any assertion can strand the cache's active-operation drain.
            release.TrySetResult();
            if (callbackTask is not null)
                callbackError = await Record.ExceptionAsync(() => callbackTask);
            operationError = await Record.ExceptionAsync(() => operation);
        }

        if (failure == Failure.Asynchronous)
        {
            Assert.False(completedBeforeRelease);
            Assert.NotNull(fanoutBeforeRelease);
            Assert.Equal(["before", "middle"], fanoutBeforeRelease);
            Assert.Same(expected, callbackError);
            Assert.Equal(change == Change.Added ? 1 : 0, committedCountBeforeRelease);
            Assert.Equal(change == Change.Cleared ? 1 : 0, disposalsBeforeRelease);
            Assert.NotNull(valuesBeforeRelease);
            if (change == Change.Added)
                Assert.Same(resource, Assert.Single(valuesBeforeRelease));
            else
                Assert.Empty(valuesBeforeRelease);
        }

        // Removal also has an outer release catch: loss of dispatch isolation need not throw to its caller.
        Assert.Equal(["before", "middle", "after"], calls.Select(call => call.Observer).ToArray());
        Assert.Null(operationError);
        if (change == Change.Removed)
        {
            Assert.NotNull(removal);
            Assert.True(await removal);
        }
        Assert.All(calls, call =>
        {
            Assert.Equal(change, call.Kind);
            if (change == Change.Cleared)
                Assert.Null(call.Value);
            else
                Assert.Same(resource, call.Value);
            Assert.Equal(calls[0].Token, call.Token);
            Assert.NotEqual(caller.Token, call.Token);
            Assert.True(call.Token.CanBeCanceled);
            Assert.False(call.Token.IsCancellationRequested);
        });
        Assert.Equal(0, cache.Statistics.CreationFaults);
        Assert.Equal(0, cache.Statistics.PendingCreations);
        Assert.Equal(1, cache.Statistics.TotalCreated);
        if (change == Change.Added)
        {
            Assert.Equal(1, cache.Statistics.Count);
            Assert.Equal(0, resource.DisposeCount);
            Assert.Same(resource, await index.GetAsync(resource.Id, caller.Token));
        }
        else
        {
            Assert.Equal(0, cache.Statistics.Count);
            Assert.Equal(1, resource.DisposeCount);
            Assert.Empty(cache.GetValues(caller.Token));
            await Assert.ThrowsAsync<KeyNotFoundException>(async () => await index.GetAsync(resource.Id, caller.Token));
        }

        if (failure == Failure.None)
        {
            Assert.Empty(logger.Records);
            Assert.Empty(logger.EnabledLevels);
        }
        else
        {
            bool logged = diagnostics is Diagnostics.Recording or Diagnostics.LogThrows;
            if (logged)
            {
                LogRecord record = Assert.Single(logger.Records);
                Assert.Equal(LogLevel.Warning, record.Level);
                Assert.Equal(default, record.EventId);
                Assert.Same(expected, record.Error);
                Assert.Equal(change switch
                {
                    Change.Added => "Resource cache observer faulted after resource add",
                    Change.Removed => "Resource cache observer faulted after resource removal",
                    Change.Cleared => "Resource cache observer faulted after cache clear",
                    _ => throw new ArgumentOutOfRangeException(nameof(change))
                }, record.Message);
            }
            else
                Assert.Empty(logger.Records);

            if (diagnostics is Diagnostics.Absent or Diagnostics.GetterThrows)
                Assert.Empty(logger.EnabledLevels);
            else
                Assert.Equal([LogLevel.Warning], logger.EnabledLevels);
            Assert.Equal(diagnostics == Diagnostics.GetterThrows ? 1 : 0, getter.WarningAttempts);
        }

        ValueTask Observe(string name, Change kind, Resource? value, CancellationToken token)
        {
            calls.Add(new Call(name, kind, value, token));
            if (name != "middle" || failure == Failure.None)
                return default;
            if (failure == Failure.Asynchronous)
            {
                callbackTask = FailAfterReleaseAsync();
                return new ValueTask(callbackTask);
            }
            throw expected;
        }

        async Task FailAfterReleaseAsync()
        {
            entered.TrySetResult();
            await release.Task.ConfigureAwait(false);
            throw expected;
        }
    }

    private sealed record Call(string Observer, Change Kind, Resource? Value, CancellationToken Token);
    private sealed record LogRecord(LogLevel Level, EventId EventId, Exception? Error, string Message);
    private sealed class ObserverException(string message) : Exception(message);

    private sealed class Resource(string id) : IAsyncDisposable
    {
        private int _disposeCount;
        public string Id { get; } = id;
        public int DisposeCount => Volatile.Read(ref _disposeCount);
        public ValueTask DisposeAsync()
        {
            Interlocked.Increment(ref _disposeCount);
            return default;
        }
    }

    private sealed class Observer(Func<Change, Resource?, CancellationToken, ValueTask> callback)
        : IResourceCacheObserver<Resource>
    {
        public ValueTask ResourceAddedAsync(Resource value, CancellationToken cancellationToken) =>
            callback(Change.Added, value, cancellationToken);
        public ValueTask ResourceRemovedAsync(Resource value, CancellationToken cancellationToken) =>
            callback(Change.Removed, value, cancellationToken);
        public ValueTask CacheClearedAsync(CancellationToken cancellationToken) =>
            callback(Change.Cleared, null, cancellationToken);
    }

    private sealed class ProbeLogger(Diagnostics diagnostics) : ILogger
    {
        public List<LogRecord> Records { get; } = [];
        public List<LogLevel> EnabledLevels { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel)
        {
            EnabledLevels.Add(logLevel);
            if (diagnostics == Diagnostics.IsEnabledThrows)
                throw new InvalidOperationException("IsEnabled failed");
            return diagnostics != Diagnostics.Disabled;
        }
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Records.Add(new LogRecord(logLevel, eventId, exception, formatter(state, exception)));
            if (diagnostics == Diagnostics.LogThrows)
                throw new InvalidOperationException("Log failed");
        }
    }

    private sealed class ThrowingWarningContext(ILogger logger) : ILogContext
    {
        public int WarningAttempts { get; private set; }
        public ILogger Logger => logger;
        public ILogContext Messages => this;
        public EnabledLogger? Critical => null;
        public EnabledLogger? Debug => null;
        public EnabledLogger? Error => null;
        public EnabledLogger? Info => null;
        public EnabledLogger? Trace => null;
        public EnabledLogger? Warning
        {
            get
            {
                WarningAttempts++;
                throw new InvalidOperationException("Warning getter failed");
            }
        }
        public ILogContext CreateLogContext(string categoryName) => this;
    }

    private sealed class LoggingScope : IDisposable
    {
        private readonly ILogContext? _previous = LogContext.Current;
        public LoggingScope(Diagnostics diagnostics, ILogger logger, ILogContext getter)
        {
            if (diagnostics == Diagnostics.Absent)
                LogContext.Current = null;
            else if (diagnostics == Diagnostics.GetterThrows)
                LogContext.Current = getter;
            else
                LogContext.ConfigureCurrentLogContext(logger);
        }
        public void Dispose() => LogContext.Current = _previous;
    }
}
