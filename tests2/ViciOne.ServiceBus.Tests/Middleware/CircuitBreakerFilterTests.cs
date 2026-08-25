using ViciOne.ServiceBus.Contracts;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Testing;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware;

public sealed class CircuitBreakerFilterTests
{
    private static readonly DateTimeOffset StartTime =
        new(2030, 1, 2, 3, 4, 5, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-MIDDLEWARE-COMPOSITION", "retry-breaker-concurrency-and-virtual-reset")]
    public async Task RetryCircuitBreakerAndConcurrencyLimit_ComposeAndRecoverThroughVirtualTime()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var router = new PipeRouter();
        var retryObserver = new CountingRetryObserver();
        var filter = new ThrowingFilter();
        CircuitBreakerOpened? opened = null;
        var closedCount = 0;
        var appliedLimit = 0;
        router.ConnectPipe(Pipe.ExecuteAsync<EventContext<CircuitBreakerOpened>>(async context =>
        {
            opened = context.Event;
            await router.SetConcurrencyLimit(1);
            appliedLimit = 1;
        }));
        router.ConnectPipe(Pipe.Execute<EventContext<CircuitBreakerClosed>>(_ =>
            Interlocked.Increment(ref closedCount)));
        IPipe<TestPipeContext> pipe = Pipe.New<TestPipeContext>(configuration =>
        {
            configuration.UseConcurrencyLimit(2, router);
            configuration.UseCircuitBreaker(breaker =>
            {
                breaker.ActiveThreshold = 1;
                breaker.TripThreshold = 100;
                breaker.TrackingPeriod = TimeSpan.FromMinutes(1);
                breaker.ResetInterval = TimeSpan.FromMinutes(2);
                breaker.TimeProvider = timeProvider;
                breaker.Router = router;
            });
            configuration.UseRetry(retry =>
            {
                retry.Immediate(1);
                retry.ConnectRetryObserver(retryObserver);
            });
            configuration.UseFilter(filter);
        });
        Assert.Equal(1, timeProvider.ActiveTimerCount);

        await Assert.ThrowsAsync<HandledException>(() => pipe.Send(new TestPipeContext()));
        await Assert.ThrowsAsync<HandledException>(() => pipe.Send(new TestPipeContext()));
        int attemptsWhenOpened = filter.Attempts;
        HandledException openFailure = await Assert.ThrowsAsync<HandledException>(() =>
            pipe.Send(new TestPipeContext()));

        Assert.Equal(4, attemptsWhenOpened);
        Assert.Equal(attemptsWhenOpened, filter.Attempts);
        Assert.NotNull(opened);
        Assert.Same(opened.Exception, openFailure);
        Assert.Equal(1, appliedLimit);
        Assert.Equal(2, retryObserver.PreRetryCount);
        Assert.Equal(2, retryObserver.RetryFaultCount);
        Assert.Equal(1, timeProvider.ActiveTimerCount);

        filter.Throw = false;
        timeProvider.Advance(TimeSpan.FromMinutes(2));
        Assert.Equal(0, timeProvider.ActiveTimerCount);
        await pipe.Send(new TestPipeContext());
        await pipe.Send(new TestPipeContext());

        Assert.Equal(1, closedCount);
        Assert.Equal(6, filter.Attempts);
        Assert.Equal(1, timeProvider.ActiveTimerCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CIRCUIT-BREAKER", "concurrent-transition-has-single-timer-owner")]
    public async Task CircuitBreaker_ConcurrentFailuresCreateExactlyOneOpenStateTimer()
    {
        const int concurrentCalls = 16;
        var timeProvider = new ObservableTimeProvider(StartTime);
        var allEntered = NewSignal();
        var release = NewSignal();
        var entered = 0;
        IPipe<TestPipeContext> pipe = Pipe.New<TestPipeContext>(configuration =>
        {
            configuration.UseCircuitBreaker(breaker =>
            {
                breaker.ActiveThreshold = 1;
                breaker.TripThreshold = 0;
                breaker.TrackingPeriod = TimeSpan.FromMinutes(1);
                breaker.ResetInterval = TimeSpan.FromHours(1);
                breaker.TimeProvider = timeProvider;
            });
            configuration.UseExecuteAsync(async _ =>
            {
                if (Interlocked.Increment(ref entered) == concurrentCalls)
                    allEntered.SetResult();

                await release.Task;
                throw new HandledException("concurrent failure");
            });
        });
        Task[] sends = Enumerable.Range(0, concurrentCalls)
            .Select(_ => pipe.Send(new TestPipeContext()))
            .ToArray();
        await allEntered.Task;

        release.SetResult();
        foreach (Task send in sends)
            await Assert.ThrowsAsync<HandledException>(() => send);

        Assert.Equal(concurrentCalls, entered);
        Assert.Equal(1, timeProvider.ActiveTimerCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CIRCUIT-BREAKER", "default-threshold-and-single-open-event")]
    public async Task CircuitBreaker_DefaultThresholdStopsAfterSixAttemptsAndPublishesOneExactFailure()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        var router = new PipeRouter();
        var expected = new HandledException("resource unavailable");
        CircuitBreakerOpened? opened = null;
        var openedCount = 0;
        var entered = 0;
        router.ConnectPipe(Pipe.Execute<EventContext<CircuitBreakerOpened>>(context =>
        {
            opened = context.Event;
            Interlocked.Increment(ref openedCount);
        }));
        IPipe<TestPipeContext> pipe = Pipe.New<TestPipeContext>(configuration =>
        {
            configuration.UseCircuitBreaker(breaker =>
            {
                breaker.ResetInterval = TimeSpan.FromSeconds(60);
                breaker.TimeProvider = timeProvider;
                breaker.Router = router;
            });
            configuration.UseExecute(_ =>
            {
                Interlocked.Increment(ref entered);
                throw expected;
            });
        });

        for (var attempt = 0; attempt < 100; attempt++)
        {
            HandledException actual = await Assert.ThrowsAsync<HandledException>(() =>
                pipe.Send(new TestPipeContext()));
            Assert.Same(expected, actual);
        }

        Assert.Equal(6, entered);
        Assert.Equal(1, openedCount);
        Assert.NotNull(opened);
        Assert.Same(expected, opened.Exception);
        Assert.Equal(1, timeProvider.ActiveTimerCount);
    }

    private static TaskCompletionSource NewSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed class TestPipeContext : BasePipeContext;

    private sealed class ThrowingFilter : IFilter<TestPipeContext>
    {
        private int _attempts;

        public int Attempts => Volatile.Read(ref _attempts);

        public bool Throw { get; set; } = true;

        public Task Send(TestPipeContext context, IPipe<TestPipeContext> next)
        {
            Interlocked.Increment(ref _attempts);
            if (Throw)
                throw new HandledException("application failed");

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

    private sealed class HandledException(string message) : Exception(message);
}
