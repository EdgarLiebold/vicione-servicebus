using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Channels;
using global::Amazon.SimpleNotificationService;
using global::Amazon.SimpleNotificationService.Model;
using global::Amazon.SQS;
using global::Amazon.SQS.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.AmazonSqs.Configuration;
using ViciOne.ServiceBus.AmazonSqs.Tests.TestDoubles;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

public sealed class AmazonSqsBatchSelectedClockTests
{
    static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(1);
    static readonly TimeSpan Guard = TimeSpan.FromSeconds(10);

    [Theory]
    [InlineData("send-existing")]
    [InlineData("send-created")]
    [InlineData("delete-existing")]
    [InlineData("delete-created")]
    [InlineData("publish-existing")]
    [InlineData("publish-created")]
    [RequirementCoverage("REQ-VSB-AWS-BATCH-CLOCK", "all-resource-paths-use-selected-owner-timer")]
    public Task ConnectionOwnerClock_FlushesPartialBatchAsync(string path) =>
        RunAsync(path, async h =>
        {
            await h.ResolveAsync(path);
            h.AssertResolutionPath(path);
            await AssertSelectedBatchAsync(h, path.Split('-')[0], "one");
        });

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-BATCH-CLOCK", "owner-clock-captured-before-resolution-and-kept-across-scopes")]
    public Task ConnectionOwnerClock_RemainsFixedAfterPayloadAndScopeUpdatesAsync() =>
        RunAsync("send-existing", async h =>
        {
            h.HoldMetadata = true;
            Task resolution = h.ResolveAsync("send-existing");
            await h.MetadataEntered.Task.WaitAsync(Guard);
            var replacement = h.OwnClock(new RecordingClock());
            var scopedClock = h.OwnClock(new RecordingClock());
            h.Connection.SetTimeProvider(replacement);
            h.ReleaseMetadata();
            await resolution.WaitAsync(Guard);
            Assert.Same(replacement, h.Connection.GetTimeProvider());

            using var scope = new ScopeClientContext(h.Client, h.Caller.Token);
            scope.SetTimeProvider(scopedClock);
            Assert.Same(scopedClock, scope.GetTimeProvider());
            await AssertSelectedBatchAsync(h, "send", "first", scope, unexpectedClocks: [replacement, scopedClock]);

            await h.ResolveAsync("send-existing", "next");
            await AssertSelectedBatchAsync(h, "send", "next", scope, "next", unexpectedClocks: [replacement, scopedClock]);
            await h.ResolveAsync("publish-existing");
            await AssertSelectedBatchAsync(h, "publish", "published", scope, unexpectedClocks: [replacement, scopedClock]);
            Assert.Empty(replacement.Timers);
            Assert.Empty(scopedClock.Timers);
        });

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-BATCH-CLOCK", "legacy-constructors-preserve-request-local-accounting-and-drain")]
    public Task LegacyConstructors_TenEntriesKeepProviderAccountingAndDrainAsync() =>
        RunAsync("legacy", async h =>
        {
            QueueInfo queue = h.Own(new QueueInfo("orders", Harness.QueueUrl("orders"),
                Harness.QueueAttributes("orders"), h.Sqs, h.Lifetime.Token, true));
            TopicInfo topic = h.Own(new TopicInfo("events", Harness.TopicArn("events"),
                h.Sns, h.Lifetime.Token, true));
            var operations = new List<Task>();
            for (var index = 0; index < 10; index++)
            {
                operations.Add(h.Track(queue.SendAsync(new SendMessageBatchRequestEntry("", "send-" + index), h.Caller.Token)));
                operations.Add(h.Track(queue.DeleteAsync("delete-" + index, h.Caller.Token)));
                operations.Add(h.Track(topic.PublishAsync(new PublishBatchRequestEntry { Message = "publish-" + index }, h.Caller.Token)));
            }

            await h.SdkEntered.Task.WaitAsync(Guard);
            Task queueDispose = h.Track(queue.DisposeAsync().AsTask());
            Task topicDispose = h.Track(topic.DisposeAsync().AsTask());
            Assert.False(Task.WhenAll(queueDispose, topicDispose).IsCompleted);
            h.ReleaseSdk();
            await Task.WhenAll(operations.Append(queueDispose).Append(topicDispose)).WaitAsync(Guard);

            foreach (string kind in new[] { "send", "delete", "publish" })
            {
                CapturedRequest[] requests = h.Requests.Where(x => x.Kind == kind).ToArray();
                Assert.NotEmpty(requests);
                Assert.Equal(Enumerable.Range(0, 10).Select(x => kind + "-" + x).Order(StringComparer.Ordinal),
                    requests.SelectMany(x => x.Values).Order(StringComparer.Ordinal));
                foreach (CapturedRequest request in requests)
                {
                    Assert.InRange(request.Values.Length, 1, 10);
                    Assert.Equal(Enumerable.Range(0, request.Values.Length).Select(x => x.ToString(CultureInfo.InvariantCulture)), request.Ids);
                    Assert.Equal(kind == "publish" ? Harness.TopicArn("events") : Harness.QueueUrl("orders"), request.Address);
                    Assert.Equal(h.Lifetime.Token, request.Token);
                }
            }
            Assert.Empty(h.Clock.Timers);
        }, attachClock: false);

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-BATCH-CLOCK", "timer-failure-closes-and-faults-all-queued-work-before-cleanup")]
    public async Task TimerCreationFailure_FaultsAdmittedWorkAndKeepsDisposalJoinableAsync()
    {
        var previous = LogContext.Current;
        var logger = new HostileErrorLogger();
        try
        {
            LogContext.ConfigureCurrentLogContext(logger);
            await RunAsync("publish-existing", async h =>
            {
                await h.ResolveAsync("publish-existing");
                h.Clock.ThrowTimer = true;
                Task first = h.Start("publish", "first");
                await Task.WhenAny(h.Clock.TimerInvoked.Task, h.SdkEntered.Task).WaitAsync(Guard);
                Assert.True(h.Clock.TimerInvoked.Task.IsCompletedSuccessfully);

                TopicInfo topic = Assert.Single(h.Topics);
                IBatcher<PublishBatchRequestEntry> batcher =
                    Assert.IsType<Lazy<IBatcher<PublishBatchRequestEntry>>>(Field(topic, "_batchPublisher", typeof(TopicInfo))).Value;
                Task worker = (Task)Field(batcher, "_batchTask", typeof(Batcher<PublishBatchRequestEntry>));
                var channel = (Channel<BatchEntry<PublishBatchRequestEntry>>)Field(batcher, "_channel", typeof(Batcher<PublishBatchRequestEntry>));
                h.FailedBatchWorker = worker;
                h.RecoverFailedBatch = () =>
                {
                    while (channel.Reader.TryRead(out BatchEntry<PublishBatchRequestEntry>? entry))
                        entry.SetFaulted(h.Clock.TimerFailure);
                };
                Task second = h.Start("publish", "second");
                Assert.Equal(2, channel.Reader.Count);
                Assert.True(channel.Reader.TryPeek(out BatchEntry<PublishBatchRequestEntry>? admitted));
                Assert.NotNull(admitted);
                Assert.False(admitted.Completed.IsCompleted);
                h.Clock.ReleaseThrow();
                await worker.WaitAsync(Guard);

                // All product terminal-state assertions precede any fixture Dispose or rescue drain.
                Assert.True(channel.Reader.Completion.IsFaulted);
                Assert.Equal(0, channel.Reader.Count);
                Assert.Same(h.Clock.TimerFailure, channel.Reader.Completion.Exception!.InnerException);
                Assert.Same(h.Clock.TimerFailure, await Assert.ThrowsAsync<ChannelClosedException>(() => first.WaitAsync(Guard, TestContext.Current.CancellationToken)));
                Assert.Same(h.Clock.TimerFailure, await Assert.ThrowsAsync<ChannelClosedException>(() => second.WaitAsync(Guard, TestContext.Current.CancellationToken)));
                Assert.Empty(h.Requests);
                Assert.Equal(1, logger.Attempts);
                Assert.Same(h.Clock.TimerFailure, logger.OriginalCause);
            });
        }
        finally { LogContext.Current = previous; }
    }

    static object Field(object instance, string name, Type declaringType) =>
        declaringType.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(instance)!;

    static async Task AssertSelectedBatchAsync(Harness h, string kind, string value, ClientContext? scope = null, string? name = null,
        RecordingClock[]? unexpectedClocks = null)
    {
        int timerCount = h.Clock.Timers.Count;
        int ttlTimerCount = h.TtlClock.Timers.Count;
        int requestCount = h.Requests.Count;
        Task selected = h.Clock.ObserveNextTimer(false);
        Task unexpectedTtlBatch = h.TtlClock.ObserveNextTimer(true);
        Task sdk = h.ObserveNextSdk();
        var signals = new List<Task> { selected, unexpectedTtlBatch, sdk };
        if (unexpectedClocks is not null)
            signals.AddRange(unexpectedClocks.Select(clock => clock.ObserveNextTimer(false)));
        Task operation = h.Start(kind, value, scope, name);
        await Task.WhenAny(signals).WaitAsync(Guard);
        RecordedTimer timer = Assert.Single(h.Clock.Timers.Skip(timerCount));
        Assert.True(timer.Initialized);
        Assert.Equal(Interval, timer.DueTime);
        Assert.Equal(Timeout.InfiniteTimeSpan, timer.Period);
        Assert.False(operation.IsCompleted);
        Assert.Equal(requestCount, h.Requests.Count);
        // Timer construction precedes collection. Observe the actual reader consume the entry
        // before advancing its clock, so expiry cannot race ahead of the first batch.Add.
        await h.WaitForCollectionAsync(kind, name);
        h.Clock.Advance(TimeSpan.FromTicks(9999));
        Assert.Equal(0, Volatile.Read(ref timer.Callbacks));
        h.Clock.Advance(TimeSpan.FromTicks(1));
        await sdk.WaitAsync(Guard);
        Assert.Equal(1, Volatile.Read(ref timer.Callbacks));
        CapturedRequest request = Assert.Single(h.Requests.Skip(requestCount));
        Assert.Equal(kind, request.Kind);
        Assert.Equal(new[] { "0" }, request.Ids);
        Assert.Equal(new[] { value }, request.Values);
        Assert.Equal(kind == "publish" ? Harness.TopicArn(name ?? "events") : Harness.QueueUrl(name ?? "orders"), request.Address);
        Assert.NotEqual(h.Caller.Token, request.Token);
        Assert.False(request.Token.IsCancellationRequested);
        Assert.DoesNotContain(h.TtlClock.Timers.Skip(ttlTimerCount), x => x.Period == Timeout.InfiniteTimeSpan);
        h.ReleaseSdk();
        await operation.WaitAsync(Guard);
    }

    static async Task RunAsync(string path, Func<Harness, Task> body, bool attachClock = true)
    {
        var h = new Harness(path, attachClock);
        Exception? primary = null;
        try { await body(h); }
        catch (Exception error) { primary = error; }
        List<Exception> errors = await h.CleanupAsync();
        if (primary is not null && errors.Count == 0)
            ExceptionDispatchInfo.Capture(primary).Throw();
        if (primary is not null)
            errors.Insert(0, primary);
        if (errors.Count != 0)
            throw new AggregateException("Batch clock assertion and owned cleanup failures", errors);
    }

    sealed record CapturedRequest(string Kind, string Address, string[] Ids, string[] Values, CancellationToken Token);

    sealed class RecordedTimer(TimeSpan dueTime, TimeSpan period)
    {
        public TimeSpan DueTime { get; } = dueTime;
        public TimeSpan Period { get; } = period;
        public bool Initialized { get; set; }
        public int Callbacks;
    }

    sealed class RecordingClock : TimeProvider
    {
        readonly FakeTimeProvider _inner = new(new DateTimeOffset(2044, 1, 2, 3, 4, 5, TimeSpan.Zero));
        readonly object _sync = new();
        readonly ManualResetEventSlim _throwGate = new(false);
        TaskCompletionSource? _nextTimer;
        bool _ctsOnly;

        public ConcurrentQueue<RecordedTimer> Timers { get; } = new();
        public TaskCompletionSource TimerInvoked { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public ChannelClosedException TimerFailure { get; } = new("selected timer creation failed");
        public bool ThrowTimer { get; set; }
        public override DateTimeOffset GetUtcNow() => _inner.GetUtcNow();
        public override long GetTimestamp() => _inner.GetTimestamp();
        public override long TimestampFrequency => _inner.TimestampFrequency;
        public void Advance(TimeSpan interval) => _inner.Advance(interval);
        public void ReleaseThrow() => _throwGate.Set();

        public Task ObserveNextTimer(bool ctsOnly)
        {
            lock (_sync)
            {
                _nextTimer?.TrySetCanceled();
                _ctsOnly = ctsOnly;
                _nextTimer = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                return _nextTimer.Task;
            }
        }

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            TimerInvoked.TrySetResult();
            if (ThrowTimer)
            {
                if (!_throwGate.Wait(Guard))
                    throw new TimeoutException("Fixture timer throw gate was not released");
                throw TimerFailure;
            }
            var record = new RecordedTimer(dueTime, period);
            ITimer timer = _inner.CreateTimer(value =>
            {
                Interlocked.Increment(ref record.Callbacks);
                callback(value);
            }, state, dueTime, period);
            record.Initialized = true;
            Timers.Enqueue(record);
            lock (_sync)
                if (!_ctsOnly || (dueTime == Interval && period == Timeout.InfiniteTimeSpan))
                    _nextTimer?.TrySetResult();
            return timer;
        }

        public void CloseSignals()
        {
            ReleaseThrow();
            lock (_sync)
                _nextTimer?.TrySetCanceled();
            _throwGate.Dispose();
        }
    }

    sealed class TestQueue(string name) : ViciOne.ServiceBus.AmazonSqs.Topology.Queue
    {
        public string EntityName => name;
        public bool Durable => false;
        public bool AutoDelete => true;
        public IDictionary<string, object> QueueAttributes { get; } = new Dictionary<string, object>();
        public IDictionary<string, object> QueueSubscriptionAttributes { get; } = new Dictionary<string, object>();
        public IDictionary<string, string> QueueTags { get; } = new Dictionary<string, string>();
    }

    sealed class TestTopic(string name) : ViciOne.ServiceBus.AmazonSqs.Topology.Topic
    {
        public string EntityName => name;
        public bool Durable => false;
        public bool AutoDelete => true;
        public IDictionary<string, object> TopicAttributes { get; } = new Dictionary<string, object>();
        public IDictionary<string, object> TopicSubscriptionAttributes { get; } = new Dictionary<string, object>();
        public IDictionary<string, string> TopicTags { get; } = new Dictionary<string, string>();
    }

    sealed class HostileErrorLogger : ILogger
    {
        public int Attempts;
        public Exception? OriginalCause;
        public bool IsEnabled(LogLevel logLevel) => true;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (formatter(state, exception) != "WaitForBatch Faulted")
                return;
            Interlocked.Increment(ref Attempts);
            OriginalCause = exception;
            throw new InvalidOperationException("optional error logger failed");
        }
    }

    sealed class Harness
    {
        readonly string _path;
        readonly List<Task> _tasks = [];
        readonly List<RecordingClock> _additionalClocks = [];
        readonly TaskCompletionSource _metadataRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly TaskCompletionSource _sdkRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly object _sync = new();
        TaskCompletionSource? _nextSdk;
        int _metadataHoldTaken;
        bool _queueExists;
        readonly QueueDoesNotExistException _missingQueue = new("fixture queue must be created");

        public Harness(string path, bool attachClock)
        {
            _path = path;
            _queueExists = !path.EndsWith("created", StringComparison.Ordinal);
            Sqs = InterfaceProxy<IAmazonSQS>.Create((method, args) => method.Name switch
            {
                nameof(IAmazonSQS.GetQueueUrlAsync) => Track(GetQueueUrlAsync(Assert.IsType<string>(args![0]), Token(args))),
                nameof(IAmazonSQS.GetQueueAttributesAsync) => Track(GetQueueAttributesAsync(Assert.IsType<string>(args![0]), Token(args))),
                nameof(IAmazonSQS.CreateQueueAsync) => Track(CreateQueueAsync(Assert.IsType<CreateQueueRequest>(args![0]), Token(args))),
                nameof(IAmazonSQS.SendMessageBatchAsync) => Track(SendAsync(Assert.IsType<SendMessageBatchRequest>(args![0]), Token(args))),
                nameof(IAmazonSQS.DeleteMessageBatchAsync) => Track(DeleteAsync(Assert.IsType<DeleteMessageBatchRequest>(args![0]), Token(args))),
                nameof(IDisposable.Dispose) => null,
                _ => throw new NotSupportedException(method.Name)
            });
            Sns = InterfaceProxy<IAmazonSimpleNotificationService>.Create((method, args) => method.Name switch
            {
                nameof(IAmazonSimpleNotificationService.ListTopicsAsync) => Track(ListTopicsAsync(Token(args))),
                nameof(IAmazonSimpleNotificationService.CreateTopicAsync) => Track(CreateTopicAsync(Assert.IsType<CreateTopicRequest>(args![0]), Token(args))),
                nameof(IAmazonSimpleNotificationService.GetTopicAttributesAsync) => Track(GetTopicAttributesAsync(Token(args))),
                nameof(IAmazonSimpleNotificationService.PublishBatchAsync) => Track(PublishAsync(Assert.IsType<PublishBatchRequest>(args![0]), Token(args))),
                nameof(IDisposable.Dispose) => null,
                _ => throw new NotSupportedException(method.Name)
            });
            var bus = new AmazonSqsBusConfiguration(new AmazonSqsTopologyConfiguration(AmazonSqsBusFactory.CreateMessageTopology()));
            var host = new AmazonSqsHostConfigurator(new Uri("amazonsqs://eu-central-1"));
            ((IAmazonSqsHostConfigurator)host).ClientContextCache(
                new AmazonSqsClientContextCacheOptions(16, TimeSpan.FromDays(1), TtlClock));
            bus.HostConfiguration.Settings = host.Settings;
            Connection = new AmazonSqsConnectionContext(new ViciOne.ServiceBus.AmazonSqs.Connection(() => Sqs, () => Sns),
                bus.HostConfiguration, Lifetime.Token);
            if (attachClock)
                Connection.SetTimeProvider(Clock);
            Client = Connection.CreateClientContext(Lifetime.Token);
        }

        public RecordingClock Clock { get; } = new();
        public RecordingClock TtlClock { get; } = new();
        public CancellationTokenSource Lifetime { get; } = new();
        public CancellationTokenSource Caller { get; } = new();
        public IAmazonSQS Sqs { get; }
        public IAmazonSimpleNotificationService Sns { get; }
        public AmazonSqsConnectionContext Connection { get; }
        public ClientContext Client { get; }
        public List<QueueInfo> Queues { get; } = [];
        public List<TopicInfo> Topics { get; } = [];
        public ConcurrentQueue<CapturedRequest> Requests { get; } = new();
        public ConcurrentDictionary<string, int> MetadataCalls { get; } = new();
        public TaskCompletionSource MetadataEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource SdkEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool HoldMetadata { get; set; }
        public Task? FailedBatchWorker { get; set; }
        public Action? RecoverFailedBatch { get; set; }

        static CancellationToken Token(object?[]? args) => Assert.IsType<CancellationToken>(args![^1]);
        public static string QueueUrl(string name) => "https://sqs.eu-central-1.amazonaws.com/123456789012/" + name;
        public static string TopicArn(string name) => "arn:aws:sns:eu-central-1:123456789012:" + name;
        public static Dictionary<string, string> QueueAttributes(string name) => new()
        {
            [QueueAttributeName.QueueArn] = "arn:aws:sqs:eu-central-1:123456789012:" + name
        };

        public RecordingClock OwnClock(RecordingClock clock) { _additionalClocks.Add(clock); return clock; }
        public QueueInfo Own(QueueInfo queue) { Queues.Add(queue); return queue; }
        public TopicInfo Own(TopicInfo topic) { Topics.Add(topic); return topic; }
        public T Track<T>(T task) where T : Task
        {
            lock (_sync) _tasks.Add(task);
            return task;
        }
        public void ReleaseMetadata() => _metadataRelease.TrySetResult();
        public void ReleaseSdk() => _sdkRelease.TrySetResult();
        public Task ObserveNextSdk()
        {
            lock (_sync)
            {
                _nextSdk?.TrySetCanceled();
                _nextSdk = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                return _nextSdk.Task;
            }
        }

        public Task Start(string kind, string value, ClientContext? scope = null, string? name = null) =>
            Track(kind switch
            {
                "send" => (scope ?? Client).SendMessageAsync(name ?? "orders", new SendMessageBatchRequestEntry("", value), Caller.Token),
                "delete" => (scope ?? Client).DeleteMessageAsync(name ?? "orders", value, Caller.Token),
                "publish" => (scope ?? Client).PublishAsync(name ?? "events", new PublishBatchRequestEntry { Message = value }, Caller.Token),
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            });

        public Task WaitForCollectionAsync(string kind, string? name)
        {
            if (kind == "publish")
            {
                TopicInfo topic = Topics.Single(x => x.EntityName == (name ?? "events"));
                var lazy = (Lazy<IBatcher<PublishBatchRequestEntry>>)Field(topic, "_batchPublisher", typeof(TopicInfo));
                return CollectedAsync<PublishBatchRequestEntry>(lazy.Value);
            }
            QueueInfo queue = Queues.Single(x => x.EntityName == (name ?? "orders"));
            return kind == "send"
                ? CollectedAsync<SendMessageBatchRequestEntry>(
                    ((Lazy<IBatcher<SendMessageBatchRequestEntry>>)Field(queue, "_batchSender", typeof(QueueInfo))).Value)
                : CollectedAsync<DeleteMessageBatchRequestEntry>(
                    ((Lazy<IBatcher<DeleteMessageBatchRequestEntry>>)Field(queue, "_batchDeleter", typeof(QueueInfo))).Value);
        }

        static async Task CollectedAsync<TEntry>(IBatcher<TEntry> batcher)
        {
            var channel = (Channel<BatchEntry<TEntry>>)Field(batcher, "_channel", typeof(Batcher<TEntry>));
            using var guard = new CancellationTokenSource(Guard);
            while (channel.Reader.Count != 0)
            {
                if (guard.IsCancellationRequested)
                    throw new TimeoutException("Fixture collection observation guard expired; zero causal credit");
                await Task.Yield();
            }
        }

        public Task ResolveAsync(string path, string? name = null) => Track(ResolveCoreAsync(path, name));

        async Task ResolveCoreAsync(string path, string? name)
        {
            if (path.StartsWith("publish", StringComparison.Ordinal))
            {
                TopicInfo topic = path.EndsWith("created", StringComparison.Ordinal)
                    ? await Connection.GetTopicAsync(new TestTopic(name ?? "events"), Caller.Token)
                    : await Connection.GetTopicByNameAsync(name ?? "events", Caller.Token);
                Own(topic);
            }
            else
            {
                QueueInfo queue = path.EndsWith("created", StringComparison.Ordinal)
                    ? await Connection.GetQueueAsync(new TestQueue(name ?? "orders"), Caller.Token)
                    : await Connection.GetQueueByNameAsync(name ?? "orders", Caller.Token);
                Own(queue);
            }
        }

        public void AssertResolutionPath(string path)
        {
            if (path.StartsWith("publish", StringComparison.Ordinal))
            {
                Assert.Equal(1, MetadataCalls[nameof(IAmazonSimpleNotificationService.ListTopicsAsync)]);
                Assert.Equal(path.EndsWith("existing", StringComparison.Ordinal), Assert.Single(Topics).Existing);
                if (path.EndsWith("created", StringComparison.Ordinal))
                {
                    Assert.Equal(1, MetadataCalls[nameof(IAmazonSimpleNotificationService.CreateTopicAsync)]);
                    Assert.Equal(1, MetadataCalls[nameof(IAmazonSimpleNotificationService.GetTopicAttributesAsync)]);
                }
                else
                    Assert.False(MetadataCalls.ContainsKey(nameof(IAmazonSimpleNotificationService.CreateTopicAsync)));
            }
            else
            {
                Assert.Equal(1, MetadataCalls[nameof(IAmazonSQS.GetQueueUrlAsync)]);
                Assert.Equal(1, MetadataCalls[nameof(IAmazonSQS.GetQueueAttributesAsync)]);
                Assert.Equal(path.EndsWith("existing", StringComparison.Ordinal), Assert.Single(Queues).Existing);
                if (path.EndsWith("created", StringComparison.Ordinal))
                    Assert.Equal(1, MetadataCalls[nameof(IAmazonSQS.CreateQueueAsync)]);
                else
                    Assert.False(MetadataCalls.ContainsKey(nameof(IAmazonSQS.CreateQueueAsync)));
            }
        }

        async Task MetadataAsync(string method, CancellationToken token)
        {
            MetadataCalls.AddOrUpdate(method, 1, (_, count) => count + 1);
            MetadataEntered.TrySetResult();
            if (HoldMetadata && Interlocked.Exchange(ref _metadataHoldTaken, 1) == 0)
                await _metadataRelease.Task.WaitAsync(token);
        }

        async Task<GetQueueUrlResponse> GetQueueUrlAsync(string name, CancellationToken token)
        {
            await MetadataAsync(nameof(IAmazonSQS.GetQueueUrlAsync), token);
            if (!_queueExists)
                throw _missingQueue;
            return new GetQueueUrlResponse { HttpStatusCode = HttpStatusCode.OK, QueueUrl = QueueUrl(name) };
        }

        async Task<GetQueueAttributesResponse> GetQueueAttributesAsync(string url, CancellationToken token)
        {
            await MetadataAsync(nameof(IAmazonSQS.GetQueueAttributesAsync), token);
            return new GetQueueAttributesResponse
            {
                HttpStatusCode = HttpStatusCode.OK,
                Attributes = QueueAttributes(url[(url.LastIndexOf('/') + 1)..])
            };
        }

        async Task<CreateQueueResponse> CreateQueueAsync(CreateQueueRequest request, CancellationToken token)
        {
            await MetadataAsync(nameof(IAmazonSQS.CreateQueueAsync), token);
            _queueExists = true;
            return new CreateQueueResponse { HttpStatusCode = HttpStatusCode.OK, QueueUrl = QueueUrl(request.QueueName) };
        }

        async Task<ListTopicsResponse> ListTopicsAsync(CancellationToken token)
        {
            await MetadataAsync(nameof(IAmazonSimpleNotificationService.ListTopicsAsync), token);
            return new ListTopicsResponse
            {
                HttpStatusCode = HttpStatusCode.OK,
                Topics = _path.EndsWith("created", StringComparison.Ordinal)
                    ? [] : [new global::Amazon.SimpleNotificationService.Model.Topic { TopicArn = TopicArn("events") }]
            };
        }

        async Task<CreateTopicResponse> CreateTopicAsync(CreateTopicRequest request, CancellationToken token)
        {
            await MetadataAsync(nameof(IAmazonSimpleNotificationService.CreateTopicAsync), token);
            return new CreateTopicResponse { HttpStatusCode = HttpStatusCode.OK, TopicArn = TopicArn(request.Name) };
        }

        async Task<GetTopicAttributesResponse> GetTopicAttributesAsync(CancellationToken token)
        {
            await MetadataAsync(nameof(IAmazonSimpleNotificationService.GetTopicAttributesAsync), token);
            return new GetTopicAttributesResponse { HttpStatusCode = HttpStatusCode.OK, Attributes = [] };
        }

        async Task CaptureAsync(CapturedRequest request)
        {
            Requests.Enqueue(request);
            lock (_sync) _nextSdk?.TrySetResult();
            SdkEntered.TrySetResult();
            await _sdkRelease.Task.WaitAsync(request.Token);
        }

        async Task<SendMessageBatchResponse> SendAsync(SendMessageBatchRequest request, CancellationToken token)
        {
            await CaptureAsync(new CapturedRequest("send", request.QueueUrl, request.Entries.Select(x => x.Id).ToArray(),
                request.Entries.Select(x => x.MessageBody).ToArray(), token));
            return new SendMessageBatchResponse
            {
                HttpStatusCode = HttpStatusCode.OK,
                Successful = request.Entries.Select(x => new SendMessageBatchResultEntry { Id = x.Id }).ToList(), Failed = []
            };
        }

        async Task<DeleteMessageBatchResponse> DeleteAsync(DeleteMessageBatchRequest request, CancellationToken token)
        {
            await CaptureAsync(new CapturedRequest("delete", request.QueueUrl, request.Entries.Select(x => x.Id).ToArray(),
                request.Entries.Select(x => x.ReceiptHandle).ToArray(), token));
            return new DeleteMessageBatchResponse
            {
                HttpStatusCode = HttpStatusCode.OK,
                Successful = request.Entries.Select(x => new DeleteMessageBatchResultEntry { Id = x.Id }).ToList(), Failed = []
            };
        }

        async Task<PublishBatchResponse> PublishAsync(PublishBatchRequest request, CancellationToken token)
        {
            await CaptureAsync(new CapturedRequest("publish", request.TopicArn, request.PublishBatchRequestEntries.Select(x => x.Id).ToArray(),
                request.PublishBatchRequestEntries.Select(x => x.Message).ToArray(), token));
            return new PublishBatchResponse
            {
                HttpStatusCode = HttpStatusCode.OK,
                Successful = request.PublishBatchRequestEntries.Select(x => new PublishBatchResultEntry { Id = x.Id }).ToList(), Failed = []
            };
        }

        public async Task<List<Exception>> CleanupAsync()
        {
            var errors = new List<Exception>();
            async Task JoinAsync(Task task)
            {
                try { await task.WaitAsync(Guard); }
                catch (Exception error)
                {
                    if (!ReferenceEquals(error, Clock.TimerFailure) && !ReferenceEquals(error, _missingQueue))
                        errors.Add(error);
                }
            }
            ReleaseMetadata();
            Clock.ReleaseThrow();
            ReleaseSdk();
            var disposals = new List<Task>();
            foreach (IAsyncDisposable resource in Queues.Cast<IAsyncDisposable>().Concat(Topics))
            {
                try { disposals.Add(resource.DisposeAsync().AsTask()); }
                catch (Exception error) { errors.Add(error); }
            }
            foreach (Task disposal in disposals)
                await JoinAsync(disposal);
            if (FailedBatchWorker is not null)
            {
                await JoinAsync(FailedBatchWorker);
                if (FailedBatchWorker.IsCompleted)
                {
                    // Neutral fixture rescue after every product assertion; never product drain credit.
                    try { RecoverFailedBatch?.Invoke(); }
                    catch (Exception error) { errors.Add(error); }
                }
            }
            Task[] tasks;
            lock (_sync) tasks = _tasks.ToArray();
            foreach (Task task in tasks)
                await JoinAsync(task);
            try { await Connection.DisposeAsync().AsTask().WaitAsync(Guard); }
            catch (Exception error) { errors.Add(error); }
            Clock.CloseSignals();
            TtlClock.CloseSignals();
            foreach (RecordingClock clock in _additionalClocks)
                clock.CloseSignals();
            lock (_sync) _nextSdk?.TrySetCanceled();
            Caller.Dispose();
            Lifetime.Dispose();
            return errors;
        }
    }
}
