using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.CircuitBreaker;
using ViciOne.ServiceBus.Monitoring;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Testing;
using Xunit;
using ActivityContext = System.Diagnostics.ActivityContext;

namespace ViciOne.ServiceBus.Tests.Middleware.CircuitBreaker;

[Collection(OpenTelemetryGlobalCollection.Name)]
public sealed class CircuitBreakerFilterTests
{
    private static readonly DateTimeOffset StartTime =
        new(2030, 1, 2, 3, 4, 5, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-CIRCUIT-BREAKER-TRIP", "inclusive-throughput-and-ratio-boundary")]
    public async Task ExactMinimumThroughputAndFailureRatio_OpenTheCircuitInclusivelyAsync()
    {
        var time = new ObservableTimeProvider(StartTime);
        var outcomes = new Queue<Exception?>(
        [
            null,
            null,
            new ExpectedFailureException("failure one"),
            new ExpectedFailureException("failure two"),
            null,
        ]);
        var protectedCallCount = 0;
        IPipe<TestPipeContext> pipe = CreatePipe(time, async _ =>
        {
            Interlocked.Increment(ref protectedCallCount);
            Exception? outcome = outcomes.Dequeue();
            if (outcome is not null)
                throw outcome;

            await Task.CompletedTask;
        }, options => options.SetMinimumThroughput(4).SetFailureRatio(0.50));

        await pipe.SendAsync(new TestPipeContext());
        await pipe.SendAsync(new TestPipeContext());
        await Assert.ThrowsAsync<ExpectedFailureException>(() => pipe.SendAsync(new TestPipeContext()));
        await Assert.ThrowsAsync<ExpectedFailureException>(() => pipe.SendAsync(new TestPipeContext()));
        await Assert.ThrowsAsync<CircuitBreakerOpenException>(() => pipe.SendAsync(new TestPipeContext()));

        Assert.Equal(4, protectedCallCount);
        Assert.Equal(0, time.TimerCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CIRCUIT-BREAKER-TRIP", "below-minimum-throughput-stays-closed")]
    public async Task MatchingFailuresBelowMinimumThroughput_KeepTheCircuitClosedAsync()
    {
        var time = new ObservableTimeProvider(StartTime);
        var fail = true;
        var protectedCallCount = 0;
        IPipe<TestPipeContext> pipe = CreatePipe(time, _ =>
        {
            Interlocked.Increment(ref protectedCallCount);
            return fail
                ? Task.FromException(new ExpectedFailureException("below minimum throughput"))
                : Task.CompletedTask;
        }, options => options.SetMinimumThroughput(3).SetFailureRatio(1));

        await Assert.ThrowsAsync<ExpectedFailureException>(() => pipe.SendAsync(new TestPipeContext()));
        await Assert.ThrowsAsync<ExpectedFailureException>(() => pipe.SendAsync(new TestPipeContext()));
        fail = false;
        await pipe.SendAsync(new TestPipeContext());

        Assert.Equal(3, protectedCallCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CIRCUIT-BREAKER-TRIP", "below-failure-ratio-stays-closed")]
    public async Task MatchingFailuresBelowFailureRatio_KeepTheCircuitClosedAsync()
    {
        var time = new ObservableTimeProvider(StartTime);
        var outcomes = new Queue<Exception?>(
        [
            null,
            new ExpectedFailureException("below failure ratio"),
            null,
        ]);
        var protectedCallCount = 0;
        IPipe<TestPipeContext> pipe = CreatePipe(time, _ =>
        {
            Interlocked.Increment(ref protectedCallCount);
            Exception? outcome = outcomes.Dequeue();
            return outcome is null ? Task.CompletedTask : Task.FromException(outcome);
        }, options => options.SetMinimumThroughput(2).SetFailureRatio(0.75));

        await pipe.SendAsync(new TestPipeContext());
        await Assert.ThrowsAsync<ExpectedFailureException>(() => pipe.SendAsync(new TestPipeContext()));
        await pipe.SendAsync(new TestPipeContext());

        Assert.Equal(3, protectedCallCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CIRCUIT-BREAKER-TRIP", "sampling-counters-saturate-without-wrapping")]
    public void ClosedSamplingCounters_SaturateWithoutWrappingOrSuppressingATrip()
    {
        var time = new ObservableTimeProvider(StartTime);
        CircuitBreakerSettings settings = new CircuitBreakerOptions()
            .SetMinimumThroughput(1)
            .SetFailureRatio(1)
            .SetSamplingDuration(TimeSpan.FromMinutes(1))
            .SetBreakDuration(TimeSpan.FromSeconds(1))
            .SetTimeProvider(time)
            .CreateSettings();
        var stateMachine = new CircuitBreakerStateMachine(settings);
        CircuitBreakerLease lease = stateMachine.Acquire();
        CircuitBreakerStateMachine.ClosedState closed = Assert.IsType<CircuitBreakerStateMachine.ClosedState>(lease.State);
        closed.AttemptCount = int.MaxValue;
        closed.FailureCount = int.MaxValue;

        CircuitBreakerLease saturatedLease = stateMachine.Acquire();
        stateMachine.RecordFailure(saturatedLease, new ExpectedFailureException("saturated failure"));

        Assert.Equal(int.MaxValue, closed.AttemptCount);
        Assert.Equal(int.MaxValue, closed.FailureCount);
        Assert.Equal("open", stateMachine.GetSnapshot().State);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CIRCUIT-BREAKER-TRIP", "lazy-sampling-window-boundary")]
    public async Task FirstAdmissionAtSamplingBoundary_StartsAFreshWindowWithoutATimerAsync()
    {
        var time = new ObservableTimeProvider(StartTime);
        var protectedCallCount = 0;
        IPipe<TestPipeContext> pipe = CreatePipe(time, _ =>
        {
            Interlocked.Increment(ref protectedCallCount);
            throw new ExpectedFailureException("resource unavailable");
        }, options => options
            .SetMinimumThroughput(2)
            .SetFailureRatio(1)
            .SetSamplingDuration(TimeSpan.FromMinutes(1)));

        await Assert.ThrowsAsync<ExpectedFailureException>(() => pipe.SendAsync(new TestPipeContext()));
        time.Advance(TimeSpan.FromMinutes(1));
        await Assert.ThrowsAsync<ExpectedFailureException>(() => pipe.SendAsync(new TestPipeContext()));
        await Assert.ThrowsAsync<ExpectedFailureException>(() => pipe.SendAsync(new TestPipeContext()));
        await Assert.ThrowsAsync<CircuitBreakerOpenException>(() => pipe.SendAsync(new TestPipeContext()));

        Assert.Equal(3, protectedCallCount);
        Assert.Equal(0, time.TimerCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CIRCUIT-BREAKER-OPEN", "distinct-rejection-and-exact-retry-delay")]
    public async Task OpenCircuit_RejectsBeforeTheProtectedPipeWithADistinctFailureAsync()
    {
        var time = new ObservableTimeProvider(StartTime);
        var expected = new ExpectedFailureException("resource unavailable");
        var protectedCallCount = 0;
        IPipe<TestPipeContext> pipe = CreatePipe(time, _ =>
        {
            Interlocked.Increment(ref protectedCallCount);
            throw expected;
        });

        await Assert.ThrowsAsync<ExpectedFailureException>(() => pipe.SendAsync(new TestPipeContext()));
        CircuitBreakerOpenException rejection = await Assert.ThrowsAsync<CircuitBreakerOpenException>(
            () => pipe.SendAsync(new TestPipeContext()));

        Assert.Equal(1, protectedCallCount);
        Assert.Same(expected, rejection.InnerException);
        Assert.Equal(TimeSpan.FromSeconds(1), rejection.RetryAfter);
        Assert.False(rejection.ProbeInProgress);

        time.Advance(TimeSpan.FromSeconds(1) - TimeSpan.FromTicks(1));
        CircuitBreakerOpenException boundaryRejection = await Assert.ThrowsAsync<CircuitBreakerOpenException>(
            () => pipe.SendAsync(new TestPipeContext()));
        Assert.Equal(TimeSpan.FromTicks(1), boundaryRejection.RetryAfter);
        Assert.Equal(1, protectedCallCount);

        time.Advance(TimeSpan.FromTicks(1));
        await Assert.ThrowsAsync<ExpectedFailureException>(() => pipe.SendAsync(new TestPipeContext()));
        Assert.Equal(2, protectedCallCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CIRCUIT-BREAKER-HALF-OPEN", "exactly-one-concurrent-probe")]
    public async Task HalfOpen_AdmitsExactlyOneProbeAndRejectsEveryConcurrentCompetitorAsync()
    {
        const int contenderCount = 33;
        using var time = new ContendedTimeProvider(StartTime, OperationTimeout, TestCancellationToken);
        var probeEntered = NewSignal();
        var releaseProbe = NewSignal();
        var protectedCallCount = 0;
        var recoveryEntrants = 0;
        var decisions = 0;
        var rejections = 0;
        var allDecided = NewSignal();
        var fail = true;
        IPipe<TestPipeContext> pipe = CreatePipe(time, async _ =>
        {
            Interlocked.Increment(ref protectedCallCount);
            if (fail)
                throw new ExpectedFailureException("initial trip");

            Interlocked.Increment(ref recoveryEntrants);
            probeEntered.TrySetResult();
            if (Interlocked.Increment(ref decisions) == contenderCount)
                allDecided.TrySetResult();
            await releaseProbe.Task.WaitAsync(OperationTimeout, TestCancellationToken);
        });

        await Assert.ThrowsAsync<ExpectedFailureException>(() => pipe.SendAsync(new TestPipeContext()));
        fail = false;
        time.Advance(TimeSpan.FromSeconds(1));
        time.ArmContention(contenderCount);
        Task<Exception?>[] attempts = Enumerable.Range(0, contenderCount)
            .Select(_ => Task.Factory.StartNew(
                async () =>
                {
                    try
                    {
                        await pipe.SendAsync(new TestPipeContext());
                        return null;
                    }
                    catch (Exception exception)
                    {
                        if (exception is CircuitBreakerOpenException)
                            Interlocked.Increment(ref rejections);
                        if (Interlocked.Increment(ref decisions) == contenderCount)
                            allDecided.TrySetResult();
                        return exception;
                    }
                },
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default).Unwrap())
            .ToArray();

        try
        {
            await time.AllContendersArrived.WaitAsync(OperationTimeout, TestCancellationToken);
            time.ReleaseContenders();
            await probeEntered.Task.WaitAsync(OperationTimeout, TestCancellationToken);
            await allDecided.Task.WaitAsync(OperationTimeout, TestCancellationToken);

            Assert.Equal(1, recoveryEntrants);
            Assert.Equal(contenderCount - 1, rejections);
            Assert.Equal(2, protectedCallCount);
        }
        finally
        {
            time.ReleaseContenders();
            releaseProbe.TrySetResult();
            await Task.WhenAll(attempts).WaitAsync(OperationTimeout, CancellationToken.None);
        }

        Exception?[] results = attempts.Select(task => task.Result).ToArray();
        CircuitBreakerOpenException[] rejectedResults = results.OfType<CircuitBreakerOpenException>().ToArray();
        Assert.Equal(contenderCount - 1, rejectedResults.Length);
        Assert.Single(results, result => result is null);
        Assert.All(rejectedResults, rejection =>
        {
            Assert.True(rejection.ProbeInProgress);
            Assert.Equal(TimeSpan.Zero, rejection.RetryAfter);
        });
        await pipe.SendAsync(new TestPipeContext());

        Assert.Equal(3, protectedCallCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CIRCUIT-BREAKER-HALF-OPEN", "classified-failure-escalates-saturates-and-success-resets")]
    public async Task ClassifiedProbeFailure_EscalatesSaturatesAndSuccessResetsTheBreakDurationAsync()
    {
        var time = new ObservableTimeProvider(StartTime);
        var outcome = ProbeOutcome.Fail;
        IPipe<TestPipeContext> pipe = CreatePipe(time, _ => outcome switch
        {
            ProbeOutcome.Fail => Task.FromException(new ExpectedFailureException("resource unavailable")),
            _ => Task.CompletedTask,
        }, options => options.SetBreakDurations(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3)));

        await Assert.ThrowsAsync<ExpectedFailureException>(() => pipe.SendAsync(new TestPipeContext()));
        time.Advance(TimeSpan.FromSeconds(1));
        await Assert.ThrowsAsync<ExpectedFailureException>(() => pipe.SendAsync(new TestPipeContext()));
        CircuitBreakerOpenException escalated = await Assert.ThrowsAsync<CircuitBreakerOpenException>(
            () => pipe.SendAsync(new TestPipeContext()));
        Assert.Equal(TimeSpan.FromSeconds(3), escalated.RetryAfter);

        time.Advance(TimeSpan.FromSeconds(3));
        await Assert.ThrowsAsync<ExpectedFailureException>(() => pipe.SendAsync(new TestPipeContext()));
        CircuitBreakerOpenException saturated = await Assert.ThrowsAsync<CircuitBreakerOpenException>(
            () => pipe.SendAsync(new TestPipeContext()));
        Assert.Equal(TimeSpan.FromSeconds(3), saturated.RetryAfter);

        outcome = ProbeOutcome.Succeed;
        time.Advance(TimeSpan.FromSeconds(3));
        await pipe.SendAsync(new TestPipeContext());

        outcome = ProbeOutcome.Fail;
        await Assert.ThrowsAsync<ExpectedFailureException>(() => pipe.SendAsync(new TestPipeContext()));
        CircuitBreakerOpenException reset = await Assert.ThrowsAsync<CircuitBreakerOpenException>(
            () => pipe.SendAsync(new TestPipeContext()));
        Assert.Equal(TimeSpan.FromSeconds(1), reset.RetryAfter);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CIRCUIT-BREAKER-HALF-OPEN", "caller-cancellation-releases-probe")]
    public async Task CallerCancellation_ReleasesTheProbeWithoutClosingOrReopeningAsync()
    {
        var time = new ObservableTimeProvider(StartTime);
        var recoveryEntered = NewSignal();
        var releaseRecovery = NewSignal();
        var outcome = ProbeOutcome.Fail;
        var recoveryCallCount = 0;
        IPipe<TestPipeContext> pipe = CreatePipe(time, async context =>
        {
            switch (outcome)
            {
                case ProbeOutcome.Fail:
                    throw new ExpectedFailureException("initial trip");
                case ProbeOutcome.Cancel:
                    throw new OperationCanceledException("caller canceled");
                case ProbeOutcome.HoldRecovery:
                    if (Interlocked.Increment(ref recoveryCallCount) == 1)
                    {
                        recoveryEntered.SetResult();
                        await releaseRecovery.Task.WaitAsync(OperationTimeout, TestCancellationToken);
                    }
                    break;
            }
        });

        await Assert.ThrowsAsync<ExpectedFailureException>(() => pipe.SendAsync(new TestPipeContext()));
        time.Advance(TimeSpan.FromSeconds(1));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        outcome = ProbeOutcome.Cancel;
        OperationCanceledException canceled = await Assert.ThrowsAsync<OperationCanceledException>(
            () => pipe.SendAsync(new TestPipeContext(cancellation.Token)));
        Assert.Equal(CancellationToken.None, canceled.CancellationToken);

        outcome = ProbeOutcome.HoldRecovery;
        Task recovery = pipe.SendAsync(new TestPipeContext());
        try
        {
            await recoveryEntered.Task.WaitAsync(OperationTimeout, TestCancellationToken);
            CircuitBreakerOpenException competitor = await Assert.ThrowsAsync<CircuitBreakerOpenException>(
                () => pipe.SendAsync(new TestPipeContext()));
            Assert.True(competitor.ProbeInProgress);
            Assert.Equal(1, recoveryCallCount);
        }
        finally
        {
            releaseRecovery.TrySetResult();
            await recovery.WaitAsync(OperationTimeout, CancellationToken.None);
        }

        outcome = ProbeOutcome.Succeed;
        await pipe.SendAsync(new TestPipeContext());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [RequirementCoverage("REQ-VSB-CIRCUIT-BREAKER-HALF-OPEN", "nested-caller-versus-mixed-and-dependency-cancellation")]
    public async Task NestedCancellation_ReleasesOnlyPureCallerProbeAsync(int kind)
    {
        var time = new ObservableTimeProvider(StartTime);
        var protectedCalls = 0;
        Exception? outcome = new ExpectedFailureException("initial trip");
        IPipe<TestPipeContext> pipe = CreatePipe(time, _ =>
        {
            Interlocked.Increment(ref protectedCalls);
            return outcome is null ? Task.CompletedTask : Task.FromException(outcome);
        });

        await Assert.ThrowsAsync<ExpectedFailureException>(() => pipe.SendAsync(new TestPipeContext()));
        time.Advance(TimeSpan.FromSeconds(1));
        using var caller = new CancellationTokenSource();
        using var dependency = new CancellationTokenSource();
        caller.Cancel();
        dependency.Cancel();
        Exception canceled = kind == 3
            ? new OperationCanceledException("operation canceled",
                new ExpectedFailureException("inner business failure"), caller.Token)
            : new OperationCanceledException("operation canceled",
                kind == 2 ? dependency.Token : caller.Token);
        outcome = kind switch
        {
            1 => new AggregateException(new AggregateException(canceled),
                new ExpectedFailureException("business failure")),
            4 => new AggregateException(new AggregateException(canceled), new AggregateException()),
            _ => new AggregateException(new AggregateException(canceled)),
        };

        AggregateException observed = await Assert.ThrowsAsync<AggregateException>(
            () => pipe.SendAsync(new TestPipeContext(caller.Token)));
        Assert.Same(outcome, observed);
        outcome = null;

        if (kind == 0)
        {
            await pipe.SendAsync(new TestPipeContext());
            Assert.Equal(3, protectedCalls);
        }
        else
        {
            CircuitBreakerOpenException rejection = await Assert.ThrowsAsync<CircuitBreakerOpenException>(
                () => pipe.SendAsync(new TestPipeContext()));
            Assert.Same(observed, rejection.InnerException);
            Assert.False(rejection.ProbeInProgress);
            Assert.Equal(2, protectedCalls);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CIRCUIT-BREAKER-HALF-OPEN", "dependency-cancellation-reopens")]
    public async Task DependencyCancellation_IsAClassifiedProbeFailureAndReopensTheCircuitAsync()
    {
        var time = new ObservableTimeProvider(StartTime);
        Exception outcome = new ExpectedFailureException("initial trip");
        IPipe<TestPipeContext> pipe = CreatePipe(time, _ => Task.FromException(outcome));

        await Assert.ThrowsAsync<ExpectedFailureException>(() => pipe.SendAsync(new TestPipeContext()));
        time.Advance(TimeSpan.FromSeconds(1));
        using var dependencyCancellation = new CancellationTokenSource();
        dependencyCancellation.Cancel();
        outcome = new OperationCanceledException("dependency timeout", dependencyCancellation.Token);
        await Assert.ThrowsAsync<OperationCanceledException>(() => pipe.SendAsync(new TestPipeContext()));

        CircuitBreakerOpenException rejection = await Assert.ThrowsAsync<CircuitBreakerOpenException>(
            () => pipe.SendAsync(new TestPipeContext()));
        Assert.Same(outcome, rejection.InnerException);
        Assert.Equal(TimeSpan.FromSeconds(1), rejection.RetryAfter);
        Assert.False(rejection.ProbeInProgress);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CIRCUIT-BREAKER-HALF-OPEN", "distinct-dependency-cancellation-wins-over-concurrent-caller-cancellation")]
    public async Task DistinctDependencyCancellation_ReopensEvenWhenTheCallerIsAlsoCanceledAsync()
    {
        var time = new ObservableTimeProvider(StartTime);
        Exception outcome = new ExpectedFailureException("initial trip");
        IPipe<TestPipeContext> pipe = CreatePipe(time, _ => Task.FromException(outcome));

        await Assert.ThrowsAsync<ExpectedFailureException>(() => pipe.SendAsync(new TestPipeContext()));
        time.Advance(TimeSpan.FromSeconds(1));

        using var callerCancellation = new CancellationTokenSource();
        using var dependencyCancellation = new CancellationTokenSource();
        callerCancellation.Cancel();
        dependencyCancellation.Cancel();
        outcome = new OperationCanceledException("dependency timeout", dependencyCancellation.Token);

        OperationCanceledException observed = await Assert.ThrowsAsync<OperationCanceledException>(
            () => pipe.SendAsync(new TestPipeContext(callerCancellation.Token)));
        Assert.Same(outcome, observed);

        CircuitBreakerOpenException rejection = await Assert.ThrowsAsync<CircuitBreakerOpenException>(
            () => pipe.SendAsync(new TestPipeContext()));
        Assert.Same(outcome, rejection.InnerException);
        Assert.False(rejection.ProbeInProgress);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CIRCUIT-BREAKER-HALF-OPEN", "matching-but-not-canceled-caller-token-reopens")]
    public async Task MatchingButNotCanceledContextToken_IsAClassifiedDependencyFailureAsync()
    {
        var time = new ObservableTimeProvider(StartTime);
        using var callerCancellation = new CancellationTokenSource();
        Exception outcome = new ExpectedFailureException("initial trip");
        IPipe<TestPipeContext> pipe = CreatePipe(time, _ => Task.FromException(outcome));

        await Assert.ThrowsAsync<ExpectedFailureException>(() => pipe.SendAsync(new TestPipeContext()));
        time.Advance(TimeSpan.FromSeconds(1));
        outcome = new OperationCanceledException("dependency used the context token", callerCancellation.Token);
        OperationCanceledException observed = await Assert.ThrowsAsync<OperationCanceledException>(
            () => pipe.SendAsync(new TestPipeContext(callerCancellation.Token)));

        Assert.False(callerCancellation.IsCancellationRequested);
        Assert.Same(outcome, observed);
        CircuitBreakerOpenException rejection = await Assert.ThrowsAsync<CircuitBreakerOpenException>(
            () => pipe.SendAsync(new TestPipeContext()));
        Assert.Same(outcome, rejection.InnerException);
        Assert.False(rejection.ProbeInProgress);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CIRCUIT-BREAKER-HALF-OPEN", "unclassified-failure-releases-probe")]
    public async Task FailureOutsideTheConfiguredFilter_ReleasesTheProbeWithoutClaimingRecoveryAsync()
    {
        var time = new ObservableTimeProvider(StartTime);
        var recoveryEntered = NewSignal();
        var releaseRecovery = NewSignal();
        var outcome = ProbeOutcome.Fail;
        var recoveryCallCount = 0;
        var unclassified = new UnclassifiedFailureException();
        IPipe<TestPipeContext> pipe = CreatePipe(time, async _ =>
        {
            switch (outcome)
            {
                case ProbeOutcome.Fail:
                    throw new ExpectedFailureException("initial trip");
                case ProbeOutcome.Unclassified:
                    throw unclassified;
                case ProbeOutcome.HoldRecovery:
                    if (Interlocked.Increment(ref recoveryCallCount) == 1)
                    {
                        recoveryEntered.SetResult();
                        await releaseRecovery.Task.WaitAsync(OperationTimeout, TestCancellationToken);
                    }
                    break;
            }
        }, options => options.SetExceptionFilter(filter => filter.Handle<ExpectedFailureException>()));

        await Assert.ThrowsAsync<ExpectedFailureException>(() => pipe.SendAsync(new TestPipeContext()));
        time.Advance(TimeSpan.FromSeconds(1));
        outcome = ProbeOutcome.Unclassified;
        UnclassifiedFailureException observed = await Assert.ThrowsAsync<UnclassifiedFailureException>(
            () => pipe.SendAsync(new TestPipeContext()));
        Assert.Same(unclassified, observed);

        outcome = ProbeOutcome.HoldRecovery;
        Task recovery = pipe.SendAsync(new TestPipeContext());
        try
        {
            await recoveryEntered.Task.WaitAsync(OperationTimeout, TestCancellationToken);
            CircuitBreakerOpenException competitor = await Assert.ThrowsAsync<CircuitBreakerOpenException>(
                () => pipe.SendAsync(new TestPipeContext()));
            Assert.True(competitor.ProbeInProgress);
            Assert.Equal(1, recoveryCallCount);
        }
        finally
        {
            releaseRecovery.TrySetResult();
            await recovery.WaitAsync(OperationTimeout, CancellationToken.None);
        }

        outcome = ProbeOutcome.Succeed;
        await pipe.SendAsync(new TestPipeContext());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CIRCUIT-BREAKER-HALF-OPEN", "classifier-failure-releases-probe")]
    public async Task ExceptionClassifierFailure_ReleasesTheProbeAndPropagatesTheExactFailureAsync()
    {
        var time = new ObservableTimeProvider(StartTime);
        var classifierCalls = 0;
        var protectedCalls = 0;
        var succeed = false;
        var classifierFailure = new ClassifierFailureException();
        IPipe<TestPipeContext> pipe = CreatePipe(time, _ =>
        {
            Interlocked.Increment(ref protectedCalls);
            return succeed
                ? Task.CompletedTask
                : Task.FromException(new ExpectedFailureException("resource unavailable"));
        }, options => options.SetExceptionFilter(configurator =>
            configurator.Handle<ExpectedFailureException>(_ =>
            {
                if (Interlocked.Increment(ref classifierCalls) == 1)
                    return true;

                throw classifierFailure;
            })));

        await Assert.ThrowsAsync<ExpectedFailureException>(() => pipe.SendAsync(new TestPipeContext()));
        time.Advance(TimeSpan.FromSeconds(1));
        ClassifierFailureException observed = await Assert.ThrowsAsync<ClassifierFailureException>(
            () => pipe.SendAsync(new TestPipeContext()));
        Assert.Same(classifierFailure, observed);

        succeed = true;
        await pipe.SendAsync(new TestPipeContext());
        await pipe.SendAsync(new TestPipeContext());

        Assert.Equal(4, protectedCalls);
        Assert.Equal(2, classifierCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MIDDLEWARE-COMPOSITION", "retry-breaker-concurrency-and-exclusive-recovery")]
    public async Task RetryCircuitBreakerAndConcurrencyLimit_ComposeWithoutChangingTheirOwnershipAsync()
    {
        var time = new ObservableTimeProvider(StartTime);
        var retryObserver = new CountingRetryObserver();
        var filter = new ThrowingFilter();
        IPipe<TestPipeContext> pipe = Pipe.New<TestPipeContext>(configuration =>
        {
            configuration.UseConcurrencyLimit(2);
            configuration.UseCircuitBreaker(options => options
                .SetMinimumThroughput(2)
                .SetFailureRatio(1)
                .SetBreakDuration(TimeSpan.FromSeconds(1))
                .SetTimeProvider(time));
            configuration.UseRetry(retry =>
            {
                retry.Immediate(1);
                retry.ConnectRetryObserver(retryObserver);
            });
            configuration.UseFilter(filter);
        });

        await Assert.ThrowsAsync<ExpectedFailureException>(() => pipe.SendAsync(new TestPipeContext()));
        await Assert.ThrowsAsync<ExpectedFailureException>(() => pipe.SendAsync(new TestPipeContext()));
        int attemptsAtOpen = filter.Attempts;
        await Assert.ThrowsAsync<CircuitBreakerOpenException>(() => pipe.SendAsync(new TestPipeContext()));

        Assert.Equal(4, attemptsAtOpen);
        Assert.Equal(attemptsAtOpen, filter.Attempts);
        Assert.Equal(2, retryObserver.PreRetryCount);
        Assert.Equal(2, retryObserver.RetryFaultCount);

        filter.Throw = false;
        time.Advance(TimeSpan.FromSeconds(1));
        await pipe.SendAsync(new TestPipeContext());
        int attemptsAfterRecovery = filter.Attempts;
        filter.HoldAtConcurrency(2);
        Task[] concurrent = Enumerable.Range(0, 3)
            .Select(_ => pipe.SendAsync(new TestPipeContext()))
            .ToArray();
        try
        {
            await filter.ConfiguredConcurrencyReached.WaitAsync(OperationTimeout, TestCancellationToken);

            Assert.Equal(2, filter.CurrentConcurrency);
            Assert.Equal(2, filter.MaximumConcurrency);
            Assert.Equal(attemptsAfterRecovery + 2, filter.Attempts);
            Assert.All(concurrent, task => Assert.False(task.IsCompleted));
        }
        finally
        {
            filter.ReleaseHeldCalls();
            await Task.WhenAll(concurrent).WaitAsync(OperationTimeout, CancellationToken.None);
        }

        Assert.Equal(attemptsAfterRecovery + 3, filter.Attempts);
        Assert.Equal(2, filter.MaximumConcurrency);
        Assert.Equal(0, time.TimerCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CIRCUIT-BREAKER-OBSERVABILITY", "otel-low-cardinality-signals")]
    public async Task StateProbeAndRejection_EmitOnlyLowCardinalityOpenTelemetrySignalsAsync()
    {
        var measurements = new ConcurrentQueue<MeasurementRecord>();
        var activities = new ConcurrentQueue<ActivityRecord>();
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == "ViciOne.ServiceBus"
                && instrument.Name.StartsWith("vicione.servicebus.circuit_breaker.", StringComparison.Ordinal))
                listener.EnableMeasurementEvents(instrument);
        };
        meterListener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
            measurements.Enqueue(new MeasurementRecord(instrument.Name, instrument.Meter.Version, value, CopyTags(tags))));
        meterListener.Start();
        using var activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "ViciOne.ServiceBus",
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity =>
            {
                if (activity.OperationName.StartsWith("ViciOne.ServiceBus.CircuitBreaker.", StringComparison.Ordinal))
                    activities.Enqueue(new ActivityRecord(
                        activity.OperationName,
                        activity.Source.Version,
                        activity.Kind,
                        activity.TagObjects.ToArray()));
            },
        };
        ActivitySource.AddActivityListener(activityListener);

        var time = new ObservableTimeProvider(StartTime);
        var recoveryEntered = NewSignal();
        var releaseRecovery = NewSignal();
        var fail = true;
        IPipe<TestPipeContext> pipe = CreatePipe(time, async _ =>
        {
            if (fail)
                throw new ExpectedFailureException("resource unavailable");

            recoveryEntered.TrySetResult();
            await releaseRecovery.Task.WaitAsync(OperationTimeout, TestCancellationToken);
        });

        await Assert.ThrowsAsync<ExpectedFailureException>(() => pipe.SendAsync(new TestPipeContext()));
        CircuitBreakerOpenException openRejection = await Assert.ThrowsAsync<CircuitBreakerOpenException>(
            () => pipe.SendAsync(new TestPipeContext()));
        Assert.False(openRejection.ProbeInProgress);
        fail = false;
        time.Advance(TimeSpan.FromSeconds(1));
        Task recovery = pipe.SendAsync(new TestPipeContext());
        try
        {
            await recoveryEntered.Task.WaitAsync(OperationTimeout, TestCancellationToken);
            CircuitBreakerOpenException probeRejection = await Assert.ThrowsAsync<CircuitBreakerOpenException>(
                () => pipe.SendAsync(new TestPipeContext()));
            Assert.True(probeRejection.ProbeInProgress);
        }
        finally
        {
            releaseRecovery.TrySetResult();
            await recovery.WaitAsync(OperationTimeout, CancellationToken.None);
        }

        string expectedVersion = Assert.IsType<string>(HostMetadataCache.Host.ViciOneServiceBusVersion);
        Assert.All(measurements, item => Assert.Equal(expectedVersion, item.SourceVersion));
        Assert.All(activities, item =>
        {
            Assert.Equal(expectedVersion, item.SourceVersion);
            Assert.Equal(ActivityKind.Internal, item.Kind);
        });
        Assert.Equal(
            [
                "vicione.servicebus.circuit_breaker.probes|",
                "vicione.servicebus.circuit_breaker.rejections|circuit_breaker.rejection.reason=open",
                "vicione.servicebus.circuit_breaker.rejections|circuit_breaker.rejection.reason=probe_in_progress",
                "vicione.servicebus.circuit_breaker.state_transitions|circuit_breaker.state.from=closed,circuit_breaker.state.to=open",
                "vicione.servicebus.circuit_breaker.state_transitions|circuit_breaker.state.from=half_open,circuit_breaker.state.to=closed",
                "vicione.servicebus.circuit_breaker.state_transitions|circuit_breaker.state.from=open,circuit_breaker.state.to=half_open",
            ],
            measurements.Select(item => DescribeSignal(item.Name, item.Tags)).Order(StringComparer.Ordinal));
        Assert.Equal(
            [
                "ViciOne.ServiceBus.CircuitBreaker.Probe|circuit_breaker.probe.result=acquired",
                "ViciOne.ServiceBus.CircuitBreaker.Rejected|circuit_breaker.rejection.reason=open",
                "ViciOne.ServiceBus.CircuitBreaker.Rejected|circuit_breaker.rejection.reason=probe_in_progress",
                "ViciOne.ServiceBus.CircuitBreaker.StateTransition|circuit_breaker.state.from=closed,circuit_breaker.state.to=open",
                "ViciOne.ServiceBus.CircuitBreaker.StateTransition|circuit_breaker.state.from=half_open,circuit_breaker.state.to=closed",
                "ViciOne.ServiceBus.CircuitBreaker.StateTransition|circuit_breaker.state.from=open,circuit_breaker.state.to=half_open",
            ],
            activities.Select(item => DescribeSignal(item.Name, item.Tags)).Order(StringComparer.Ordinal));
        Assert.All(measurements, item => Assert.Equal(1, item.Value));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CIRCUIT-BREAKER-OBSERVABILITY", "observer-failure-cannot-change-product-semantics")]
    public async Task ThrowingOpenTelemetryObservers_CannotChangeCircuitStateOrFailuresAsync()
    {
        using (var meterListener = new MeterListener())
        {
            meterListener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == "ViciOne.ServiceBus"
                    && instrument.Name.StartsWith("vicione.servicebus.circuit_breaker.", StringComparison.Ordinal))
                    listener.EnableMeasurementEvents(instrument);
            };
            meterListener.SetMeasurementEventCallback<long>(static (_, _, _, _) => throw new TelemetryObserverException());
            meterListener.Start();

            await AssertCircuitSemanticsSurviveTelemetryObserverAsync();
        }

        using (var activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "ViciOne.ServiceBus",
            Sample = static (ref ActivityCreationOptions<ActivityContext> options) =>
            {
                if (options.Name.StartsWith("ViciOne.ServiceBus.CircuitBreaker.", StringComparison.Ordinal))
                    throw new TelemetryObserverException();

                return ActivitySamplingResult.None;
            },
        })
        {
            ActivitySource.AddActivityListener(activityListener);
            await AssertCircuitSemanticsSurviveTelemetryObserverAsync();
        }

        using (var stopParent = new Activity("circuit stop parent").Start())
        using (var stoppingListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ServiceBusTelemetry.ActivitySourceName,
            Sample = static (ref ActivityCreationOptions<ActivityContext> options) =>
                options.Name.StartsWith("ViciOne.ServiceBus.CircuitBreaker.", StringComparison.Ordinal)
                    ? ActivitySamplingResult.AllData
                    : ActivitySamplingResult.None,
            ActivityStopped = static _ => throw new TelemetryObserverException(),
        })
        {
            ActivitySource.AddActivityListener(stoppingListener);
            try
            {
                await AssertCircuitSemanticsSurviveTelemetryObserverAsync();
                CircuitBreakerTelemetry.StateTransition("closed", "open");
                Assert.Same(stopParent, Activity.Current);
            }
            finally
            {
                Activity.Current = stopParent;
            }
        }

        using var parent = new Activity("circuit business parent").Start();
        var started = new ConcurrentQueue<string>();
        using var startingListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ServiceBusTelemetry.ActivitySourceName,
            Sample = static (ref ActivityCreationOptions<ActivityContext> options) =>
                options.Name.StartsWith("ViciOne.ServiceBus.CircuitBreaker.", StringComparison.Ordinal)
                    ? ActivitySamplingResult.AllData
                    : ActivitySamplingResult.None,
            ActivityStarted = activity =>
            {
                started.Enqueue(activity.OperationName);
                throw new TelemetryObserverException();
            },
        };
        ActivitySource.AddActivityListener(startingListener);
        var time = new ObservableTimeProvider(StartTime);
        var expected = new ExpectedFailureException("resource unavailable");
        var fail = true;
        var protectedCalls = 0;
        IPipe<TestPipeContext> pipe = CreatePipe(time, _ =>
        {
            protectedCalls++;
            Assert.Same(parent, Activity.Current);
            return fail ? Task.FromException(expected) : Task.CompletedTask;
        });

        try
        {
            ExpectedFailureException observed = await Assert.ThrowsAsync<ExpectedFailureException>(
                () => pipe.SendAsync(new TestPipeContext()));
            Assert.Same(expected, observed);
            Assert.Same(parent, Activity.Current);
            CircuitBreakerOpenException rejected = await Assert.ThrowsAsync<CircuitBreakerOpenException>(
                () => pipe.SendAsync(new TestPipeContext()));
            Assert.Same(expected, rejected.InnerException);
            Assert.Same(parent, Activity.Current);
            fail = false;
            time.Advance(TimeSpan.FromSeconds(1));
            await pipe.SendAsync(new TestPipeContext());
            Assert.Same(parent, Activity.Current);
            Assert.Equal(2, protectedCalls);
            Assert.Equal(
                [
                    "ViciOne.ServiceBus.CircuitBreaker.StateTransition",
                    "ViciOne.ServiceBus.CircuitBreaker.Rejected",
                    "ViciOne.ServiceBus.CircuitBreaker.StateTransition",
                    "ViciOne.ServiceBus.CircuitBreaker.Probe",
                    "ViciOne.ServiceBus.CircuitBreaker.StateTransition",
                ], started);
        }
        finally
        {
            Activity.Current = parent;
        }
    }

    private static IPipe<TestPipeContext> CreatePipe(
        TimeProvider timeProvider,
        Func<TestPipeContext, Task> protectedOperation,
        Action<CircuitBreakerOptions>? configure = null)
    {
        return Pipe.New<TestPipeContext>(configuration =>
        {
            configuration.UseCircuitBreaker(options =>
            {
                options
                    .SetMinimumThroughput(1)
                    .SetFailureRatio(0)
                    .SetSamplingDuration(TimeSpan.FromMinutes(1))
                    .SetBreakDuration(TimeSpan.FromSeconds(1))
                    .SetTimeProvider(timeProvider);
                configure?.Invoke(options);
            });
            configuration.UseExecuteAwaited(protectedOperation);
        });
    }

    private static KeyValuePair<string, object?>[] CopyTags(
        ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        var copy = new KeyValuePair<string, object?>[tags.Length];
        tags.CopyTo(copy);
        return copy;
    }

    private static string DescribeSignal(string name, IEnumerable<KeyValuePair<string, object?>> tags) =>
        $"{name}|{string.Join(',', tags.OrderBy(tag => tag.Key, StringComparer.Ordinal).Select(tag => $"{tag.Key}={tag.Value}"))}";

    private static async Task AssertCircuitSemanticsSurviveTelemetryObserverAsync()
    {
        var time = new ObservableTimeProvider(StartTime);
        var expected = new ExpectedFailureException("resource unavailable");
        var fail = true;
        IPipe<TestPipeContext> pipe = CreatePipe(time, _ => fail ? Task.FromException(expected) : Task.CompletedTask);

        ExpectedFailureException observed = await Assert.ThrowsAsync<ExpectedFailureException>(
            () => pipe.SendAsync(new TestPipeContext()));
        Assert.Same(expected, observed);
        CircuitBreakerOpenException rejection = await Assert.ThrowsAsync<CircuitBreakerOpenException>(
            () => pipe.SendAsync(new TestPipeContext()));
        Assert.Same(expected, rejection.InnerException);

        fail = false;
        time.Advance(TimeSpan.FromSeconds(1));
        await pipe.SendAsync(new TestPipeContext());
        await pipe.SendAsync(new TestPipeContext());
    }

    private static TaskCompletionSource NewSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static TimeSpan OperationTimeout => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private static CancellationToken TestCancellationToken => TestContext.Current.CancellationToken;

    private enum ProbeOutcome
    {
        Fail,
        Cancel,
        HoldRecovery,
        Unclassified,
        Succeed,
    }

    private sealed class TestPipeContext(CancellationToken cancellationToken = default) : BasePipeContext(cancellationToken);

    private sealed class ExpectedFailureException(string? message = null) : Exception(message);

    private sealed class UnclassifiedFailureException : Exception;

    private sealed class ClassifierFailureException : Exception;

    private sealed class TelemetryObserverException : Exception;

    private sealed class ThrowingFilter : IFilter<TestPipeContext>
    {
        private int _attempts;
        private int _currentConcurrency;
        private int _hold;
        private int _maximumConcurrency;
        private int _targetConcurrency;
        private readonly TaskCompletionSource _configuredConcurrencyReached = NewSignal();
        private readonly TaskCompletionSource _releaseHeldCalls = NewSignal();

        public int Attempts => Volatile.Read(ref _attempts);

        public int CurrentConcurrency => Volatile.Read(ref _currentConcurrency);

        public int MaximumConcurrency => Volatile.Read(ref _maximumConcurrency);

        public Task ConfiguredConcurrencyReached => _configuredConcurrencyReached.Task;

        public bool Throw { get; set; } = true;

        public void HoldAtConcurrency(int targetConcurrency)
        {
            _targetConcurrency = targetConcurrency;
            Volatile.Write(ref _hold, 1);
        }

        public void ReleaseHeldCalls() => _releaseHeldCalls.TrySetResult();

        public async Task SendAsync(TestPipeContext context, IPipe<TestPipeContext> next)
        {
            Interlocked.Increment(ref _attempts);
            if (Throw)
                throw new ExpectedFailureException("application failed");

            if (Volatile.Read(ref _hold) == 1)
            {
                int current = Interlocked.Increment(ref _currentConcurrency);
                SetMaximumConcurrency(current);
                if (current == _targetConcurrency)
                    _configuredConcurrencyReached.TrySetResult();

                try
                {
                    await _releaseHeldCalls.Task.WaitAsync(OperationTimeout, TestCancellationToken);
                }
                finally
                {
                    Interlocked.Decrement(ref _currentConcurrency);
                }
            }

            await next.SendAsync(context);
        }

        public void Probe(ProbeContext context)
        {
        }

        private void SetMaximumConcurrency(int value)
        {
            int observed = Volatile.Read(ref _maximumConcurrency);
            while (value > observed)
            {
                int previous = Interlocked.CompareExchange(ref _maximumConcurrency, value, observed);
                if (previous == observed)
                    return;
                observed = previous;
            }
        }
    }

    private sealed class ContendedTimeProvider(
        DateTimeOffset startTime,
        TimeSpan timeout,
        CancellationToken cancellationToken) : TimeProvider, IDisposable
    {
        private readonly TaskCompletionSource _allContendersArrived = NewSignal();
        private readonly FakeTimeProvider _inner = new(startTime);
        private readonly ManualResetEventSlim _release = new(initialState: false);
        private int _arrivals;
        private int _armed;
        private int _contenderCount;

        public Task AllContendersArrived => _allContendersArrived.Task;

        public override DateTimeOffset GetUtcNow() => _inner.GetUtcNow();

        public override TimeZoneInfo LocalTimeZone => _inner.LocalTimeZone;

        public override long TimestampFrequency => _inner.TimestampFrequency;

        public override long GetTimestamp()
        {
            long timestamp = _inner.GetTimestamp();
            if (Volatile.Read(ref _armed) == 0)
                return timestamp;

            if (Interlocked.Increment(ref _arrivals) == _contenderCount)
                _allContendersArrived.TrySetResult();
            if (!_release.Wait(timeout, cancellationToken))
                throw new TimeoutException("Timed out while waiting to release circuit-breaker contenders.");
            return timestamp;
        }

        public override ITimer CreateTimer(
            TimerCallback callback,
            object? state,
            TimeSpan dueTime,
            TimeSpan period) => throw new InvalidOperationException("The circuit breaker must not allocate timers.");

        public void Advance(TimeSpan elapsed) => _inner.Advance(elapsed);

        public void ArmContention(int contenderCount)
        {
            _contenderCount = contenderCount;
            Volatile.Write(ref _armed, 1);
        }

        public void ReleaseContenders()
        {
            Volatile.Write(ref _armed, 0);
            _release.Set();
        }

        public void Dispose() => _release.Dispose();
    }

    private sealed class CountingRetryObserver : IRetryObserver
    {
        private int _preRetryCount;
        private int _retryFaultCount;

        public int PreRetryCount => Volatile.Read(ref _preRetryCount);

        public int RetryFaultCount => Volatile.Read(ref _retryFaultCount);

        public Task PostCreateAsync<T>(RetryPolicyContext<T> context)
            where T : class, PipeContext => Task.CompletedTask;

        public Task PostFaultAsync<T>(RetryContext<T> context)
            where T : class, PipeContext => Task.CompletedTask;

        public Task PreRetryAsync<T>(RetryContext<T> context)
            where T : class, PipeContext
        {
            Interlocked.Increment(ref _preRetryCount);
            return Task.CompletedTask;
        }

        public Task RetryFaultAsync<T>(RetryContext<T> context)
            where T : class, PipeContext
        {
            Interlocked.Increment(ref _retryFaultCount);
            return Task.CompletedTask;
        }

        public Task RetryCompleteAsync<T>(RetryContext<T> context)
            where T : class, PipeContext => Task.CompletedTask;
    }

    private sealed record MeasurementRecord(
        string Name,
        string? SourceVersion,
        long Value,
        KeyValuePair<string, object?>[] Tags);

    private sealed record ActivityRecord(
        string Name,
        string? SourceVersion,
        ActivityKind Kind,
        KeyValuePair<string, object?>[] Tags);
}
