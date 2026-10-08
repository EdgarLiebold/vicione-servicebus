using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
using System.Threading.Channels;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using ViciOne.ServiceBus.AmazonSqs.Tests.TestDoubles;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

public sealed class AmazonSqsTopicLifecycleTests
{
    const string TopicName = "events";
    const string OldArn = "arn:aws:sns:eu-central-1:123456789012:events";
    const string FreshArn = OldArn + "-fresh";
    static readonly TimeSpan Watchdog = TimeSpan.FromSeconds(10);

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-LIFECYCLE", "disposed-cold-topic-rejects-publish-without-creating-batcher")]
    public async Task DisposedColdTopic_RejectsPublishWithoutCreatingBatcherAsync()
    {
        using var fixture = new Fixture();
        TopicInfo topic = fixture.CreateTopic(OldArn);
        Exception? observed = null;
        bool createdByPublish = false;
        try
        {
            await topic.DisposeAsync();
            Task? publish = null;
            observed = Record.Exception(() => { publish = fixture.Start(() => topic.PublishAsync(Entry("cold"), CancellationToken.None)); });
            createdByPublish = GetPublisher(topic).IsValueCreated;
            fixture.Release();
            if (observed is null && publish is not null)
                observed = await Record.ExceptionAsync(() => publish.WaitAsync(Watchdog, TestContext.Current.CancellationToken));
        }
        finally
        {
            await fixture.FinishAsync();
        }

        ObjectDisposedException disposed = Assert.IsType<ObjectDisposedException>(observed);
        Assert.Equal(nameof(TopicInfo), disposed.ObjectName);
        Assert.False(createdByPublish);
        Assert.Empty(fixture.Requests);
        Assert.Empty(fixture.ProviderErrors);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-LIFECYCLE", "every-topic-disposal-waits-for-admitted-provider-work")]
    public async Task ConcurrentDisposals_BothWaitForAdmittedProviderWorkAsync()
    {
        using var fixture = new Fixture();
        TopicInfo topic = fixture.CreateTopic(OldArn);
        Task publish = fixture.Start(() => topic.PublishAsync(Entry("drain"), CancellationToken.None));
        try
        {
            await fixture.ProviderEntered.Task.WaitAsync(Watchdog, TestContext.Current.CancellationToken);
            Task first = fixture.Start(() => topic.DisposeAsync().AsTask());
            Task second = fixture.Start(() => topic.DisposeAsync().AsTask());

            Assert.False(second.IsCompleted);
            Assert.False(first.IsCompleted);
            Assert.False(publish.IsCompleted);
            Assert.Equal(0, Volatile.Read(ref fixture.ProviderExited));
            Assert.False(fixture.OwnerToken.IsCancellationRequested);
        }
        finally
        {
            await fixture.FinishAsync();
        }

        Assert.All(fixture.OperationErrors, error => Assert.Null(error));
        Assert.All(fixture.ProviderErrors, error => Assert.Null(error));
        Assert.Equal(1, fixture.ProviderExited);
        AssertRequest(Assert.Single(fixture.Requests), OldArn, "drain", fixture.OwnerToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-LIFECYCLE", "client-reresolves-disposed-topic-before-publish-admission")]
    public async Task Client_ReresolvesDisposedTopicBeforeAdmissionAsync()
    {
        using var fixture = new Fixture();
        TopicInfo stale = fixture.CreateTopic(OldArn);
        TopicInfo fresh = fixture.CreateTopic(FreshArn);
        await stale.DisposeAsync();
        AmazonSqsClientContext client = fixture.CreateClient(attempt => attempt == 1 ? stale : fresh, CancellationToken.None);
        Exception? observed;
        try
        {
            fixture.Release();
            Task publish = fixture.Start(() => client.PublishAsync(TopicName, Entry("fresh"), CancellationToken.None));
            observed = await Record.ExceptionAsync(() => publish.WaitAsync(Watchdog, TestContext.Current.CancellationToken));
        }
        finally
        {
            await fixture.FinishAsync();
        }

        Assert.Null(observed);
        Assert.Equal(2, fixture.Lookups);
        AssertRequest(Assert.Single(fixture.Requests), FreshArn, "fresh", fixture.OwnerToken);
        Assert.False(GetPublisher(stale).IsValueCreated);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-LIFECYCLE", "client-reresolves-only-unadmitted-publish-after-channel-close")]
    public async Task Client_ReresolvesOnlyUnadmittedEntryAfterFullChannelClosesAsync()
    {
        using var fixture = new Fixture(requiredProviderEntries: 10);
        TopicInfo old = fixture.CreateTopic(OldArn);
        TopicInfo fresh = fixture.CreateTopic(FreshArn);
        AmazonSqsClientContext client = fixture.CreateClient(attempt => attempt == 1 ? old : fresh, CancellationToken.None);
        var filler = new List<Task>();
        PublishBatchRequestEntry targetEntry = Entry("target");
        Exception? observed = null;
        try
        {
            for (int i = 0; i < 300; i++)
                filler.Add(fixture.Start(() => old.PublishAsync(Entry($"filler-{i}"), CancellationToken.None)));

            await fixture.ProviderEntered.Task.WaitAsync(Watchdog, TestContext.Current.CancellationToken);
            Task target = fixture.Start(() => client.PublishAsync(TopicName, targetEntry, CancellationToken.None));

            // Positive admission-phase witness: the actual target is in the runtime's blocked
            // WriteAsync ring, read under its own monitor. A merely pending Task proves no phase.
            Assert.Contains(GetBlockedEntries(old), entry => ReferenceEquals(entry, targetEntry));
            _ = fixture.Start(() => old.DisposeAsync().AsTask());
            fixture.Release();
            observed = await Record.ExceptionAsync(() => target.WaitAsync(Watchdog, TestContext.Current.CancellationToken));
        }
        finally
        {
            await fixture.FinishAsync();
        }

        Assert.Null(observed);
        Assert.Equal(2, fixture.Lookups);
        CapturedRequest[] requests = fixture.Requests.ToArray();
        Assert.DoesNotContain(requests.Where(x => x.Arn == OldArn).SelectMany(x => x.Entries), x => x.Message == "target");
        AssertRequest(Assert.Single(requests, x => x.Arn == FreshArn), FreshArn, "target", fixture.OwnerToken);
        Assert.All(fixture.ProviderErrors, error => Assert.Null(error));
        Assert.All(filler, task => Assert.True(task.IsCompletedSuccessfully || task.Exception?.InnerException is BatchAdmissionClosedException));
        Assert.All(requests, request => Assert.Equal(fixture.OwnerToken, request.Token));
        foreach (CapturedRequest request in requests)
            Assert.Equal(Enumerable.Range(0, request.Entries.Length).Select(x => x.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                request.Entries.Select(x => x.Id));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [RequirementCoverage("REQ-VSB-AWS-SQS-LIFECYCLE", "client-never-replays-admitted-publish-after-provider-failure")]
    public async Task Client_DoesNotReplayAdmittedProviderFailureAsync(int failureKind)
    {
        Exception failure = failureKind switch
        {
            0 => new ObjectDisposedException("provider-owner"),
            1 => new ChannelClosedException("provider-channel"),
            _ => new InvalidOperationException("provider-sentinel")
        };
        using var fixture = new Fixture(providerFailure: failure);
        TopicInfo topic = fixture.CreateTopic(OldArn);
        AmazonSqsClientContext client = fixture.CreateClient(_ => topic, CancellationToken.None);
        Exception? observed = null;
        try
        {
            Task publish = fixture.Start(() => client.PublishAsync(TopicName, Entry("admitted"), CancellationToken.None));
            await fixture.ProviderEntered.Task.WaitAsync(Watchdog, TestContext.Current.CancellationToken);
            fixture.Release();
            observed = await Record.ExceptionAsync(() => publish.WaitAsync(Watchdog, TestContext.Current.CancellationToken));
        }
        finally
        {
            await fixture.FinishAsync();
        }

        Assert.Same(failure, observed);
        Assert.Equal(1, fixture.Lookups);
        AssertRequest(Assert.Single(fixture.Requests), OldArn, "admitted", fixture.OwnerToken);
        Assert.Same(failure, Assert.Single(fixture.ProviderErrors));
        Assert.Same(failure, Assert.Single(fixture.OperationErrors));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-LIFECYCLE", "pre-canceled-client-publish-does-not-resolve-topic")]
    public async Task Client_PreCanceledPublishDoesNotResolveOrCallProviderAsync()
    {
        using var fixture = new Fixture();
        using var caller = new CancellationTokenSource();
        TopicInfo topic = fixture.CreateTopic(OldArn);
        AmazonSqsClientContext client = fixture.CreateClient(_ => topic, caller.Token);
        caller.Cancel();
        Exception? observed;
        try
        {
            Task publish = fixture.Start(() => client.PublishAsync(TopicName, Entry("canceled"), caller.Token));
            observed = await Record.ExceptionAsync(() => publish.WaitAsync(Watchdog, TestContext.Current.CancellationToken));
        }
        finally
        {
            await fixture.FinishAsync();
        }

        Assert.Equal(0, fixture.Lookups);
        Assert.Equal(caller.Token, Assert.IsAssignableFrom<OperationCanceledException>(observed).CancellationToken);
        Assert.Empty(fixture.Requests);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-LIFECYCLE", "cancellation-after-stale-topic-resolution-stops-relookup")]
    public async Task Client_CancellationAfterStaleResolutionStopsBeforeSecondLookupAsync()
    {
        using var fixture = new Fixture();
        using var caller = new CancellationTokenSource();
        TopicInfo stale = fixture.CreateTopic(OldArn);
        await stale.DisposeAsync();
        AmazonSqsClientContext client = fixture.CreateClient(_ => { caller.Cancel(); return stale; }, caller.Token);
        Exception? observed;
        try
        {
            Task publish = fixture.Start(() => client.PublishAsync(TopicName, Entry("cancel-stale"), caller.Token));
            observed = await Record.ExceptionAsync(() => publish.WaitAsync(Watchdog, TestContext.Current.CancellationToken));
        }
        finally
        {
            await fixture.FinishAsync();
        }

        Assert.Equal(caller.Token, Assert.IsAssignableFrom<OperationCanceledException>(observed).CancellationToken);
        Assert.Equal(1, fixture.Lookups);
        Assert.Empty(fixture.Requests);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-LIFECYCLE", "client-bounds-disposed-topic-reresolution-to-two-lookups")]
    public async Task Client_TwoStaleTopicsStopWithoutCallingProviderAsync()
    {
        using var fixture = new Fixture();
        TopicInfo stale = fixture.CreateTopic(OldArn);
        await stale.DisposeAsync();
        AmazonSqsClientContext client = fixture.CreateClient(_ => stale, CancellationToken.None);
        Exception? observed;
        try
        {
            fixture.Release();
            Task publish = fixture.Start(() => client.PublishAsync(TopicName, Entry("two-stale"), CancellationToken.None));
            observed = await Record.ExceptionAsync(() => publish.WaitAsync(Watchdog, TestContext.Current.CancellationToken));
        }
        finally
        {
            await fixture.FinishAsync();
        }

        ObjectDisposedException disposed = Assert.IsType<ObjectDisposedException>(observed);
        Assert.Equal(nameof(TopicInfo), disposed.ObjectName);
        Assert.Contains(TopicName, disposed.Message);
        Assert.Equal(2, fixture.Lookups);
        Assert.Empty(fixture.Requests);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-LIFECYCLE", "live-topic-preserves-publish-metadata-usage-and-provider-token")]
    public async Task LiveTopic_PreservesMetadataUsageAndProviderTokenAsync()
    {
        using var fixture = new Fixture();
        TopicInfo topic = fixture.CreateTopic(OldArn);
        int used = 0;
        topic.Used += () => Interlocked.Increment(ref used);
        AmazonSqsClientContext client = fixture.CreateClient(_ => topic, CancellationToken.None);
        try
        {
            Task publish = fixture.Start(() => client.PublishAsync(TopicName, Entry("healthy"), CancellationToken.None));
            await fixture.ProviderEntered.Task.WaitAsync(Watchdog, TestContext.Current.CancellationToken);
            Assert.False(publish.IsCompleted);
            fixture.Release();
            await publish.WaitAsync(Watchdog, TestContext.Current.CancellationToken);
        }
        finally
        {
            await fixture.FinishAsync();
        }

        Assert.Equal(1, used);
        Assert.Equal(1, fixture.Lookups);
        Assert.Equal(TopicName, topic.EntityName);
        Assert.Equal(OldArn, topic.Arn);
        Assert.True(topic.Existing);
        AssertRequest(Assert.Single(fixture.Requests), OldArn, "healthy", fixture.OwnerToken);
        Assert.All(fixture.OperationErrors, error => Assert.Null(error));
    }

    static PublishBatchRequestEntry Entry(string nonce) => new() { Message = nonce, MessageAttributes = [] };

    static void AssertRequest(CapturedRequest request, string arn, string nonce, CancellationToken token)
    {
        Assert.Equal(arn, request.Arn);
        Assert.Equal(token, request.Token);
        CapturedEntry entry = Assert.Single(request.Entries);
        Assert.Equal(nonce, entry.Message);
        Assert.Equal("0", entry.Id);
    }

    static Lazy<IBatcher<PublishBatchRequestEntry>> GetPublisher(TopicInfo topic) =>
        (Lazy<IBatcher<PublishBatchRequestEntry>>)(typeof(TopicInfo).GetField("_batchPublisher", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(topic) ?? throw new InvalidOperationException("The actual topic batcher is unavailable."));

    static IReadOnlyList<PublishBatchRequestEntry> GetBlockedEntries(TopicInfo topic)
    {
        IBatcher<PublishBatchRequestEntry> publisher = GetPublisher(topic).Value;
        var channel = (Channel<BatchEntry<PublishBatchRequestEntry>>)typeof(Batcher<PublishBatchRequestEntry>)
            .GetField("_channel", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(publisher)!;
        Type runtimeType = channel.GetType();
        const BindingFlags privateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        object sync = runtimeType.GetProperty("SyncObj", privateInstance)!.GetValue(channel)!;
        var entries = new List<PublishBatchRequestEntry>();
        lock (sync)
        {
            object? head = runtimeType.GetField("_blockedWritersHead", privateInstance)!.GetValue(channel);
            object? current = head;
            if (head is null)
                return entries;
            do
            {
                Type operationType = current!.GetType();
                var pending = (BatchEntry<PublishBatchRequestEntry>)operationType.GetProperty("Item")!.GetValue(current)!;
                entries.Add(pending.Entry);
                current = operationType.GetProperty("Next")!.GetValue(current);
            } while (current is not null && !ReferenceEquals(current, head));
        }
        return entries;
    }

    sealed record CapturedEntry(string Id, string Message);
    sealed record CapturedRequest(string Arn, CancellationToken Token, CapturedEntry[] Entries);

    sealed class Fixture : IDisposable
    {
        readonly CancellationTokenSource _owner = new();
        readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly List<TopicInfo> _topics = [];
        readonly List<Task> _operations = [];
        readonly ConcurrentBag<Task> _providerTasks = [];
        readonly int _requiredProviderEntries;
        readonly Exception? _providerFailure;
        readonly IAmazonSQS _sqs;
        readonly IAmazonSimpleNotificationService _sns;
        int _entered;

        public Fixture(int requiredProviderEntries = 1, Exception? providerFailure = null)
        {
            _requiredProviderEntries = requiredProviderEntries;
            _providerFailure = providerFailure;
            _sqs = InterfaceProxy<IAmazonSQS>.Create((method, _) => throw new NotSupportedException(method.Name));
            _sns = InterfaceProxy<IAmazonSimpleNotificationService>.Create((method, args) =>
            {
                if (method.Name != nameof(IAmazonSimpleNotificationService.PublishBatchAsync))
                    throw new NotSupportedException(method.Name);
                Task<PublishBatchResponse> operation = SendAsync((PublishBatchRequest)args![0]!, (CancellationToken)args[1]!);
                _providerTasks.Add(operation);
                return operation;
            });
        }

        public TaskCompletionSource ProviderEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public ConcurrentQueue<CapturedRequest> Requests { get; } = new();
        public Exception?[] OperationErrors { get; private set; } = [];
        public Exception?[] ProviderErrors { get; private set; } = [];
        public CancellationToken OwnerToken => _owner.Token;
        public int ProviderExited;
        public int Lookups;

        public TopicInfo CreateTopic(string arn)
        {
            var topic = new TopicInfo(TopicName, arn, _sns, OwnerToken, existing: true);
            _topics.Add(topic);
            return topic;
        }

        public AmazonSqsClientContext CreateClient(Func<int, TopicInfo> resolve, CancellationToken lookupToken)
        {
            ConnectionContext connection = InterfaceProxy<ConnectionContext>.Create((method, args) =>
            {
                if (method.Name != nameof(ConnectionContext.GetTopicByNameAsync))
                    throw new NotSupportedException(method.Name);
                Assert.Equal(TopicName, (string)args![0]!);
                Assert.Equal(lookupToken, (CancellationToken)args[1]!);
                return Task.FromResult(resolve(Interlocked.Increment(ref Lookups)));
            });
            return new AmazonSqsClientContext(connection, _sqs, _sns, CancellationToken.None);
        }

        public Task Start(Func<Task> start)
        {
            Task operation = start();
            _operations.Add(operation);
            return operation;
        }

        public void Release() => _release.TrySetResult();

        async Task<PublishBatchResponse> SendAsync(PublishBatchRequest request, CancellationToken token)
        {
            Requests.Enqueue(new CapturedRequest(request.TopicArn, token,
                request.PublishBatchRequestEntries.Select(x => new CapturedEntry(x.Id, x.Message)).ToArray()));
            if (Interlocked.Increment(ref _entered) >= _requiredProviderEntries)
                ProviderEntered.TrySetResult();
            try
            {
                await _release.Task.ConfigureAwait(false);
                if (_providerFailure is not null)
                    throw _providerFailure;
                return new PublishBatchResponse
                {
                    HttpStatusCode = HttpStatusCode.OK,
                    Successful = request.PublishBatchRequestEntries.Select(x => new PublishBatchResultEntry { Id = x.Id }).ToList(),
                    Failed = []
                };
            }
            finally
            {
                Interlocked.Increment(ref ProviderExited);
            }
        }

        public async Task FinishAsync()
        {
            Release();
            var cleanupErrors = new List<Exception>();
            try
            {
                Task[] disposals = _topics.Select(x => x.DisposeAsync().AsTask()).ToArray();
                await Task.WhenAll(disposals).WaitAsync(Watchdog, CancellationToken.None);
            }
            catch (Exception exception)
            {
                cleanupErrors.Add(exception);
            }
            finally
            {
                try
                {
                    // Original cold-disposal defect can create a batcher after ownership ended.
                    // Join existing lazy values only; fallback has no product ownership credit.
                    Task[] fallback = _topics.Select(GetPublisher).Where(x => x.IsValueCreated)
                        .Select(x => x.Value.DisposeAsync().AsTask()).ToArray();
                    await Task.WhenAll(fallback).WaitAsync(Watchdog, CancellationToken.None);
                }
                catch (Exception exception)
                {
                    cleanupErrors.Add(exception);
                }
                finally
                {
                    try
                    {
                        OperationErrors = await Task.WhenAll(_operations.Select(x => Record.ExceptionAsync(() => x).AsTask()))
                            .WaitAsync(Watchdog, CancellationToken.None);
                    }
                    catch (Exception exception)
                    {
                        cleanupErrors.Add(exception);
                    }
                    finally
                    {
                        try
                        {
                            ProviderErrors = await Task.WhenAll(_providerTasks.Select(x => Record.ExceptionAsync(() => x).AsTask()))
                                .WaitAsync(Watchdog, CancellationToken.None);
                        }
                        catch (Exception exception)
                        {
                            cleanupErrors.Add(exception);
                        }
                    }
                }
            }
            if (cleanupErrors.Count > 0)
                throw new AggregateException("Actual topic/provider cleanup could not be completely joined.", cleanupErrors);
        }

        public void Dispose() => _owner.Dispose();
    }
}
