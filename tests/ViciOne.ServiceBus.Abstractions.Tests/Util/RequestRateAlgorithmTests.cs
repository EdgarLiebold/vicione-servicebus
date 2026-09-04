using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Util;

public sealed class RequestRateAlgorithmTests
{
    private static readonly TimeSpan CompletionTimeout = TimeSpan.FromSeconds(10);

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-RATE-RUN", "process-every-result")]
    public async Task Run_RequestsConfiguredLimitAndProcessesEveryResult()
    {
        using var algorithm = CreateAlgorithm(prefetchCount: 100, requestResultLimit: 10);
        using var timeout = new CancellationTokenSource(CompletionTimeout);
        var requestedLimits = new ConcurrentQueue<int>();
        var processedResults = new ConcurrentDictionary<int, byte>();
        var processingCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var callbackCount = 0;

        Task<IEnumerable<int>> Request(int resultLimit, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            requestedLimits.Enqueue(resultLimit);
            return Task.FromResult<IEnumerable<int>>(Enumerable.Range(0, resultLimit).ToArray());
        }

        Task Process(int result, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            processedResults.TryAdd(result, 0);
            if (Interlocked.Increment(ref callbackCount) == 10)
                processingCompleted.TrySetResult();

            return Task.CompletedTask;
        }

        var resultCount = await algorithm.Run(Request, Process, timeout.Token);
        await processingCompleted.Task.WaitAsync(timeout.Token);

        Assert.Equal(10, resultCount);
        Assert.Equal([10], requestedLimits);
        Assert.Equal(10, callbackCount);
        Assert.Equal(Enumerable.Range(0, 10), processedResults.Keys.Order());
        Assert.Equal(0, algorithm.ActiveRequestCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-RATE-GROUPED-RUN", "deterministic-request-overlap")]
    public async Task GroupedRun_RepeatedFullBatchesReachConfiguredRequestConcurrency()
    {
        using var algorithm = CreateAlgorithm(prefetchCount: 100, requestResultLimit: 10, concurrentResultLimit: 1_000);
        using var timeout = new CancellationTokenSource(CompletionTimeout);
        var groupCallbackCount = 0;
        var orderCallbackCount = 0;

        for (var pass = 0; pass < 5; pass++)
        {
            var expectedRequestCount = algorithm.RequestCount;
            var expectedResultCount = expectedRequestCount * algorithm.ResultLimit;
            var requestBarrier = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var processingCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var arrivedRequestCount = 0;
            var requestSequence = 0;
            var processedResultCount = 0;

            async Task<IEnumerable<GroupedMessage>> Request(int resultLimit, CancellationToken cancellationToken)
            {
                var sequence = Interlocked.Increment(ref requestSequence);
                if (Interlocked.Increment(ref arrivedRequestCount) == expectedRequestCount)
                    requestBarrier.TrySetResult();

                await requestBarrier.Task.WaitAsync(cancellationToken);

                return Enumerable.Range(0, resultLimit)
                    .Select(index => new GroupedMessage(index.ToString(CultureInfo.InvariantCulture), sequence))
                    .ToArray();
            }

            Task Process(GroupedMessage result, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (Interlocked.Increment(ref processedResultCount) == expectedResultCount)
                    processingCompleted.TrySetResult();

                return Task.CompletedTask;
            }

            IEnumerable<IGrouping<string, GroupedMessage>> Group(IEnumerable<GroupedMessage> results)
            {
                Interlocked.Increment(ref groupCallbackCount);
                return results.GroupBy(message => message.GroupId, StringComparer.Ordinal);
            }

            IEnumerable<GroupedMessage> Order(IEnumerable<GroupedMessage> results)
            {
                Interlocked.Increment(ref orderCallbackCount);
                return results.OrderBy(message => message.SequenceNumber);
            }

            var resultCount = await algorithm.Run(Request, Process, Group, Order, timeout.Token);
            await processingCompleted.Task.WaitAsync(timeout.Token);

            Assert.Equal(expectedRequestCount, arrivedRequestCount);
            Assert.Equal(expectedResultCount, resultCount);
            Assert.Equal(expectedResultCount, processedResultCount);
            Assert.Equal(0, algorithm.ActiveRequestCount);
        }

        Assert.Equal(5, groupCallbackCount);
        Assert.Equal(50, orderCallbackCount);
        Assert.Equal(10, algorithm.RequestCount);
        Assert.Equal(10, algorithm.MaxActiveRequestCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-RATE-SCALING", "full-batch-growth-curve")]
    public async Task FullBatches_IncreaseRequestCountAlongStableCurve()
    {
        using var algorithm = CreateAlgorithm(prefetchCount: 100, requestResultLimit: 10);
        var observedRequestCounts = new List<int> { algorithm.RequestCount };

        for (var index = 0; index < 5; index++)
        {
            using var request = await algorithm.BeginRequest(TestContext.Current.CancellationToken);
            Assert.Equal(10, request.ResultLimit);

            await request.Complete(request.ResultLimit, TestContext.Current.CancellationToken);
            observedRequestCounts.Add(algorithm.RequestCount);
        }

        Assert.Equal([1, 6, 8, 9, 10, 10], observedRequestCounts);
        Assert.Equal(0, algorithm.ActiveRequestCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-RATE-SCALING", "concurrent-full-batches-do-not-deadlock")]
    public async Task RepeatedConcurrentFullBatches_CompleteWhileRequestCountGrows()
    {
        using var algorithm = CreateAlgorithm(prefetchCount: 100, requestResultLimit: 10);
        using var timeout = new CancellationTokenSource(CompletionTimeout);
        var observedRequestCounts = new List<int> { algorithm.RequestCount };

        for (var pass = 0; pass < 3; pass++)
        {
            int expectedRequestCount = algorithm.RequestCount;
            var allRequestsStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var arrivedRequestCount = 0;

            async Task<int> Request(int resultLimit, CancellationToken cancellationToken)
            {
                if (Interlocked.Increment(ref arrivedRequestCount) == expectedRequestCount)
                    allRequestsStarted.TrySetResult();

                await allRequestsStarted.Task.WaitAsync(cancellationToken);
                return resultLimit;
            }

            int resultCount = await algorithm.Run(Request, timeout.Token).WaitAsync(timeout.Token);

            Assert.Equal(expectedRequestCount * algorithm.ResultLimit, resultCount);
            Assert.Equal(expectedRequestCount, arrivedRequestCount);
            Assert.Equal(0, algorithm.ActiveRequestCount);
            observedRequestCounts.Add(algorithm.RequestCount);
        }

        Assert.Equal([1, 6, 10, 10], observedRequestCounts);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-RATE-LIMITS", "prefetch-clamps-result-limit")]
    public void ResultLimit_IsClampedToPrefetchCount()
    {
        using var algorithm = CreateAlgorithm(prefetchCount: 1, requestResultLimit: 10);

        Assert.Equal(1, algorithm.RequestCount);
        Assert.Equal(1, algorithm.ResultLimit);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(0)]
    [RequirementCoverage("REQ-VSB-REQUEST-RATE-LIMITS", "single-request-full-and-empty-results")]
    public async Task EqualPrefetchAndResultLimits_RemainSingleRequestAfterCompletion(int completedResultCount)
    {
        using var algorithm = CreateAlgorithm(prefetchCount: 100, requestResultLimit: 100);

        Assert.Equal(1, algorithm.RequestCount);
        Assert.Equal(100, algorithm.ResultLimit);

        using var request = await algorithm.BeginRequest(TestContext.Current.CancellationToken);
        await request.Complete(completedResultCount, TestContext.Current.CancellationToken);

        Assert.Equal(1, algorithm.RequestCount);
        Assert.Equal(100, algorithm.ResultLimit);
        Assert.Equal(0, algorithm.ActiveRequestCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-RATE-CLOCK", "rate-window-exact-boundary")]
    public async Task RateLimitWindow_ReopensOnlyWhenTheConfiguredClockReachesTheExactInterval()
    {
        TimeSpan interval = TimeSpan.FromMinutes(1);
        var clock = new FakeTimeProvider(new DateTimeOffset(2035, 6, 7, 8, 9, 10, TimeSpan.Zero));
        using var algorithm = new RequestRateAlgorithm(new RequestRateAlgorithmOptions
        {
            PrefetchCount = 1,
            RequestResultLimit = 1,
            RequestRateLimit = 1,
            RequestRateInterval = interval,
        }, clock);

        using (ActiveRequest first = await algorithm.BeginRequest(TestContext.Current.CancellationToken))
            await first.Complete(0, TestContext.Current.CancellationToken);

        Task<ActiveRequest> nextRequest = algorithm.BeginRequest(TestContext.Current.CancellationToken);
        await Task.Yield();
        Assert.False(nextRequest.IsCompleted);

        clock.Advance(interval - TimeSpan.FromTicks(1));
        await Task.Yield();
        Assert.False(nextRequest.IsCompleted);

        clock.Advance(TimeSpan.FromTicks(1));
        using ActiveRequest second = await nextRequest.WaitAsync(
            TimeSpan.FromSeconds(2),
            TestContext.Current.CancellationToken);

        Assert.Equal(1, second.ResultLimit);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-RATE-CLOCK", "active-request-cancellation-grace")]
    public async Task ParentCancellation_CancelsAnActiveRequestAtTheConfiguredClockBoundary()
    {
        TimeSpan grace = TimeSpan.FromSeconds(30);
        var clock = new FakeTimeProvider(new DateTimeOffset(2036, 7, 8, 9, 10, 11, TimeSpan.Zero));
        using var algorithm = new RequestRateAlgorithm(new RequestRateAlgorithmOptions
        {
            PrefetchCount = 1,
            RequestResultLimit = 1,
            RequestCancellationTimeout = grace,
        }, clock);
        using var parent = new CancellationTokenSource();
        using ActiveRequest request = await algorithm.BeginRequest(parent.Token);

        parent.Cancel();
        Assert.False(request.CancellationToken.IsCancellationRequested);

        clock.Advance(grace - TimeSpan.FromTicks(1));
        Assert.False(request.CancellationToken.IsCancellationRequested);

        clock.Advance(TimeSpan.FromTicks(1));
        Assert.True(request.CancellationToken.IsCancellationRequested);
    }

    [Theory]
    [InlineData(RequiredOption.PrefetchCount, "PrefetchCount")]
    [InlineData(RequiredOption.RequestResultLimit, "RequestResultLimit")]
    [RequirementCoverage("REQ-VSB-REQUEST-RATE-VALIDATION", "zero-required-option-rejected")]
    public void Constructor_RejectsZeroRequiredOptions(RequiredOption option, string optionName)
    {
        var options = new RequestRateAlgorithmOptions
        {
            PrefetchCount = option == RequiredOption.PrefetchCount ? 0 : 1,
            RequestResultLimit = option == RequiredOption.RequestResultLimit ? 0 : 1,
        };

        var exception = Assert.Throws<ArgumentException>(() => new RequestRateAlgorithm(options));

        Assert.Equal("options", exception.ParamName);
        Assert.Contains($"{optionName} must be > 0", exception.Message, StringComparison.Ordinal);
    }

    public enum RequiredOption
    {
        PrefetchCount,
        RequestResultLimit,
    }

    private static RequestRateAlgorithm CreateAlgorithm(int prefetchCount, int requestResultLimit, int? concurrentResultLimit = null)
    {
        return new RequestRateAlgorithm(new RequestRateAlgorithmOptions
        {
            PrefetchCount = prefetchCount,
            RequestResultLimit = requestResultLimit,
            ConcurrentResultLimit = concurrentResultLimit,
        });
    }

    private sealed record GroupedMessage(string GroupId, int SequenceNumber);
}
