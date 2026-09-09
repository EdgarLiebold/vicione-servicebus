using System.Reflection;
using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.Contracts;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.ConcurrencyLimiting;
using ViciOne.ServiceBus.Middleware.RateLimiting;
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
        Assert.Equal(0, timeProvider.TimerCount);

        await pipe.SendAsync(new LimitContext());
        await pipe.SendAsync(new LimitContext());
        Assert.Equal(0, timeProvider.TimerCount);
        Assert.Equal(0, timeProvider.ActiveTimerCount);
        Task held = pipe.SendAsync(new LimitContext());
        Assert.False(held.IsCompleted);
        Assert.Equal(1, timeProvider.TimerCount);
        Assert.Equal(1, timeProvider.ActiveTimerCount);

        timeProvider.Advance(interval);
        await held.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);

        Assert.Equal(3, count);
        Assert.Equal(1, timeProvider.TimerCount);
        Assert.Equal(0, timeProvider.ActiveTimerCount);
        timeProvider.Advance(interval);
        Assert.Equal(0, timeProvider.ActiveTimerCount);
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
        Assert.Equal(0, timeProvider.TimerCount);

        LimitedOperationException actual = await Assert.ThrowsAsync<LimitedOperationException>(() =>
            pipe.SendAsync(new LimitContext()));
        Assert.Equal(0, timeProvider.TimerCount);
        Task held = pipe.SendAsync(new LimitContext());
        Assert.False(held.IsCompleted);
        Assert.Equal(1, timeProvider.TimerCount);
        Assert.Equal(1, timeProvider.ActiveTimerCount);

        timeProvider.Advance(interval);
        await held.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);

        Assert.Same(expected, actual);
        Assert.Equal(2, count);
        Assert.Equal(1, timeProvider.TimerCount);
        Assert.Equal(0, timeProvider.ActiveTimerCount);
        timeProvider.Advance(interval);
        Assert.Equal(0, timeProvider.ActiveTimerCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RATE-LIMIT", "timer-cleanup-failure-cannot-own-admission")]
    public async Task RateLimit_TimerDisposalFailureDoesNotLosePermitsOrEscapeTheTimerCallbackAsync()
    {
        ILogContext? previousLogContext = LogContext.Current;
        var logger = new ThrowingLogger();
        LogContext.ConfigureCurrentLogContext(logger);
        var timeProvider = new ObservableTimeProvider(StartTime, new TimerCleanupException());
        TimeSpan interval = TimeSpan.FromMinutes(1);
        var count = 0;
        IPipe<LimitContext> pipe = Pipe.New<LimitContext>(configuration =>
        {
            configuration.UseRateLimit(1, interval, timeProvider: timeProvider);
            configuration.UseExecute(_ => Interlocked.Increment(ref count));
        });

        try
        {
            await pipe.SendAsync(new LimitContext());
            Task queued = pipe.SendAsync(new LimitContext());
            Assert.False(queued.IsCompleted);

            timeProvider.Advance(interval);
            await queued.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            timeProvider.Advance(interval);

            Assert.Equal(2, count);
            Assert.Equal(0, timeProvider.ActiveTimerCount);
            Assert.True(logger.CallCount >= 1);
        }
        finally
        {
            LogContext.Current = previousLogContext;
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RATE-LIMIT", "last-waiter-cancellation-disposes-interval-timer")]
    public async Task RateLimit_CancelingTheLastWaiterDisposesItsPendingIntervalTimerAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        TimeSpan interval = TimeSpan.FromHours(1);
        var count = 0;
        IPipe<LimitContext> pipe = Pipe.New<LimitContext>(configuration =>
        {
            configuration.UseRateLimit(1, interval, timeProvider: timeProvider);
            configuration.UseExecute(_ => Interlocked.Increment(ref count));
        });

        await pipe.SendAsync(new LimitContext());
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        Task queued = pipe.SendAsync(new LimitContext(cancellation.Token));
        Assert.False(queued.IsCompleted);
        Assert.Equal(1, timeProvider.ActiveTimerCount);

        cancellation.Cancel();
        OperationCanceledException observed = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            queued.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken));

        Assert.Equal(cancellation.Token, observed.CancellationToken);
        Assert.Equal(0, timeProvider.ActiveTimerCount);
        timeProvider.Advance(interval);
        await pipe.SendAsync(new LimitContext());
        Assert.Equal(2, count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RATE-LIMIT", "stale-timer-cannot-reset-a-new-window")]
    public async Task RateLimit_IgnoresAStaleTimerCallbackAfterANewWindowStartsAsync()
    {
        var timeProvider = new ControllableTimerTimeProvider(StartTime);
        TimeSpan interval = TimeSpan.FromMinutes(1);
        var count = 0;
        IPipe<LimitContext> pipe = Pipe.New<LimitContext>(configuration =>
        {
            configuration.UseRateLimit(1, interval, timeProvider: timeProvider);
            configuration.UseExecute(_ => Interlocked.Increment(ref count));
        });

        await pipe.SendAsync(new LimitContext());
        Task second = pipe.SendAsync(new LimitContext());
        Assert.False(second.IsCompleted);
        Assert.Equal(1, timeProvider.TimerCount);

        timeProvider.Advance(interval);
        Task third = pipe.SendAsync(new LimitContext());
        Task admitted = await Task.WhenAny(second, third).WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);
        await admitted.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);
        Task waiting = ReferenceEquals(admitted, second) ? third : second;
        await WaitUntilAsync(() => timeProvider.TimerCount == 2, TestContext.Current.CancellationToken);
        Assert.False(waiting.IsCompleted);

        timeProvider.FireTimer(0);
        Assert.False(waiting.IsCompleted);

        timeProvider.FireTimer(1);
        await waiting.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);
        Assert.Equal(3, count);
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
        await heldAtOne.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);
        timeProvider.Advance(interval);

        await router.SetRateLimitAsync(1, cancellationToken: TestContext.Current.CancellationToken);
        await pipe.SendAsync(new LimitContext());
        using var cancellation = new CancellationTokenSource();
        Task heldAfterSecondDecrease = pipe.SendAsync(new LimitContext(cancellation.Token));
        Assert.False(heldAfterSecondDecrease.IsCompleted);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            heldAfterSecondDecrease.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken));

        Assert.Equal(3, count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RATE-LIMIT", "canceled-adjustment-rolls-back")]
    public async Task RateLimit_CanceledDecreaseReturnsEveryPermitAndKeepsThePreviousLimitAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var filter = new RateLimitFilter<LimitContext>(3, TimeSpan.FromHours(1), timeProvider);
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
    [RequirementCoverage("REQ-VSB-RATE-LIMIT", "endpoint-budget-is-shared-across-message-types")]
    public async Task EndpointRateLimit_SharesOneAdmissionBudgetAcrossDifferentMessageTypesAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        TimeSpan interval = TimeSpan.FromMinutes(1);
        var timeProvider = new ObservableTimeProvider(StartTime);
        var firstEntered = NewSignal();
        var secondEntered = NewSignal();
        var secondArrived = NewSignal();
        var secondCompleted = NewSignal();
        string queueName = $"shared-rate-{NewId.NextGuid():N}";
        IBusControl bus = Bus.Factory.CreateUsingInMemory(configuration => configuration.ReceiveEndpoint(queueName, endpoint =>
        {
            endpoint.UseRateLimit(1, interval, timeProvider);
            endpoint.Handler<FirstRateLimitedMessage>(_ =>
            {
                firstEntered.TrySetResult();
                return Task.CompletedTask;
            });
            endpoint.Handler<SecondRateLimitedMessage>(_ =>
            {
                secondEntered.TrySetResult();
                return Task.CompletedTask;
            });
        }));
        var started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
            started = true;
            ISendEndpoint endpoint = await bus.GetSendEndpointAsync(new Uri($"queue:{queueName}"), cancellationToken)
                .WaitAsync(timeout, cancellationToken);
            Guid secondMessageId = NewId.NextGuid();
            using ConnectHandle observer = bus.ConnectReceiveObserver(
                new MessageArrivalObserver(secondMessageId, secondArrived, secondCompleted));

            await endpoint.SendAsync(new FirstRateLimitedMessage(), cancellationToken).WaitAsync(timeout, cancellationToken);
            await firstEntered.Task.WaitAsync(timeout, cancellationToken);
            await endpoint.SendAsync(
                    new SecondRateLimitedMessage(),
                    context => context.MessageId = secondMessageId,
                    cancellationToken)
                .WaitAsync(timeout, cancellationToken);
            await secondArrived.Task.WaitAsync(timeout, cancellationToken);
            Task timerCreated = timeProvider.WaitForTimerCountAsync(1);
            Task admissionState = await Task.WhenAny(timerCreated, secondEntered.Task)
                .WaitAsync(timeout, cancellationToken);

            Assert.Same(timerCreated, admissionState);
            Assert.False(secondEntered.Task.IsCompleted);
            Assert.Equal(1, timeProvider.ActiveTimerCount);

            timeProvider.Advance(interval);
            await secondEntered.Task.WaitAsync(timeout, cancellationToken);
            await secondCompleted.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal(0, timeProvider.ActiveTimerCount);
            Assert.Equal(1, timeProvider.TimerCount);
        }
        finally
        {
            timeProvider.Advance(interval);
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
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
            configuration.UseExecuteAwaited(async context =>
            {
                entered[context.Index].SetResult();
                await release[context.Index].Task.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);
            });
        });

        Task first = pipe.SendAsync(new IndexedLimitContext(0));
        Task second = pipe.SendAsync(new IndexedLimitContext(1));
        await Task.WhenAll(entered[0].Task, entered[1].Task)
            .WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);

        Task decrease = router.SetConcurrencyLimitAsync(1, cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(decrease.IsCompleted);
        release[0].SetResult();
        await Task.WhenAll(first, decrease).WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);

        Task third = pipe.SendAsync(new IndexedLimitContext(2));
        Assert.False(entered[2].Task.IsCompleted);
        await router.SetConcurrencyLimitAsync(2, cancellationToken: TestContext.Current.CancellationToken);
        await entered[2].Task.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);

        release[1].SetResult();
        release[2].SetResult();
        await Task.WhenAll(second, third).WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONCURRENCY-LIMIT", "local-router-id-and-timestamp-routing")]
    public async Task ConcurrencyLimit_IgnoresTargetedCommandsAndRejectsStaleBroadcastsAsync()
    {
        var filter = new ConcurrencyLimitFilter<LimitContext>(2);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await filter.SendAsync(new ConcurrencyCommandContext(
            concurrencyLimit: 4,
            cancellationToken,
            commandTimestamp: StartTime.AddMinutes(4),
            limiterId: "orders"));
        await filter.SendAsync(new ConcurrencyCommandContext(
            concurrencyLimit: 1,
            cancellationToken,
            contextTimestamp: StartTime.AddMinutes(2)));

        CommandException stale = await Assert.ThrowsAsync<CommandException>(() => filter.SendAsync(
            new ConcurrencyCommandContext(
                concurrencyLimit: 3,
                cancellationToken,
                commandTimestamp: StartTime.AddMinutes(1))));
        Assert.Contains("updated after", stale.Message, StringComparison.Ordinal);

        var entered = NewSignal();
        var release = NewSignal();
        IPipe<LimitContext> next = Pipe.ExecuteAwaited<LimitContext>(async _ =>
        {
            entered.TrySetResult();
            await release.Task.WaitAsync(OperationTimeout(), cancellationToken);
        });
        Task first = filter.SendAsync(new LimitContext(), next);
        await entered.Task.WaitAsync(OperationTimeout(), cancellationToken);
        Task second = filter.SendAsync(new LimitContext(), next);

        try
        {
            Assert.False(second.IsCompleted);
        }
        finally
        {
            release.TrySetResult();
        }

        await Task.WhenAll(first, second).WaitAsync(OperationTimeout(), cancellationToken);
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
            configuration.UseExecuteAwaited(async _ =>
            {
                int current = Interlocked.Increment(ref executing);
                UpdateMaximum(ref maximum, current);
                int entry = Interlocked.Increment(ref entered);
                if (entry == limit)
                    allSlotsEntered.SetResult();
                else if (entry > limit)
                    additionalEntry.TrySetResult();

                await release.Task.WaitAsync(timeout, cancellationToken);
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
        var filter = new ConcurrencyLimitFilter<IndexedLimitContext>(3);
        TaskCompletionSource[] entered = [NewSignal(), NewSignal(), NewSignal()];
        var release = NewSignal();
        IPipe<IndexedLimitContext> next = Pipe.ExecuteAwaited<IndexedLimitContext>(async context =>
        {
            entered[context.Index].SetResult();
            await release.Task.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);
        });
        Task first = filter.SendAsync(new IndexedLimitContext(0), next);
        Task second = filter.SendAsync(new IndexedLimitContext(1), next);
        await Task.WhenAll(entered[0].Task, entered[1].Task)
            .WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);

        using var adjustmentCancellation = new CancellationTokenSource();
        Task adjustment = filter.SendAsync(new ConcurrencyCommandContext(1, adjustmentCancellation.Token));
        Assert.False(adjustment.IsCompleted);
        adjustmentCancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            adjustment.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken));

        Task third = filter.SendAsync(new IndexedLimitContext(2), next);
        await entered[2].Task.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);
        release.SetResult();
        await Task.WhenAll(first, second, third).WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONCURRENCY-LIMIT", "transport-timestamp-fallback")]
    public async Task SharedConcurrencyLimiter_UsesTheTransportTimestampWhenTheCommandOmitsOneAsync()
    {
        var limiter = new ConcurrencyLimiter(2);
        LimitConsumeContext context = CreateLimitConsumeContext(
            new ExternalConcurrencyLimitCommand(4, null, null),
            StartTime,
            TestContext.Current.CancellationToken);

        await limiter.ConsumeAsync(context);

        Assert.Equal(4, ((IConcurrencyLimiter)limiter).Limit);
        Assert.Equal(1, ((LimitConsumeContextProxy)(object)context).ResponseCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONCURRENCY-LIMIT", "serialized-timestamped-updates")]
    public async Task SharedConcurrencyLimiter_SerializesConcurrentChangesWithoutPermitOrTimestampCorruptionAsync()
    {
        var limiter = new ConcurrencyLimiter(3);
        IConcurrencyLimiter limiterState = limiter;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await limiterState.WaitAsync(cancellationToken);
        await limiterState.WaitAsync(cancellationToken);
        var heldPermitCount = 2;

        try
        {
            LimitConsumeContext olderContext = CreateLimitConsumeContext(
                new ExternalConcurrencyLimitCommand(1, StartTime.AddMinutes(1), null),
                StartTime.AddMinutes(1),
                cancellationToken);
            Task older = limiter.ConsumeAsync(olderContext);
            await WaitUntilAsync(() => limiterState.Available == 0, cancellationToken);

            LimitConsumeContext newerContext = CreateLimitConsumeContext(
                new ExternalConcurrencyLimitCommand(4, StartTime.AddMinutes(2), null),
                StartTime.AddMinutes(2),
                cancellationToken);
            Task newer = limiter.ConsumeAsync(newerContext);

            Assert.False(newer.IsCompleted);

            limiterState.Release();
            limiterState.Release();
            heldPermitCount = 0;
            await Task.WhenAll(older, newer).WaitAsync(OperationTimeout(), cancellationToken);

            Assert.Equal(4, limiterState.Limit);
            Assert.Equal(1, ((LimitConsumeContextProxy)(object)olderContext).ResponseCount);
            Assert.Equal(1, ((LimitConsumeContextProxy)(object)newerContext).ResponseCount);
        }
        finally
        {
            for (var index = 0; index < heldPermitCount; index++)
                limiterState.Release();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONCURRENCY-LIMIT", "same-limit-timestamp-ordering")]
    public async Task SharedConcurrencyLimiter_RecordsAnAcceptedSameLimitCommandForStaleCommandDetectionAsync()
    {
        var limiter = new ConcurrencyLimiter(2);
        LimitConsumeContext newestContext = CreateLimitConsumeContext(
            new ExternalConcurrencyLimitCommand(2, StartTime.AddMinutes(2), null),
            StartTime.AddMinutes(2),
            TestContext.Current.CancellationToken);
        LimitConsumeContext staleContext = CreateLimitConsumeContext(
            new ExternalConcurrencyLimitCommand(4, StartTime.AddMinutes(1), null),
            StartTime.AddMinutes(1),
            TestContext.Current.CancellationToken);

        await limiter.ConsumeAsync(newestContext);
        CommandException exception = await Assert.ThrowsAsync<CommandException>(() => limiter.ConsumeAsync(staleContext));

        Assert.Contains("updated after", exception.Message, StringComparison.Ordinal);
        Assert.Equal(2, ((IConcurrencyLimiter)limiter).Limit);
        Assert.Equal(1, ((LimitConsumeContextProxy)(object)newestContext).ResponseCount);
        Assert.Equal(0, ((LimitConsumeContextProxy)(object)staleContext).ResponseCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONCURRENCY-LIMIT", "canceled-shared-decrease-rolls-back")]
    public async Task SharedConcurrencyLimiter_CanceledDecreaseRestoresPermitsAndTimestampStateAsync()
    {
        var limiter = new ConcurrencyLimiter(3);
        IConcurrencyLimiter limiterState = limiter;
        CancellationToken testCancellationToken = TestContext.Current.CancellationToken;
        await limiterState.WaitAsync(testCancellationToken);
        await limiterState.WaitAsync(testCancellationToken);
        var heldPermitCount = 2;
        using var adjustmentCancellation = CancellationTokenSource.CreateLinkedTokenSource(testCancellationToken);

        try
        {
            LimitConsumeContext canceledContext = CreateLimitConsumeContext(
                new ExternalConcurrencyLimitCommand(1, StartTime.AddMinutes(2), null),
                StartTime.AddMinutes(2),
                adjustmentCancellation.Token);
            Task canceledAdjustment = limiter.ConsumeAsync(canceledContext);
            await WaitUntilAsync(() => limiterState.Available == 0, testCancellationToken);

            adjustmentCancellation.Cancel();
            OperationCanceledException cancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                canceledAdjustment.WaitAsync(OperationTimeout(), testCancellationToken));
            Assert.Equal(adjustmentCancellation.Token, cancellation.CancellationToken);
            Assert.Equal(3, limiterState.Limit);
            Assert.Equal(1, limiterState.Available);

            limiterState.Release();
            limiterState.Release();
            heldPermitCount = 0;

            LimitConsumeContext earlierContext = CreateLimitConsumeContext(
                new ExternalConcurrencyLimitCommand(4, StartTime.AddMinutes(1), null),
                StartTime.AddMinutes(1),
                testCancellationToken);
            await limiter.ConsumeAsync(earlierContext);

            Assert.Equal(4, limiterState.Limit);
            Assert.Equal(4, limiterState.Available);
        }
        finally
        {
            for (var index = 0; index < heldPermitCount; index++)
                limiterState.Release();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONCURRENCY-LIMIT", "broadcast-and-case-insensitive-target-routing")]
    public async Task SharedConcurrencyLimiter_RoutesBroadcastAndTargetedCommandsDeterministicallyAsync()
    {
        var limiter = new ConcurrencyLimiter(2, "orders");
        IConcurrencyLimiter limiterState = limiter;
        LimitConsumeContext broadcast = CreateLimitConsumeContext(
            new ExternalConcurrencyLimitCommand(4, StartTime.AddMinutes(1), null),
            StartTime.AddMinutes(1),
            TestContext.Current.CancellationToken);
        LimitConsumeContext otherTarget = CreateLimitConsumeContext(
            new ExternalConcurrencyLimitCommand(3, StartTime.AddMinutes(2), "billing"),
            StartTime.AddMinutes(2),
            TestContext.Current.CancellationToken);
        LimitConsumeContext matchingTarget = CreateLimitConsumeContext(
            new ExternalConcurrencyLimitCommand(1, StartTime.AddMinutes(3), "ORDERS"),
            StartTime.AddMinutes(3),
            TestContext.Current.CancellationToken);

        await limiter.ConsumeAsync(broadcast);
        await limiter.ConsumeAsync(otherTarget);
        await limiter.ConsumeAsync(matchingTarget);

        Assert.Equal(1, limiterState.Limit);
        Assert.Equal(1, ((LimitConsumeContextProxy)(object)broadcast).ResponseCount);
        Assert.Equal(0, ((LimitConsumeContextProxy)(object)otherTarget).ResponseCount);
        Assert.Equal(1, ((LimitConsumeContextProxy)(object)matchingTarget).ResponseCount);
        Assert.Equal("orders", ((LimitConsumeContextProxy)(object)broadcast).ResponseId);
        Assert.Equal("orders", ((LimitConsumeContextProxy)(object)matchingTarget).ResponseId);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MIDDLEWARE-LIMITS", "invalid-public-boundaries")]
    public void Limits_RejectInvalidConstructionValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ConcurrencyLimitFilter<LimitContext>(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ConcurrencyLimiter(0));
        Assert.Throws<ArgumentException>(() => new ConcurrencyLimiter(1, " "));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RateLimitFilter<LimitContext>(0, TimeSpan.FromSeconds(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RateLimitFilter<LimitContext>(1, TimeSpan.Zero));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MIDDLEWARE-LIMITS", "configuration-entry-points-fail-before-pipeline-materialization")]
    public void FlowControlConfiguration_RejectsInvalidArgumentsAtThePublicBoundary()
    {
        ArgumentOutOfRangeException rateLimit = Assert.Throws<ArgumentOutOfRangeException>(() =>
            Pipe.New<LimitContext>(configuration => configuration.UseRateLimit(0)));
        ArgumentOutOfRangeException interval = Assert.Throws<ArgumentOutOfRangeException>(() =>
            Pipe.New<LimitContext>(configuration => configuration.UseRateLimit(1, TimeSpan.Zero)));
        ArgumentOutOfRangeException concurrencyLimit = Assert.Throws<ArgumentOutOfRangeException>(() =>
            Pipe.New<LimitContext>(configuration => configuration.UseConcurrencyLimit(0)));
        ArgumentOutOfRangeException partitionCount = Assert.Throws<ArgumentOutOfRangeException>(() =>
            Pipe.New<LimitContext>(configuration => configuration.UsePartitioner(0, _ => Array.Empty<byte>())));
        ArgumentNullException keyProvider = Assert.Throws<ArgumentNullException>(() =>
            Pipe.New<LimitContext>(configuration => configuration.UsePartitioner(1, (Func<LimitContext, byte[]>)null!)));
        ArgumentNullException partitioner = Assert.Throws<ArgumentNullException>(() =>
            Pipe.New<LimitContext>(configuration =>
                configuration.UsePartitioner((IPartitioner)null!, _ => Array.Empty<byte>())));

        Assert.Equal("rateLimit", rateLimit.ParamName);
        Assert.Equal("interval", interval.ParamName);
        Assert.Equal("concurrencyLimit", concurrencyLimit.ParamName);
        Assert.Equal("partitionCount", partitionCount.ParamName);
        Assert.Equal("keyProvider", keyProvider.ParamName);
        Assert.Equal("partitioner", partitioner.ParamName);
    }

    private static TaskCompletionSource NewSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static LimitConsumeContext CreateLimitConsumeContext(
        SetConcurrencyLimit command,
        DateTimeOffset? sentTime,
        CancellationToken cancellationToken = default)
    {
        LimitConsumeContext context = DispatchProxy.Create<LimitConsumeContext, LimitConsumeContextProxy>();
        ((LimitConsumeContextProxy)(object)context).Configure(command, sentTime, cancellationToken);
        return context;
    }

    private static async Task WaitUntilAsync(Func<bool> condition, CancellationToken cancellationToken)
    {
        DateTimeOffset deadline = TimeProvider.System.GetUtcNow() + OperationTimeout();
        while (!condition())
        {
            if (TimeProvider.System.GetUtcNow() >= deadline)
                throw new TimeoutException("The expected limiter state was not reached before the test deadline.");

            await Task.Delay(TimeSpan.FromMilliseconds(10), cancellationToken);
        }
    }

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

    private sealed class ConcurrencyCommandContext(
        int concurrencyLimit,
        CancellationToken cancellationToken,
        DateTimeOffset? commandTimestamp = null,
        string? limiterId = null,
        DateTimeOffset? contextTimestamp = null)
        : BasePipeContext(cancellationToken), CommandContext<SetConcurrencyLimit>
    {
        public DateTimeOffset Timestamp { get; } = contextTimestamp ?? StartTime;

        public SetConcurrencyLimit Command { get; } = new ExternalConcurrencyLimitCommand(concurrencyLimit, commandTimestamp, limiterId);
    }

    private sealed record RateLimitCommand(int RateLimit) : SetRateLimit;

    private sealed record ExternalConcurrencyLimitCommand(
        int ConcurrencyLimit,
        DateTimeOffset? Timestamp,
        string? LimiterId) : SetConcurrencyLimit;

    public sealed record FirstRateLimitedMessage;

    public sealed record SecondRateLimitedMessage;

    private sealed class MessageArrivalObserver(
        Guid messageId,
        TaskCompletionSource arrived,
        TaskCompletionSource completed) : IReceiveObserver
    {
        public Task PreReceiveAsync(ReceiveContext context)
        {
            if (context.GetMessageId() == messageId)
                arrived.TrySetResult();

            return Task.CompletedTask;
        }

        public Task PostReceiveAsync(ReceiveContext context)
        {
            if (context.GetMessageId() == messageId)
                completed.TrySetResult();

            return Task.CompletedTask;
        }

        public Task PostConsumeAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType)
            where T : class => Task.CompletedTask;

        public Task ConsumeFaultAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception)
            where T : class => Task.CompletedTask;

        public Task ReceiveFaultAsync(ReceiveContext context, Exception exception) => Task.CompletedTask;
    }

    private interface LimitConsumeContext : ConsumeContext<SetConcurrencyLimit>, ConsumeContext;

    private class LimitConsumeContextProxy : DispatchProxy
    {
        private CancellationToken _cancellationToken;
        private SetConcurrencyLimit? _command;
        private DateTimeOffset? _sentTime;

        public int ResponseCount { get; private set; }

        public string? ResponseId { get; private set; }

        public void Configure(SetConcurrencyLimit command, DateTimeOffset? sentTime, CancellationToken cancellationToken)
        {
            _command = command;
            _sentTime = sentTime;
            _cancellationToken = cancellationToken;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name == "get_Message")
                return _command ?? throw new InvalidOperationException("The consume context was not configured.");
            if (targetMethod.Name == "get_SentTime")
                return _sentTime;
            if (targetMethod.Name == "get_CancellationToken")
                return _cancellationToken;
            if (targetMethod.Name == nameof(PipeContext.TryGetPayload) && targetMethod.IsGenericMethod)
            {
                args![0] = null;
                return false;
            }
            if (targetMethod.Name == nameof(ConsumeContext.RespondAsync))
            {
                ResponseCount++;
                ResponseId = args?[0]?.GetType().GetProperty(nameof(ConcurrencyLimitUpdated.LimiterId))?.GetValue(args[0]) as string;
                return Task.CompletedTask;
            }

            throw new NotSupportedException($"Unexpected consume-context member: {targetMethod.Name}");
        }
    }

    private sealed class LimitedOperationException(string message) : Exception(message);

    private sealed class TimerCleanupException : Exception;

    private sealed class ThrowingLogger : ILogger
    {
        private int _callCount;

        public int CallCount => Volatile.Read(ref _callCount);

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Interlocked.Increment(ref _callCount);
            throw new InvalidOperationException("Secondary logger failure");
        }
    }

    private sealed class ControllableTimerTimeProvider(DateTimeOffset startTime) : TimeProvider
    {
        private readonly object _lock = new();
        private readonly List<TimerRegistration> _timers = [];
        private DateTimeOffset _utcNow = startTime;
        private long _timestamp;

        public int TimerCount
        {
            get
            {
                lock (_lock)
                    return _timers.Count;
            }
        }

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override DateTimeOffset GetUtcNow()
        {
            lock (_lock)
                return _utcNow;
        }

        public override long GetTimestamp()
        {
            lock (_lock)
                return _timestamp;
        }

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            ArgumentNullException.ThrowIfNull(callback);

            var timer = new TimerRegistration(callback, state);
            lock (_lock)
                _timers.Add(timer);

            return timer;
        }

        public void Advance(TimeSpan elapsed)
        {
            lock (_lock)
            {
                _utcNow += elapsed;
                _timestamp += elapsed.Ticks;
            }
        }

        public void FireTimer(int index)
        {
            TimerRegistration timer;
            lock (_lock)
                timer = _timers[index];

            timer.FireQueuedCallback();
        }

        private sealed class TimerRegistration(TimerCallback callback, object? state) : ITimer
        {
            public bool Change(TimeSpan newDueTime, TimeSpan newPeriod)
            {
                return true;
            }

            public void Dispose()
            {
            }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;

            public void FireQueuedCallback() => callback(state);
        }
    }
}
