using ViciOne.ServiceBus.Contracts;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Testing;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware;

public sealed class RateAndConcurrencyLimitTests
{
    private static readonly DateTimeOffset StartTime =
        new(2030, 1, 2, 3, 4, 5, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-RATE-LIMIT", "virtual-time-replenishment")]
    public async Task RateLimit_ReplenishesExactlyWhenTheConfiguredTimeProviderAdvancesAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var count = 0;
        TimeSpan interval = TimeSpan.FromMinutes(1);
        IPipe<LimitContext> pipe = Pipe.New<LimitContext>(configuration =>
        {
            configuration.UseRateLimit(2, interval, timeProvider: timeProvider);
            configuration.UseExecute(_ => Interlocked.Increment(ref count));
        });
        Assert.Equal(1, timeProvider.TimerCount);

        await pipe.SendAsync(new LimitContext());
        await pipe.SendAsync(new LimitContext());
        Task held = pipe.SendAsync(new LimitContext());
        Assert.False(held.IsCompleted);

        timeProvider.Advance(interval);
        await held;

        Assert.Equal(3, count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RATE-LIMIT", "faults-consume-permits")]
    public async Task RateLimit_CountsFaultedAndSuccessfulAttemptsEquallyAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var count = 0;
        TimeSpan interval = TimeSpan.FromMinutes(1);
        var expected = new LimitedOperationException("first failed");
        IPipe<LimitContext> pipe = Pipe.New<LimitContext>(configuration =>
        {
            configuration.UseRateLimit(1, interval, timeProvider: timeProvider);
            configuration.UseExecute(_ =>
            {
                if (Interlocked.Increment(ref count) == 1)
                    throw expected;
            });
        });
        Assert.Equal(1, timeProvider.TimerCount);

        LimitedOperationException actual = await Assert.ThrowsAsync<LimitedOperationException>(() =>
            pipe.SendAsync(new LimitContext()));
        Task held = pipe.SendAsync(new LimitContext());
        Assert.False(held.IsCompleted);

        timeProvider.Advance(interval);
        await held;

        Assert.Same(expected, actual);
        Assert.Equal(2, count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RATE-LIMIT", "dynamic-down-up-and-down")]
    public async Task RateLimit_AppliesRepeatedDynamicChangesInBothDirectionsAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var router = new PipeRouter();
        var count = 0;
        TimeSpan interval = TimeSpan.FromMinutes(1);
        IPipe<LimitContext> pipe = Pipe.New<LimitContext>(configuration =>
        {
            configuration.UseRateLimit(2, interval, router, timeProvider);
            configuration.UseExecute(_ => Interlocked.Increment(ref count));
        });

        await router.SetRateLimitAsync(1, cancellationToken: TestContext.Current.CancellationToken);
        await pipe.SendAsync(new LimitContext());
        Task heldAtOne = pipe.SendAsync(new LimitContext());
        Assert.False(heldAtOne.IsCompleted);

        await router.SetRateLimitAsync(2, cancellationToken: TestContext.Current.CancellationToken);
        await heldAtOne;
        timeProvider.Advance(interval);

        await router.SetRateLimitAsync(1, cancellationToken: TestContext.Current.CancellationToken);
        await pipe.SendAsync(new LimitContext());
        using var cancellation = new CancellationTokenSource();
        Task heldAfterSecondDecrease = pipe.SendAsync(new LimitContext(cancellation.Token));
        Assert.False(heldAfterSecondDecrease.IsCompleted);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => heldAfterSecondDecrease);

        Assert.Equal(3, count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RATE-LIMIT", "canceled-adjustment-rolls-back")]
    public async Task RateLimit_CanceledDecreaseReturnsEveryPermitAndKeepsThePreviousLimitAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        using var filter = new RateLimitFilter<LimitContext>(3, TimeSpan.FromHours(1), timeProvider);
        var count = 0;
        IPipe<LimitContext> next = Pipe.Execute<LimitContext>(_ => Interlocked.Increment(ref count));
        await filter.SendAsync(new LimitContext(), next);
        await filter.SendAsync(new LimitContext(), next);
        using var adjustmentCancellation = new CancellationTokenSource();
        Task adjustment = filter.SendAsync(new RateCommandContext(1, adjustmentCancellation.Token));
        Assert.False(adjustment.IsCompleted);

        adjustmentCancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => adjustment);
        await filter.SendAsync(new LimitContext(), next);

        using var heldCancellation = new CancellationTokenSource();
        Task held = filter.SendAsync(new LimitContext(heldCancellation.Token), next);
        Assert.False(held.IsCompleted);
        heldCancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => held);

        Assert.Equal(3, count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONCURRENCY-LIMIT", "dynamic-down-up")]
    public async Task ConcurrencyLimit_AppliesADecreaseFollowedByAnIncreaseToTheRunningPipeAsync()
    {
        var router = new PipeRouter();
        TaskCompletionSource[] entered = [NewSignal(), NewSignal(), NewSignal()];
        TaskCompletionSource[] release = [NewSignal(), NewSignal(), NewSignal()];
        IPipe<IndexedLimitContext> pipe = Pipe.New<IndexedLimitContext>(configuration =>
        {
            configuration.UseConcurrencyLimit(2, router);
            configuration.UseExecuteAsync(async context =>
            {
                entered[context.Index].SetResult();
                await release[context.Index].Task;
            });
        });

        Task first = pipe.SendAsync(new IndexedLimitContext(0));
        Task second = pipe.SendAsync(new IndexedLimitContext(1));
        await Task.WhenAll(entered[0].Task, entered[1].Task);

        Task decrease = router.SetConcurrencyLimitAsync(1, cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(decrease.IsCompleted);
        release[0].SetResult();
        await Task.WhenAll(first, decrease);

        Task third = pipe.SendAsync(new IndexedLimitContext(2));
        Assert.False(entered[2].Task.IsCompleted);
        await router.SetConcurrencyLimitAsync(2, cancellationToken: TestContext.Current.CancellationToken);
        await entered[2].Task;

        release[1].SetResult();
        release[2].SetResult();
        await Task.WhenAll(second, third);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(32)]
    [RequirementCoverage("REQ-VSB-CONCURRENCY-LIMIT", "exact-configured-maximum")]
    public async Task ConcurrencyLimit_AdmitsExactlyTheConfiguredMaximumAndQueuesTheRemainderAsync(int limit)
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var allSlotsEntered = NewSignal();
        var additionalEntry = NewSignal();
        var release = NewSignal();
        var entered = 0;
        var executing = 0;
        var maximum = 0;
        IPipe<LimitContext> pipe = Pipe.New<LimitContext>(configuration =>
        {
            configuration.UseConcurrencyLimit(limit);
            configuration.UseExecuteAsync(async _ =>
            {
                int current = Interlocked.Increment(ref executing);
                UpdateMaximum(ref maximum, current);
                int entry = Interlocked.Increment(ref entered);
                if (entry == limit)
                    allSlotsEntered.SetResult();
                else if (entry > limit)
                    additionalEntry.TrySetResult();

                await release.Task;
                Interlocked.Decrement(ref executing);
            });
        });

        Task[] sends = Enumerable.Range(0, limit + 2)
            .Select(_ => pipe.SendAsync(new LimitContext()))
            .ToArray();
        try
        {
            await allSlotsEntered.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal(limit, Volatile.Read(ref entered));
            Assert.Equal(limit, Volatile.Read(ref executing));
            Assert.Equal(limit, Volatile.Read(ref maximum));
            Assert.False(additionalEntry.Task.IsCompleted);
        }
        finally
        {
            release.TrySetResult();
        }

        await Task.WhenAll(sends).WaitAsync(timeout, cancellationToken);

        Assert.Equal(limit + 2, entered);
        Assert.Equal(0, executing);
        Assert.Equal(limit, maximum);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONCURRENCY-LIMIT", "canceled-adjustment-rolls-back")]
    public async Task ConcurrencyLimit_CanceledDecreaseReturnsAcquiredSlotsAndKeepsThePreviousLimitAsync()
    {
        using var filter = new ConcurrencyLimitFilter<IndexedLimitContext>(3);
        TaskCompletionSource[] entered = [NewSignal(), NewSignal(), NewSignal()];
        var release = NewSignal();
        IPipe<IndexedLimitContext> next = Pipe.ExecuteAsync<IndexedLimitContext>(async context =>
        {
            entered[context.Index].SetResult();
            await release.Task;
        });
        Task first = filter.SendAsync(new IndexedLimitContext(0), next);
        Task second = filter.SendAsync(new IndexedLimitContext(1), next);
        await Task.WhenAll(entered[0].Task, entered[1].Task);

        using var adjustmentCancellation = new CancellationTokenSource();
        Task adjustment = filter.SendAsync(new ConcurrencyCommandContext(1, adjustmentCancellation.Token));
        Assert.False(adjustment.IsCompleted);
        adjustmentCancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => adjustment);

        Task third = filter.SendAsync(new IndexedLimitContext(2), next);
        await entered[2].Task;
        release.SetResult();
        await Task.WhenAll(first, second, third);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MIDDLEWARE-LIMITS", "invalid-public-boundaries")]
    public void Limits_RejectInvalidConstructionValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ConcurrencyLimitFilter<LimitContext>(0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RateLimitFilter<LimitContext>(0, TimeSpan.FromSeconds(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RateLimitFilter<LimitContext>(1, TimeSpan.Zero));
    }

    private static TaskCompletionSource NewSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private static void UpdateMaximum(ref int maximum, int candidate)
    {
        int observed;
        do
        {
            observed = Volatile.Read(ref maximum);
            if (candidate <= observed)
                return;
        }
        while (Interlocked.CompareExchange(ref maximum, candidate, observed) != observed);
    }

    private sealed class LimitContext : BasePipeContext
    {
        public LimitContext()
        {
        }

        public LimitContext(CancellationToken cancellationToken)
            : base(cancellationToken)
        {
        }
    }

    private sealed class IndexedLimitContext(int index) : BasePipeContext
    {
        public int Index { get; } = index;
    }

    private sealed class RateCommandContext(int rateLimit, CancellationToken cancellationToken)
        : BasePipeContext(cancellationToken), CommandContext<SetRateLimit>
    {
        public DateTimeOffset Timestamp { get; } = StartTime;

        public SetRateLimit Command { get; } = new RateLimitCommand(rateLimit);
    }

    private sealed class ConcurrencyCommandContext(int concurrencyLimit, CancellationToken cancellationToken)
        : BasePipeContext(cancellationToken), CommandContext<SetConcurrencyLimit>
    {
        public DateTimeOffset Timestamp { get; } = StartTime;

        public SetConcurrencyLimit Command { get; } = new ConcurrencyLimitCommand(concurrencyLimit);
    }

    private sealed record RateLimitCommand(int RateLimit) : SetRateLimit;

    private sealed record ConcurrencyLimitCommand(int ConcurrencyLimit) : SetConcurrencyLimit
    {
        public DateTimeOffset? Timestamp => null;

        public string? Id => null;
    }

    private sealed class LimitedOperationException(string message) : Exception(message);
}
