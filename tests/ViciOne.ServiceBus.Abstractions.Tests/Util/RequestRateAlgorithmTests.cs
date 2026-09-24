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
    public async Task Run_RequestsConfiguredLimitAndProcessesEveryResultAsync()
    {
        using var algorithm = CreateAlgorithm(prefetchCount: 100, requestResultLimit: 10);
        using var timeout = new CancellationTokenSource(CompletionTimeout);
        var requestedLimits = new ConcurrentQueue<int>();
        var processedResults = new ConcurrentDictionary<int, byte>();
        var processingCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var callbackCount = 0;

        Task<IEnumerable<int>> RequestAsync(int resultLimit, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            requestedLimits.Enqueue(resultLimit);
            return Task.FromResult<IEnumerable<int>>(Enumerable.Range(0, resultLimit).ToArray());
        }

        Task ProcessAsync(int result, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            processedResults.TryAdd(result, 0);
            if (Interlocked.Increment(ref callbackCount) == 10)
                processingCompleted.TrySetResult();

            return Task.CompletedTask;
        }

        var resultCount = await algorithm.RunAsync(RequestAsync, ProcessAsync, timeout.Token);
        await processingCompleted.Task.WaitAsync(timeout.Token);

        Assert.Equal(10, resultCount);
        Assert.Equal([10], requestedLimits);
        Assert.Equal(10, callbackCount);
        Assert.Equal(Enumerable.Range(0, 10), processedResults.Keys.Order());
        Assert.Equal(0, algorithm.ActiveRequestCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-RATE-GROUPED-RUN", "deterministic-request-overlap")]
    public async Task GroupedRun_RepeatedFullBatchesReachConfiguredRequestConcurrencyAsync()
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

            async Task<IEnumerable<GroupedMessage>> RequestAsync(int resultLimit, CancellationToken cancellationToken)
            {
                var sequence = Interlocked.Increment(ref requestSequence);
                if (Interlocked.Increment(ref arrivedRequestCount) == expectedRequestCount)
                    requestBarrier.TrySetResult();

                await requestBarrier.Task.WaitAsync(cancellationToken);

                return Enumerable.Range(0, resultLimit)
                    .Select(index => new GroupedMessage(index.ToString(CultureInfo.InvariantCulture), sequence))
                    .ToArray();
            }

            Task ProcessAsync(GroupedMessage result, CancellationToken cancellationToken)
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

            var resultCount = await algorithm.RunAsync(RequestAsync, ProcessAsync, Group, Order, timeout.Token);
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
    public async Task FullBatches_IncreaseRequestCountAlongStableCurveAsync()
    {
        using var algorithm = CreateAlgorithm(prefetchCount: 100, requestResultLimit: 10);
        var observedRequestCounts = new List<int> { algorithm.RequestCount };

        for (var index = 0; index < 5; index++)
        {
            using var request = await algorithm.BeginRequestAsync(TestContext.Current.CancellationToken);
            Assert.Equal(10, request.ResultLimit);

            await request.CompleteAsync(request.ResultLimit, TestContext.Current.CancellationToken);
            observedRequestCounts.Add(algorithm.RequestCount);
        }

        Assert.Equal([1, 6, 8, 9, 10, 10], observedRequestCounts);
        Assert.Equal(0, algorithm.ActiveRequestCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-RATE-SCALING", "concurrent-full-batches-do-not-deadlock")]
    public async Task RepeatedConcurrentFullBatches_CompleteWhileRequestCountGrowsAsync()
    {
        using var algorithm = CreateAlgorithm(prefetchCount: 100, requestResultLimit: 10);
        using var timeout = new CancellationTokenSource(CompletionTimeout);
        var observedRequestCounts = new List<int> { algorithm.RequestCount };

        for (var pass = 0; pass < 3; pass++)
        {
            int expectedRequestCount = algorithm.RequestCount;
            var allRequestsStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var arrivedRequestCount = 0;

            async Task<int> RequestAsync(int resultLimit, CancellationToken cancellationToken)
            {
                if (Interlocked.Increment(ref arrivedRequestCount) == expectedRequestCount)
                    allRequestsStarted.TrySetResult();

                await allRequestsStarted.Task.WaitAsync(cancellationToken);
                return resultLimit;
            }

            int resultCount = await algorithm.RunAsync(RequestAsync, timeout.Token).WaitAsync(timeout.Token);

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
    public async Task EqualPrefetchAndResultLimits_RemainSingleRequestAfterCompletionAsync(int completedResultCount)
    {
        using var algorithm = CreateAlgorithm(prefetchCount: 100, requestResultLimit: 100);

        Assert.Equal(1, algorithm.RequestCount);
        Assert.Equal(100, algorithm.ResultLimit);

        using var request = await algorithm.BeginRequestAsync(TestContext.Current.CancellationToken);
        await request.CompleteAsync(completedResultCount, TestContext.Current.CancellationToken);

        Assert.Equal(1, algorithm.RequestCount);
        Assert.Equal(100, algorithm.ResultLimit);
        Assert.Equal(0, algorithm.ActiveRequestCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-RATE-LIMITS", "maximum-prefetch-preserves-request-and-result-capacity")]
    public async Task MaximumPrefetch_DoesNotOverflowRequestOrResultCapacityAsync()
    {
        using var algorithm = CreateAlgorithm(int.MaxValue, int.MaxValue - 1);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(CompletionTimeout);
        CancellationToken cancellationToken = timeout.Token;

        using (ActiveRequest completed = await algorithm.BeginRequestAsync(cancellationToken))
        {
            Assert.Equal(int.MaxValue - 1, completed.ResultLimit);
            await completed.CompleteAsync(completed.ResultLimit, cancellationToken);
        }

        Assert.Equal(2, algorithm.RequestCount);
        using (ActiveRequest first = await algorithm.BeginRequestAsync(cancellationToken))
        {
            Assert.Equal(int.MaxValue - 1, first.ResultLimit);
            using (ActiveRequest second = await algorithm.BeginRequestAsync(cancellationToken))
            {
                Assert.Equal(1, second.ResultLimit);
                Assert.Equal(2, algorithm.ActiveRequestCount);
            }
        }

        Assert.Equal(0, algorithm.ActiveRequestCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-RATE-CLOCK", "rate-window-exact-boundary")]
    public async Task RateLimitWindow_ReopensOnlyWhenTheConfiguredClockReachesTheExactIntervalAsync()
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

        using (ActiveRequest first = await algorithm.BeginRequestAsync(TestContext.Current.CancellationToken))
            await first.CompleteAsync(0, TestContext.Current.CancellationToken);

        Task<ActiveRequest> nextRequest = algorithm.BeginRequestAsync(TestContext.Current.CancellationToken);
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
    [RequirementCoverage("REQ-VSB-REQUEST-RATE-DYNAMIC-LIMIT", "increase-releases-only-one-current-window-slot")]
    public async Task IncreasingRateLimit_AdmitsOneMoreRequestBeforeTheWindowResetsAsync()
    {
        TimeSpan interval = TimeSpan.FromMinutes(1);
        var clock = new FakeTimeProvider();
        using var algorithm = CreateRateLimitedAlgorithm(1, interval, clock);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        using (ActiveRequest first = await algorithm.BeginRequestAsync(cancellationToken))
            await first.CompleteAsync(0, cancellationToken);

        Task<ActiveRequest> secondRequest = algorithm.BeginRequestAsync(cancellationToken);
        Assert.False(secondRequest.IsCompleted);

        await algorithm.ChangeRateLimitAsync(2, cancellationToken);
        using (ActiveRequest second = await secondRequest.WaitAsync(CompletionTimeout, cancellationToken))
            await second.CompleteAsync(0, cancellationToken);

        Task<ActiveRequest> thirdRequest = algorithm.BeginRequestAsync(cancellationToken);
        Assert.False(thirdRequest.IsCompleted);
        clock.Advance(interval);
        using ActiveRequest third = await thirdRequest.WaitAsync(CompletionTimeout, cancellationToken);
        Assert.Equal(1, third.ResultLimit);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-RATE-DYNAMIC-LIMIT", "decrease-waits-and-enforces-next-window")]
    public async Task DecreasingRateLimit_WaitsForTheWindowThenAdmitsOnlyOneRequestAsync()
    {
        TimeSpan interval = TimeSpan.FromMinutes(1);
        var clock = new FakeTimeProvider();
        using var algorithm = CreateRateLimitedAlgorithm(2, interval, clock);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        for (var index = 0; index < 2; index++)
        {
            using ActiveRequest request = await algorithm.BeginRequestAsync(cancellationToken);
            await request.CompleteAsync(0, cancellationToken);
        }

        Task decrease = algorithm.ChangeRateLimitAsync(1, cancellationToken);
        Assert.False(decrease.IsCompleted);
        Task<ActiveRequest> waitingRequest = algorithm.BeginRequestAsync(cancellationToken);
        Assert.False(waitingRequest.IsCompleted);
        clock.Advance(interval - TimeSpan.FromTicks(1));
        Assert.False(decrease.IsCompleted);
        Assert.False(waitingRequest.IsCompleted);
        clock.Advance(TimeSpan.FromTicks(1));
        await decrease.WaitAsync(CompletionTimeout, cancellationToken);

        using (ActiveRequest first = await waitingRequest.WaitAsync(CompletionTimeout, cancellationToken))
            await first.CompleteAsync(0, cancellationToken);
        Task<ActiveRequest> blocked = algorithm.BeginRequestAsync(cancellationToken);
        Assert.False(blocked.IsCompleted);

        clock.Advance(interval);
        using ActiveRequest second = await blocked.WaitAsync(CompletionTimeout, cancellationToken);
        Assert.Equal(1, second.ResultLimit);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-RATE-DYNAMIC-LIMIT", "cancellation-keeps-prior-window-capacity")]
    public async Task CanceledDecrease_LeavesThePreviousRateAvailableInTheNextWindowAsync()
    {
        TimeSpan interval = TimeSpan.FromMinutes(1);
        var clock = new FakeTimeProvider();
        using var algorithm = CreateRateLimitedAlgorithm(3, interval, clock);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        for (var index = 0; index < 2; index++)
        {
            using ActiveRequest request = await algorithm.BeginRequestAsync(cancellationToken);
            await request.CompleteAsync(0, cancellationToken);
        }

        using var changeCancellation = new CancellationTokenSource();
        Task decrease = algorithm.ChangeRateLimitAsync(1, changeCancellation.Token);
        Assert.False(decrease.IsCompleted);
        changeCancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => decrease);

        clock.Advance(interval);
        for (var index = 0; index < 3; index++)
        {
            using ActiveRequest request = await algorithm.BeginRequestAsync(cancellationToken)
                .WaitAsync(CompletionTimeout, cancellationToken);
            await request.CompleteAsync(0, cancellationToken);
        }

        Task<ActiveRequest> fourth = algorithm.BeginRequestAsync(cancellationToken);
        Assert.False(fourth.IsCompleted);
        clock.Advance(interval);
        using ActiveRequest nextWindow = await fourth.WaitAsync(CompletionTimeout, cancellationToken);
        Assert.Equal(1, nextWindow.ResultLimit);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-RATE-DYNAMIC-LIMIT", "parallel-decreases-share-one-limit-change")]
    public async Task ConcurrentDecreases_ToTheSameLimit_LeaveOneRequestAvailableAsync()
    {
        TimeSpan interval = TimeSpan.FromMinutes(1);
        var clock = new FakeTimeProvider();
        using var algorithm = CreateRateLimitedAlgorithm(2, interval, clock);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        for (var index = 0; index < 2; index++)
        {
            using ActiveRequest request = await algorithm.BeginRequestAsync(cancellationToken);
            await request.CompleteAsync(0, cancellationToken);
        }

        Task firstDecrease = algorithm.ChangeRateLimitAsync(1, cancellationToken);
        Task secondDecrease = algorithm.ChangeRateLimitAsync(1, cancellationToken);
        Assert.False(firstDecrease.IsCompleted);
        Assert.False(secondDecrease.IsCompleted);

        clock.Advance(interval);
        await Task.WhenAll(firstDecrease, secondDecrease).WaitAsync(CompletionTimeout, cancellationToken);

        using (ActiveRequest allowed = await algorithm.BeginRequestAsync(cancellationToken)
                   .WaitAsync(CompletionTimeout, cancellationToken))
            await allowed.CompleteAsync(0, cancellationToken);

        Task<ActiveRequest> blocked = algorithm.BeginRequestAsync(cancellationToken);
        Assert.False(blocked.IsCompleted);
        clock.Advance(interval);
        using ActiveRequest nextWindow = await blocked.WaitAsync(CompletionTimeout, cancellationToken);
        Assert.Equal(1, nextWindow.ResultLimit);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-RATE-DYNAMIC-LIMIT", "adaptive-request-count-does-not-change-rate-capacity")]
    public async Task AdaptiveRequestGrowth_DoesNotConsumeTheDynamicRateIncreaseAsync()
    {
        var clock = new FakeTimeProvider();
        using var algorithm = new RequestRateAlgorithm(new RequestRateAlgorithmOptions
        {
            PrefetchCount = 2,
            RequestResultLimit = 1,
            RequestRateLimit = 1,
            RequestRateInterval = TimeSpan.FromMinutes(1),
        }, clock);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        using (ActiveRequest first = await algorithm.BeginRequestAsync(cancellationToken))
            await first.CompleteAsync(1, cancellationToken);
        Assert.Equal(2, algorithm.RequestCount);

        await algorithm.ChangeRateLimitAsync(2, cancellationToken);
        using (ActiveRequest second = await algorithm.BeginRequestAsync(cancellationToken)
                   .WaitAsync(CompletionTimeout, cancellationToken))
            await second.CompleteAsync(0, cancellationToken);

        Task<ActiveRequest> third = algorithm.BeginRequestAsync(cancellationToken);
        Assert.False(third.IsCompleted);
        clock.Advance(TimeSpan.FromMinutes(1));
        using ActiveRequest nextWindow = await third.WaitAsync(CompletionTimeout, cancellationToken);
        Assert.Equal(1, nextWindow.ResultLimit);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-RATE-DYNAMIC-LIMIT", "dispose-cancels-pending-decrease")]
    public async Task DisposingAlgorithm_CancelsPendingRateDecreaseAsync()
    {
        var clock = new FakeTimeProvider();
        using var algorithm = CreateRateLimitedAlgorithm(2, TimeSpan.FromMinutes(1), clock);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        for (var index = 0; index < 2; index++)
        {
            using ActiveRequest request = await algorithm.BeginRequestAsync(cancellationToken);
            await request.CompleteAsync(0, cancellationToken);
        }

        Task decrease = algorithm.ChangeRateLimitAsync(1, cancellationToken);
        Task queuedDecrease = algorithm.ChangeRateLimitAsync(1, cancellationToken);
        Assert.False(decrease.IsCompleted);
        Assert.False(queuedDecrease.IsCompleted);
        algorithm.Dispose();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => decrease.WaitAsync(CompletionTimeout, cancellationToken));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => queuedDecrease.WaitAsync(CompletionTimeout, cancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-RATE-CANCELLATION", "pre-canceled-request-does-not-create-capacity")]
    public async Task PreCanceledRequest_DoesNotCreateAnExtraConcurrentRequestSlotAsync()
    {
        using var algorithm = CreateAlgorithm(prefetchCount: 2, requestResultLimit: 1);
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => algorithm.BeginRequestAsync(canceled.Token));

        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using ActiveRequest first = await algorithm.BeginRequestAsync(cancellationToken);
        Task<ActiveRequest> second = algorithm.BeginRequestAsync(cancellationToken);
        Assert.False(second.IsCompleted);
        Assert.Equal(1, algorithm.ActiveRequestCount);

        await first.CompleteAsync(0, cancellationToken);
        using ActiveRequest admitted = await second.WaitAsync(CompletionTimeout, cancellationToken);
        Assert.Equal(1, algorithm.MaxActiveRequestCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-REQUEST-RATE-CANCELLATION", "waiting-for-result-capacity-releases-request-lease")]
    public async Task WaitingForResultCapacity_CallerCancellationOrDisposalReleasesTheLeaseAsync(bool disposeAlgorithm)
    {
        using var algorithm = CreateAlgorithm(prefetchCount: 2, requestResultLimit: 1, concurrentResultLimit: 1);
        CancellationToken testToken = TestContext.Current.CancellationToken;
        using (ActiveRequest completed = await algorithm.BeginRequestAsync(testToken))
            await completed.CompleteAsync(1, testToken);
        Assert.Equal(2, algorithm.RequestCount);

        using var first = await algorithm.BeginRequestAsync(testToken);
        using var pendingCancellation = CancellationTokenSource.CreateLinkedTokenSource(testToken);
        pendingCancellation.CancelAfter(CompletionTimeout);
        Task<ActiveRequest> waiting = Task.Run(() => algorithm.BeginRequestAsync(pendingCancellation.Token));
        try
        {
            Assert.True(SpinWait.SpinUntil(() => algorithm.ActiveRequestCount == 2, CompletionTimeout));
            Assert.False(waiting.IsCompleted);

            if (disposeAlgorithm)
                algorithm.Dispose();
            else
                pendingCancellation.Cancel();

            OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                waiting.WaitAsync(CompletionTimeout, testToken));
            if (disposeAlgorithm)
                Assert.False(pendingCancellation.IsCancellationRequested);
            else
                Assert.Equal(pendingCancellation.Token, actual.CancellationToken);
            Assert.Equal(1, algorithm.ActiveRequestCount);

            if (!disposeAlgorithm)
            {
                using var probeCancellation = CancellationTokenSource.CreateLinkedTokenSource(testToken);
                probeCancellation.CancelAfter(CompletionTimeout);
                Task<ActiveRequest> probe = Task.Run(() => algorithm.BeginRequestAsync(probeCancellation.Token));
                try
                {
                    Assert.True(SpinWait.SpinUntil(() => algorithm.ActiveRequestCount == 2, CompletionTimeout));
                    Assert.False(probe.IsCompleted);
                    probeCancellation.Cancel();
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                        probe.WaitAsync(CompletionTimeout, testToken));
                    Assert.Equal(1, algorithm.ActiveRequestCount);
                }
                finally
                {
                    probeCancellation.Cancel();
                    if (probe.IsCompletedSuccessfully)
                        (await probe).Dispose();
                }
            }
        }
        finally
        {
            pendingCancellation.Cancel();
            first.Dispose();
            if (waiting.IsCompletedSuccessfully)
                (await waiting).Dispose();
        }

        Assert.Equal(0, algorithm.ActiveRequestCount);
        if (!disposeAlgorithm)
        {
            using ActiveRequest later = await algorithm.BeginRequestAsync(testToken).WaitAsync(CompletionTimeout, testToken);
            Assert.Equal(1, later.ResultLimit);
        }
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 3)]
    [RequirementCoverage("REQ-VSB-REQUEST-RATE-CANCELLATION", "canceled-admission-refunds-only-its-own-rate-window")]
    public async Task CanceledAdmission_RefundsItsRatePermitOnlyWithinTheSameWindowAsync(
        bool resetBeforeCancellation, int availableInCurrentWindow)
    {
        TimeSpan interval = TimeSpan.FromMinutes(1);
        var clock = new FakeTimeProvider();
        using var algorithm = new RequestRateAlgorithm(new RequestRateAlgorithmOptions
        {
            PrefetchCount = 2,
            RequestResultLimit = 1,
            ConcurrentResultLimit = 1,
            RequestRateLimit = 3,
            RequestRateInterval = interval,
        }, clock);
        CancellationToken testToken = TestContext.Current.CancellationToken;

        using (ActiveRequest completed = await algorithm.BeginRequestAsync(testToken))
            await completed.CompleteAsync(1, testToken);
        Assert.Equal(2, algorithm.RequestCount);

        using var first = await algorithm.BeginRequestAsync(testToken);
        using var pendingCancellation = CancellationTokenSource.CreateLinkedTokenSource(testToken);
        pendingCancellation.CancelAfter(CompletionTimeout);
        Task<ActiveRequest> pending = Task.Run(() => algorithm.BeginRequestAsync(pendingCancellation.Token));
        try
        {
            Assert.True(SpinWait.SpinUntil(() => algorithm.ActiveRequestCount == 2, CompletionTimeout));
            Assert.False(pending.IsCompleted);
            if (resetBeforeCancellation)
                clock.Advance(interval);

            pendingCancellation.Cancel();
            OperationCanceledException failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                pending.WaitAsync(CompletionTimeout, testToken));
            Assert.Equal(pendingCancellation.Token, failure.CancellationToken);
            Assert.Equal(1, algorithm.ActiveRequestCount);
        }
        finally
        {
            pendingCancellation.Cancel();
            first.Dispose();
            if (pending.IsCompletedSuccessfully)
                (await pending).Dispose();
        }

        Assert.Equal(0, algorithm.ActiveRequestCount);
        await ConsumeAvailableAsync(availableInCurrentWindow);
        await AssertNoRateCapacityAsync();

        if (!resetBeforeCancellation)
        {
            clock.Advance(interval);
            await ConsumeAvailableAsync(3);
            await AssertNoRateCapacityAsync();
        }

        async Task ConsumeAvailableAsync(int count)
        {
            for (int index = 0; index < count; index++)
            {
                using ActiveRequest request = await algorithm.BeginRequestAsync(testToken).WaitAsync(CompletionTimeout, testToken);
                Assert.Equal(1, request.ResultLimit);
                await request.CompleteAsync(0, testToken);
            }
        }

        async Task AssertNoRateCapacityAsync()
        {
            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(testToken);
            Task<ActiveRequest> beyondLimit = algorithm.BeginRequestAsync(cancellation.Token);
            try
            {
                Assert.False(beyondLimit.IsCompleted);
                cancellation.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                    beyondLimit.WaitAsync(CompletionTimeout, testToken));
            }
            finally
            {
                cancellation.Cancel();
                if (beyondLimit.IsCompletedSuccessfully)
                    (await beyondLimit).Dispose();
            }
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-RATE-CANCELLATION", "refunded-permit-wakes-existing-rate-waiter")]
    public async Task CanceledAdmission_WakesAnAlreadyWaitingRateLimitedRequestAsync()
    {
        var clock = new FakeTimeProvider();
        using var algorithm = new RequestRateAlgorithm(new RequestRateAlgorithmOptions
        {
            PrefetchCount = 3,
            RequestResultLimit = 1,
            ConcurrentResultLimit = 1,
            RequestRateLimit = 4,
            RequestRateInterval = TimeSpan.FromMinutes(1),
        }, clock);
        CancellationToken testToken = TestContext.Current.CancellationToken;
        for (int index = 0; index < 2; index++)
        {
            using ActiveRequest completed = await algorithm.BeginRequestAsync(testToken);
            await completed.CompleteAsync(1, testToken);
        }
        Assert.Equal(3, algorithm.RequestCount);

        using var first = await algorithm.BeginRequestAsync(testToken);
        using var resultCancellation = CancellationTokenSource.CreateLinkedTokenSource(testToken);
        using var rateCancellation = CancellationTokenSource.CreateLinkedTokenSource(testToken);
        resultCancellation.CancelAfter(CompletionTimeout);
        rateCancellation.CancelAfter(CompletionTimeout);
        Task<ActiveRequest> waitingForResults = Task.Run(() => algorithm.BeginRequestAsync(resultCancellation.Token));
        Task<ActiveRequest>? waitingForRate = null;
        try
        {
            Assert.True(SpinWait.SpinUntil(() => algorithm.ActiveRequestCount == 2, CompletionTimeout));
            Assert.False(waitingForResults.IsCompleted);
            waitingForRate = algorithm.BeginRequestAsync(rateCancellation.Token);
            Assert.False(waitingForRate.IsCompleted);
            Assert.Equal(2, algorithm.ActiveRequestCount);

            resultCancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                waitingForResults.WaitAsync(CompletionTimeout, testToken));

            Assert.True(SpinWait.SpinUntil(() => algorithm.ActiveRequestCount == 2, CompletionTimeout));
            Assert.False(waitingForRate.IsCompleted);
            rateCancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                waitingForRate.WaitAsync(CompletionTimeout, testToken));
            Assert.Equal(1, algorithm.ActiveRequestCount);
        }
        finally
        {
            resultCancellation.Cancel();
            rateCancellation.Cancel();
            first.Dispose();
            if (waitingForResults.IsCompletedSuccessfully)
                (await waitingForResults).Dispose();
            if (waitingForRate?.IsCompletedSuccessfully == true)
                (await waitingForRate).Dispose();
        }

        Assert.Equal(0, algorithm.ActiveRequestCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-RATE-DYNAMIC-LIMIT", "invalid-and-unavailable-changes-are-rejected")]
    public async Task RateLimitChanges_RejectInvalidValuesMissingConfigurationAndDisposedInstancesAsync()
    {
        var clock = new FakeTimeProvider();
        using var configured = CreateRateLimitedAlgorithm(1, TimeSpan.FromMinutes(1), clock);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        foreach (int value in new[] { 0, -1 })
        {
            var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => configured.ChangeRateLimitAsync(value, cancellationToken));
            Assert.Equal("newRateLimit", exception.ParamName);
        }

        using var unconfigured = CreateAlgorithm(prefetchCount: 1, requestResultLimit: 1);
        await Assert.ThrowsAsync<InvalidOperationException>(() => unconfigured.ChangeRateLimitAsync(1, cancellationToken));

        configured.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => configured.ChangeRateLimitAsync(1, cancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-RATE-CLOCK", "active-request-cancellation-grace")]
    public async Task ParentCancellation_CancelsAnActiveRequestAtTheConfiguredClockBoundaryAsync()
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
        using ActiveRequest request = await algorithm.BeginRequestAsync(parent.Token);

        parent.Cancel();
        Assert.False(request.CancellationToken.IsCancellationRequested);

        clock.Advance(grace - TimeSpan.FromTicks(1));
        Assert.False(request.CancellationToken.IsCancellationRequested);

        clock.Advance(TimeSpan.FromTicks(1));
        Assert.True(request.CancellationToken.IsCancellationRequested);
    }

    [Theory]
    [InlineData(InvalidOption.PrefetchCount, "PrefetchCount")]
    [InlineData(InvalidOption.RequestResultLimit, "RequestResultLimit")]
    [InlineData(InvalidOption.ConcurrentResultLimit, "ConcurrentResultLimit")]
    [InlineData(InvalidOption.RequestRateLimit, "RequestRateLimit")]
    [InlineData(InvalidOption.RequestRateInterval, "RequestRateInterval")]
    [InlineData(InvalidOption.UnpairedRequestRateLimit, "RequestRateLimit and RequestRateInterval")]
    [InlineData(InvalidOption.UnpairedRequestRateInterval, "RequestRateLimit and RequestRateInterval")]
    [InlineData(InvalidOption.RequestCancellationTimeout, "RequestCancellationTimeout")]
    [RequirementCoverage("REQ-VSB-REQUEST-RATE-VALIDATION", "every-static-invariant-rejected")]
    public void Constructor_RejectsEveryInvalidStaticOption(InvalidOption option, string optionName)
    {
        var options = new RequestRateAlgorithmOptions
        {
            PrefetchCount = option == InvalidOption.PrefetchCount ? -1 : 1,
            RequestResultLimit = option == InvalidOption.RequestResultLimit ? -1 : 1,
            ConcurrentResultLimit = option == InvalidOption.ConcurrentResultLimit ? 0 : null,
            RequestRateLimit = option switch
            {
                InvalidOption.RequestRateLimit => 0,
                InvalidOption.UnpairedRequestRateLimit => 1,
                InvalidOption.RequestRateInterval => 1,
                _ => null,
            },
            RequestRateInterval = option switch
            {
                InvalidOption.RequestRateInterval => TimeSpan.Zero,
                InvalidOption.UnpairedRequestRateInterval => TimeSpan.FromSeconds(1),
                _ => null,
            },
            RequestCancellationTimeout = option == InvalidOption.RequestCancellationTimeout ? TimeSpan.Zero : null,
        };

        var exception = Assert.Throws<ArgumentException>(() => new RequestRateAlgorithm(options));

        Assert.Equal("options", exception.ParamName);
        Assert.Contains(optionName, exception.Message, StringComparison.Ordinal);
    }

    public enum InvalidOption
    {
        PrefetchCount,
        RequestResultLimit,
        ConcurrentResultLimit,
        RequestRateLimit,
        RequestRateInterval,
        UnpairedRequestRateLimit,
        UnpairedRequestRateInterval,
        RequestCancellationTimeout,
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

    private static RequestRateAlgorithm CreateRateLimitedAlgorithm(int rateLimit, TimeSpan interval, FakeTimeProvider clock)
    {
        return new RequestRateAlgorithm(new RequestRateAlgorithmOptions
        {
            PrefetchCount = 1,
            RequestResultLimit = 1,
            RequestRateLimit = rateLimit,
            RequestRateInterval = interval,
        }, clock);
    }

    private sealed record GroupedMessage(string GroupId, int SequenceNumber);
}
