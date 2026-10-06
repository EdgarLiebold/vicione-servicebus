using System.Net;
using System.Runtime.ExceptionServices;
using global::Amazon.Runtime;
using global::Amazon.SimpleNotificationService;
using global::Amazon.SimpleNotificationService.Model;
using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.Tests;

public sealed class AmazonSqsTopicDiscoveryLifetimeTests
{
    const string TopicName = "discovery-lifetime";
    const string TopicArn = "arn:aws:sns:eu-central-1:123456789012:" + TopicName;
    static readonly TimeSpan Bound = TimeSpan.FromSeconds(10);

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-AWS-SQS-LIFECYCLE", "topic-discovery-does-not-readmit-after-owner-cancellation")]
    public async Task TopicLookup_LifetimeEndsDiscoveryWithoutReadmissionAsync(bool byName, bool cancelLifetime)
    {
        var previousContext = LogContext.Current;
        using var lifetime = new CancellationTokenSource();
        using var caller = new CancellationTokenSource();
        var client = new HeldDiscoveryClient(caller);
        var cache = new TopicCache(client, new AmazonSqsClientContextCacheOptions(), lifetime.Token);
        Task<TopicInfo>? operation = null;
        Exception? primary = null;
        var cleanupFailures = new List<Exception>();

        try
        {
            operation = byName
                ? cache.GetByNameAsync(TopicName, caller.Token)
                : cache.GetAsync(new TopicEntity(1, TopicName, true, false), caller.Token);
            await client.FirstEntered.Task.WaitAsync(Bound, CancellationToken.None);
            Assert.False(operation.IsCompleted);
            NativeCall first = Assert.Single(client.Calls);
            Assert.Equal(lifetime.Token, first.Token);
            Assert.Null(first.Cursor);
            Assert.False(first.Task.IsCompleted);

            if (cancelLifetime)
            {
                lifetime.Cancel();
                client.CancelFirst(lifetime.Token);
                Exception? outcome = await Record.ExceptionAsync(async () =>
                {
                    await operation.WaitAsync(Bound, CancellationToken.None);
                });

                // The first SDK task really ended due to the owner's token.
                Assert.True(first.Task.IsCanceled);
                Assert.True(first.Token.IsCancellationRequested);
                var nativeCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                {
                    await first.Task.WaitAsync(Bound, CancellationToken.None);
                });
                Assert.Equal(lifetime.Token, nativeCancellation.CancellationToken);
                Assert.Single(client.Calls);
                Assert.True(operation.IsCanceled);
                var canceled = Assert.IsAssignableFrom<OperationCanceledException>(outcome);
                Assert.Equal(lifetime.Token, canceled.CancellationToken);
                Assert.False(client.CircuitBreakerInvoked);
            }
            else
            {
                client.CompleteFirst();
                TopicInfo result = await operation.WaitAsync(Bound, CancellationToken.None);
                Assert.True(first.Task.IsCompletedSuccessfully);
                Assert.Single(client.Calls);
                Assert.Equal(TopicName, result.EntityName);
                Assert.Equal(TopicArn, result.Arn);
                Assert.True(result.Existing);
                Assert.False(client.CircuitBreakerInvoked);
                Assert.False(caller.IsCancellationRequested);
                Assert.False(lifetime.IsCancellationRequested);
            }
        }
        catch (Exception exception)
        {
            primary = exception;
        }
        finally
        {
            try
            {
                Attempt(caller.Cancel, cleanupFailures);
                Attempt(lifetime.Cancel, cleanupFailures);
                client.CancelFirst(lifetime.Token);
                if (operation is not null)
                    await JoinAsync(operation, caller.Token, cleanupFailures, lifetime.Token);
                // These are the actual tasks returned by the native SDK seam.
                foreach (NativeCall call in client.Calls)
                    await JoinAsync(call.Task, call.Token, cleanupFailures);
                await ObserveAsync(() => cache.DisposeAsync().AsTask(), cleanupFailures);
                Attempt(client.Dispose, cleanupFailures);
            }
            finally
            {
                LogContext.Current = previousContext;
            }
        }

        if (cleanupFailures.Count > 0)
        {
            if (primary is not null)
                cleanupFailures.Insert(0, primary);
            if (cleanupFailures.Count == 1)
                ExceptionDispatchInfo.Capture(cleanupFailures[0]).Throw();
            throw new AggregateException(cleanupFailures);
        }
        if (primary is not null)
            ExceptionDispatchInfo.Capture(primary).Throw();
    }

    static void Attempt(Action action, List<Exception> failures)
    {
        try { action(); }
        catch (Exception exception) { failures.Add(exception); }
    }

    static async Task ObserveAsync(Func<Task> operation, List<Exception> failures)
    {
        try { await operation().WaitAsync(Bound, CancellationToken.None); }
        catch (Exception exception) { failures.Add(exception); }
    }

    static async Task JoinAsync(Task task, CancellationToken expected, List<Exception> failures,
        CancellationToken alternate = default)
    {
        try { await task.WaitAsync(Bound, CancellationToken.None); }
        catch (OperationCanceledException exception) when (task.IsCanceled
            && (exception.CancellationToken == expected || exception.CancellationToken == alternate)
            && exception.CancellationToken.IsCancellationRequested)
        {
            // Only terminal cancellation of the retained actual task is expected.
        }
        catch (Exception exception) { failures.Add(exception); }
    }

    sealed record NativeCall(string? Cursor, CancellationToken Token, Task<ListTopicsResponse> Task);

    sealed class HeldDiscoveryClient(CancellationTokenSource caller)
        : AmazonSimpleNotificationServiceClient(new AnonymousAWSCredentials(),
            new AmazonSimpleNotificationServiceConfig
            {
                ServiceURL = "http://127.0.0.1:1",
                AuthenticationRegion = "eu-central-1"
            })
    {
        readonly object _sync = new();
        readonly List<NativeCall> _calls = [];
        readonly TaskCompletionSource<ListTopicsResponse> _first = new(TaskCreationOptions.RunContinuationsAsynchronously);
        bool _circuitBreakerInvoked;

        public TaskCompletionSource FirstEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public NativeCall[] Calls { get { lock (_sync) return _calls.ToArray(); } }
        public bool CircuitBreakerInvoked { get { lock (_sync) return _circuitBreakerInvoked; } }

        public override Task<ListTopicsResponse> ListTopicsAsync(ListTopicsRequest request,
            CancellationToken cancellationToken = default)
        {
            Task<ListTopicsResponse> task;
            bool first;
            lock (_sync)
            {
                first = _calls.Count == 0;
                task = first ? _first.Task : Task.FromCanceled<ListTopicsResponse>(cancellationToken);
                _calls.Add(new NativeCall(request.NextToken, cancellationToken, task));
                if (!first)
                    _circuitBreakerInvoked = true;
            }

            if (first)
                FirstEntered.TrySetResult();
            else
                caller.Cancel(); // Recorded native admission precedes this finite caller circuit breaker.
            return task;
        }

        public override Task<CreateTopicResponse> CreateTopicAsync(CreateTopicRequest request,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Unexpected native topic creation.");

        public override Task<GetTopicAttributesResponse> GetTopicAttributesAsync(string topicArn,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Unexpected native topic attribute request.");

        public void CancelFirst(CancellationToken token) => _first.TrySetCanceled(token);

        public void CompleteFirst() => _first.TrySetResult(new ListTopicsResponse
        {
            HttpStatusCode = HttpStatusCode.OK,
            Topics = [new global::Amazon.SimpleNotificationService.Model.Topic { TopicArn = TopicArn }],
            NextToken = null
        });
    }
}
