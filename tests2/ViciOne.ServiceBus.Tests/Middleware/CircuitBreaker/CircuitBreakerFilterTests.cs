using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Testing;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware.CircuitBreaker;

public sealed class CircuitBreakerFilterTests
{
    private static readonly DateTimeOffset StartTime =
        new(2030, 1, 2, 3, 4, 5, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-CIRCUIT-BREAKER-TRIP", "inclusive-throughput-and-ratio-boundary")]
    public async Task ExactMinimumThroughputAndFailureRatio_OpenTheCircuitInclusively()
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

        await pipe.Send(new TestPipeContext());
        await pipe.Send(new TestPipeContext());
        await Assert.ThrowsAsync<ExpectedFailureException>(() => pipe.Send(new TestPipeContext()));
        await Assert.ThrowsAsync<ExpectedFailureException>(() => pipe.Send(new TestPipeContext()));
        await Assert.ThrowsAsync<CircuitBreakerOpenException>(() => pipe.Send(new TestPipeContext()));

        Assert.Equal(4, protectedCallCount);
        Assert.Equal(0, time.TimerCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CIRCUIT-BREAKER-TRIP", "below-minimum-throughput-stays-closed")]
    public async Task MatchingFailuresBelowMinimumThroughput_KeepTheCircuitClosed()
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

        await Assert.ThrowsAsync<ExpectedFailureException>(() => pipe.Send(new TestPipeContext()));
        await Assert.ThrowsAsync<ExpectedFailureException>(() => pipe.Send(new TestPipeContext()));
        fail = false;
        await pipe.Send(new TestPipeContext());

        Assert.Equal(3, protectedCallCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CIRCUIT-BREAKER-TRIP", "below-failure-ratio-stays-closed")]
    public async Task MatchingFailuresBelowFailureRatio_KeepTheCircuitClosed()
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

        await pipe.Send(new TestPipeContext());
        await Assert.ThrowsAsync<ExpectedFailureException>(() => pipe.Send(new TestPipeContext()));
        await pipe.Send(new TestPipeContext());

        Assert.Equal(3, protectedCallCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CIRCUIT-BREAKER-TRIP", "lazy-sampling-window-boundary")]
    public async Task FirstAdmissionAtSamplingBoundary_StartsAFreshWindowWithoutATimer()
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

        await Assert.ThrowsAsync<ExpectedFailureException>(() => pipe.Send(new TestPipeContext()));
        time.Advance(TimeSpan.FromMinutes(1));
        await Assert.ThrowsAsync<ExpectedFailureException>(() => pipe.Send(new TestPipeContext()));
        await Assert.ThrowsAsync<ExpectedFailureException>(() => pipe.Send(new TestPipeContext()));
        await Assert.ThrowsAsync<CircuitBreakerOpenException>(() => pipe.Send(new TestPipeContext()));

        Assert.Equal(3, protectedCallCount);
        Assert.Equal(0, time.TimerCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CIRCUIT-BREAKER-OPEN", "distinct-rejection-and-exact-retry-delay")]
    public async Task OpenCircuit_RejectsBeforeTheProtectedPipeWithADistinctFailure()
    {
        var time = new ObservableTimeProvider(StartTime);
        var expected = new ExpectedFailureException("resource unavailable");
        var protectedCallCount = 0;
        IPipe<TestPipeContext> pipe = CreatePipe(time, _ =>
        {
            Interlocked.Increment(ref protectedCallCount);
            throw expected;
        });

        await Assert.ThrowsAsync<ExpectedFailureException>(() => pipe.Send(new TestPipeContext()));
        CircuitBreakerOpenException rejection = await Assert.ThrowsAsync<CircuitBreakerOpenException>(
            () => pipe.Send(new TestPipeContext()));

        Assert.Equal(1, protectedCallCount);
        Assert.Same(expected, rejection.InnerException);
        Assert.Equal(TimeSpan.FromSeconds(1), rejection.RetryAfter);
        Assert.False(rejection.ProbeInProgress);

        time.Advance(TimeSpan.FromSeconds(1) - TimeSpan.FromTicks(1));
        CircuitBreakerOpenException boundaryRejection = await Assert.ThrowsAsync<CircuitBreakerOpenException>(
            () => pipe.Send(new TestPipeContext()));
        Assert.Equal(TimeSpan.FromTicks(1), boundaryRejection.RetryAfter);
        Assert.Equal(1, protectedCallCount);

        time.Advance(TimeSpan.FromTicks(1));
        await Assert.ThrowsAsync<ExpectedFailureException>(() => pipe.Send(new TestPipeContext()));
        Assert.Equal(2, protectedCallCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CIRCUIT-BREAKER-HALF-OPEN", "exactly-one-concurrent-probe")]
    public async Task HalfOpen_AdmitsExactlyOneProbeAndRejectsEveryConcurrentCompetitor()
    {
        const int competitors = 32;
        var time = new ObservableTimeProvider(StartTime);
        var probeEntered = NewSignal();
        var releaseProbe = NewSignal();
        var protectedCallCount = 0;
        var fail = true;
        IPipe<TestPipeContext> pipe = CreatePipe(time, async _ =>
        {
            int call = Interlocked.Increment(ref protectedCallCount);
            if (fail)
                throw new ExpectedFailureException("initial trip");

            if (call == 2)
            {
                probeEntered.SetResult();
                await releaseProbe.Task;
            }
        });

        await Assert.ThrowsAsync<ExpectedFailureException>(() => pipe.Send(new TestPipeContext()));
        fail = false;
        time.Advance(TimeSpan.FromSeconds(1));
        Task probe = pipe.Send(new TestPipeContext());
        await probeEntered.Task;

        CircuitBreakerOpenException[] rejections = await Task.WhenAll(
            Enumerable.Range(0, competitors)
                .Select(_ => Assert.ThrowsAsync<CircuitBreakerOpenException>(() => pipe.Send(new TestPipeContext()))));

        Assert.All(rejections, rejection =>
        {
            Assert.True(rejection.ProbeInProgress);
            Assert.Equal(TimeSpan.Zero, rejection.RetryAfter);
        });
        Assert.Equal(2, protectedCallCount);

        releaseProbe.SetResult();
        await probe;
        await pipe.Send(new TestPipeContext());

        Assert.Equal(3, protectedCallCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CIRCUIT-BREAKER-HALF-OPEN", "classified-failure-escalates-and-success-resets")]
    public async Task ClassifiedProbeFailure_EscalatesTheBreakDurationAndSuccessResetsIt()
    {
        var time = new ObservableTimeProvider(StartTime);
        var outcome = ProbeOutcome.Fail;
        IPipe<TestPipeContext> pipe = CreatePipe(time, _ => outcome switch
        {
            ProbeOutcome.Fail => Task.FromException(new ExpectedFailureException("resource unavailable")),
            _ => Task.CompletedTask,
        }, options => options.SetBreakDurations(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3)));

        await Assert.ThrowsAsync<ExpectedFailureException>(() => pipe.Send(new TestPipeContext()));
        time.Advance(TimeSpan.FromSeconds(1));
        await Assert.ThrowsAsync<ExpectedFailureException>(() => pipe.Send(new TestPipeContext()));
        CircuitBreakerOpenException escalated = await Assert.ThrowsAsync<CircuitBreakerOpenException>(
            () => pipe.Send(new TestPipeContext()));
        Assert.Equal(TimeSpan.FromSeconds(3), escalated.RetryAfter);

        outcome = ProbeOutcome.Succeed;
        time.Advance(TimeSpan.FromSeconds(3));
        await pipe.Send(new TestPipeContext());

        outcome = ProbeOutcome.Fail;
        await Assert.ThrowsAsync<ExpectedFailureException>(() => pipe.Send(new TestPipeContext()));
        CircuitBreakerOpenException reset = await Assert.ThrowsAsync<CircuitBreakerOpenException>(
            () => pipe.Send(new TestPipeContext()));
        Assert.Equal(TimeSpan.FromSeconds(1), reset.RetryAfter);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CIRCUIT-BREAKER-HALF-OPEN", "caller-cancellation-releases-probe")]
    public async Task CallerCancellation_ReleasesTheProbeWithoutClosingOrReopening()
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
                    throw new OperationCanceledException(context.CancellationToken);
                case ProbeOutcome.HoldRecovery:
                    if (Interlocked.Increment(ref recoveryCallCount) == 1)
                    {
                        recoveryEntered.SetResult();
                        await releaseRecovery.Task;
                    }
                    break;
            }
        });

        await Assert.ThrowsAsync<ExpectedFailureException>(() => pipe.Send(new TestPipeContext()));
        time.Advance(TimeSpan.FromSeconds(1));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        outcome = ProbeOutcome.Cancel;
        OperationCanceledException canceled = await Assert.ThrowsAsync<OperationCanceledException>(
            () => pipe.Send(new TestPipeContext(cancellation.Token)));
        Assert.Equal(cancellation.Token, canceled.CancellationToken);

        outcome = ProbeOutcome.HoldRecovery;
        Task recovery = pipe.Send(new TestPipeContext());
        await recoveryEntered.Task;
        CircuitBreakerOpenException competitor = await Assert.ThrowsAsync<CircuitBreakerOpenException>(
            () => pipe.Send(new TestPipeContext()));
        Assert.True(competitor.ProbeInProgress);
        Assert.Equal(1, recoveryCallCount);

        releaseRecovery.SetResult();
        await recovery;
        outcome = ProbeOutcome.Succeed;
        await pipe.Send(new TestPipeContext());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CIRCUIT-BREAKER-HALF-OPEN", "dependency-cancellation-reopens")]
    public async Task DependencyCancellation_IsAClassifiedProbeFailureAndReopensTheCircuit()
    {
        var time = new ObservableTimeProvider(StartTime);
        Exception outcome = new ExpectedFailureException("initial trip");
        IPipe<TestPipeContext> pipe = CreatePipe(time, _ => Task.FromException(outcome));

        await Assert.ThrowsAsync<ExpectedFailureException>(() => pipe.Send(new TestPipeContext()));
        time.Advance(TimeSpan.FromSeconds(1));
        using var dependencyCancellation = new CancellationTokenSource();
        dependencyCancellation.Cancel();
        outcome = new OperationCanceledException("dependency timeout", dependencyCancellation.Token);
        await Assert.ThrowsAsync<OperationCanceledException>(() => pipe.Send(new TestPipeContext()));

        CircuitBreakerOpenException rejection = await Assert.ThrowsAsync<CircuitBreakerOpenException>(
            () => pipe.Send(new TestPipeContext()));
        Assert.Same(outcome, rejection.InnerException);
        Assert.Equal(TimeSpan.FromSeconds(1), rejection.RetryAfter);
        Assert.False(rejection.ProbeInProgress);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CIRCUIT-BREAKER-HALF-OPEN", "unclassified-failure-releases-probe")]
    public async Task FailureOutsideTheConfiguredFilter_ReleasesTheProbeWithoutClaimingRecovery()
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
                        await releaseRecovery.Task;
                    }
                    break;
            }
        }, options => options.SetExceptionFilter(filter => filter.Handle<ExpectedFailureException>()));

        await Assert.ThrowsAsync<ExpectedFailureException>(() => pipe.Send(new TestPipeContext()));
        time.Advance(TimeSpan.FromSeconds(1));
        outcome = ProbeOutcome.Unclassified;
        UnclassifiedFailureException observed = await Assert.ThrowsAsync<UnclassifiedFailureException>(
            () => pipe.Send(new TestPipeContext()));
        Assert.Same(unclassified, observed);

        outcome = ProbeOutcome.HoldRecovery;
        Task recovery = pipe.Send(new TestPipeContext());
        await recoveryEntered.Task;
        CircuitBreakerOpenException competitor = await Assert.ThrowsAsync<CircuitBreakerOpenException>(
            () => pipe.Send(new TestPipeContext()));
        Assert.True(competitor.ProbeInProgress);
        Assert.Equal(1, recoveryCallCount);

        releaseRecovery.SetResult();
        await recovery;
        outcome = ProbeOutcome.Succeed;
        await pipe.Send(new TestPipeContext());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MIDDLEWARE-COMPOSITION", "retry-breaker-concurrency-and-exclusive-recovery")]
    public async Task RetryCircuitBreakerAndConcurrencyLimit_ComposeWithoutChangingTheirOwnership()
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

        await Assert.ThrowsAsync<ExpectedFailureException>(() => pipe.Send(new TestPipeContext()));
        await Assert.ThrowsAsync<ExpectedFailureException>(() => pipe.Send(new TestPipeContext()));
        int attemptsAtOpen = filter.Attempts;
        await Assert.ThrowsAsync<CircuitBreakerOpenException>(() => pipe.Send(new TestPipeContext()));

        Assert.Equal(4, attemptsAtOpen);
        Assert.Equal(attemptsAtOpen, filter.Attempts);
        Assert.Equal(2, retryObserver.PreRetryCount);
        Assert.Equal(2, retryObserver.RetryFaultCount);

        filter.Throw = false;
        time.Advance(TimeSpan.FromSeconds(1));
        await pipe.Send(new TestPipeContext());
        await pipe.Send(new TestPipeContext());

        Assert.Equal(6, filter.Attempts);
        Assert.Equal(0, time.TimerCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CIRCUIT-BREAKER-OBSERVABILITY", "otel-low-cardinality-signals")]
    public async Task StateProbeAndRejection_EmitOnlyLowCardinalityOpenTelemetrySignals()
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
                    activities.Enqueue(new ActivityRecord(activity.OperationName, activity.Source.Version, activity.TagObjects.ToArray()));
            },
        };
        ActivitySource.AddActivityListener(activityListener);

        var time = new ObservableTimeProvider(StartTime);
        var fail = true;
        IPipe<TestPipeContext> pipe = CreatePipe(time, _ => fail
            ? Task.FromException(new ExpectedFailureException("resource unavailable"))
            : Task.CompletedTask);

        await Assert.ThrowsAsync<ExpectedFailureException>(() => pipe.Send(new TestPipeContext()));
        await Assert.ThrowsAsync<CircuitBreakerOpenException>(() => pipe.Send(new TestPipeContext()));
        fail = false;
        time.Advance(TimeSpan.FromSeconds(1));
        await pipe.Send(new TestPipeContext());

        Assert.Contains(measurements, item => item.Name.EndsWith("state_transitions", StringComparison.Ordinal) && item.Value == 1);
        Assert.Contains(measurements, item => item.Name.EndsWith("probes", StringComparison.Ordinal) && item.Value == 1);
        Assert.Contains(measurements, item => item.Name.EndsWith("rejections", StringComparison.Ordinal) && item.Value == 1);
        Assert.Contains(activities, item => item.Name.EndsWith("StateTransition", StringComparison.Ordinal));
        Assert.Contains(activities, item => item.Name.EndsWith("Probe", StringComparison.Ordinal));
        Assert.Contains(activities, item => item.Name.EndsWith("Rejected", StringComparison.Ordinal));
        Assert.All(measurements, item => Assert.False(string.IsNullOrWhiteSpace(item.SourceVersion)));
        Assert.All(activities, item => Assert.False(string.IsNullOrWhiteSpace(item.SourceVersion)));
        Assert.All(measurements.SelectMany(item => item.Tags).Concat(activities.SelectMany(item => item.Tags)), tag =>
        {
            Assert.StartsWith("circuit_breaker.", tag.Key, StringComparison.Ordinal);
            string value = Assert.IsType<string>(tag.Value);
            Assert.Contains(value,
                new[] { "closed", "open", "half_open", "probe_in_progress", "acquired" });
        });
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
            configuration.UseExecuteAsync(protectedOperation);
        });
    }

    private static KeyValuePair<string, object?>[] CopyTags(
        ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        var copy = new KeyValuePair<string, object?>[tags.Length];
        tags.CopyTo(copy);
        return copy;
    }

    private static TaskCompletionSource NewSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

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

    private sealed class ThrowingFilter : IFilter<TestPipeContext>
    {
        private int _attempts;

        public int Attempts => Volatile.Read(ref _attempts);

        public bool Throw { get; set; } = true;

        public Task Send(TestPipeContext context, IPipe<TestPipeContext> next)
        {
            Interlocked.Increment(ref _attempts);
            if (Throw)
                throw new ExpectedFailureException("application failed");

            return next.Send(context);
        }

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class CountingRetryObserver : IRetryObserver
    {
        private int _preRetryCount;
        private int _retryFaultCount;

        public int PreRetryCount => Volatile.Read(ref _preRetryCount);

        public int RetryFaultCount => Volatile.Read(ref _retryFaultCount);

        public Task PostCreate<T>(RetryPolicyContext<T> context)
            where T : class, PipeContext => Task.CompletedTask;

        public Task PostFault<T>(RetryContext<T> context)
            where T : class, PipeContext => Task.CompletedTask;

        public Task PreRetry<T>(RetryContext<T> context)
            where T : class, PipeContext
        {
            Interlocked.Increment(ref _preRetryCount);
            return Task.CompletedTask;
        }

        public Task RetryFault<T>(RetryContext<T> context)
            where T : class, PipeContext
        {
            Interlocked.Increment(ref _retryFaultCount);
            return Task.CompletedTask;
        }

        public Task RetryComplete<T>(RetryContext<T> context)
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
        KeyValuePair<string, object?>[] Tags);
}
