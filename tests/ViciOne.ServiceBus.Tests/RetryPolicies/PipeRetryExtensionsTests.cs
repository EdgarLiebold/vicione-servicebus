using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.RetryPolicies;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Testing;
using Xunit;

namespace ViciOne.ServiceBus.Tests.RetryPolicies;

public sealed class PipeRetryExtensionsTests
{
    private static readonly DateTimeOffset StartTime =
        new(2030, 1, 2, 3, 4, 5, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-HELPER", "task-overload-uses-explicit-time-provider")]
    public async Task TaskOverload_UsesTheExplicitTimeProviderAndCompletesAfterOneRetryAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        TimeSpan interval = TimeSpan.FromHours(1);
        var attempts = 0;
        IRetryPolicy policy = Retry.Interval(1, interval);

        Task operation = policy.RetryAsync(() =>
        {
            if (Interlocked.Increment(ref attempts) == 1)
                throw new RetryFailureException("transient");

            return Task.CompletedTask;
        }, timeProvider, TestContext.Current.CancellationToken);
        await timeProvider.WaitForTimerCountAsync(1)
            .WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);

        Assert.False(operation.IsCompleted);
        Assert.Equal(1, attempts);

        timeProvider.Advance(interval);
        await operation;

        Assert.Equal(2, attempts);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-HELPER", "result-overload-uses-explicit-time-provider")]
    public async Task ResultOverload_UsesTheExplicitTimeProviderAndReturnsTheSuccessfulValueAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        TimeSpan interval = TimeSpan.FromHours(1);
        var attempts = 0;
        IRetryPolicy policy = Retry.Interval(1, interval);

        Task<int> operation = policy.RetryAsync(() =>
        {
            if (Interlocked.Increment(ref attempts) == 1)
                throw new RetryFailureException("transient");

            return Task.FromResult(42);
        }, timeProvider, TestContext.Current.CancellationToken);
        await timeProvider.WaitForTimerCountAsync(1)
            .WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);

        Assert.False(operation.IsCompleted);
        timeProvider.Advance(interval);

        Assert.Equal(42, await operation);
        Assert.Equal(2, attempts);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-HELPER", "terminal-failure-is-last-attempt")]
    public async Task ExhaustedBudget_RethrowsTheExactTerminalFailureAsync()
    {
        var failures = new[]
        {
            new RetryFailureException("initial"),
            new RetryFailureException("retry-1"),
            new RetryFailureException("terminal")
        };
        var attempt = 0;

        RetryFailureException actual = await Assert.ThrowsAsync<RetryFailureException>(() =>
            Retry.Immediate(2).RetryAsync(
                () => Task.FromException(failures[attempt++]),
                TestContext.Current.CancellationToken));

        Assert.Same(failures[2], actual);
        Assert.Equal(3, attempt);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-HELPER", "cancellation-during-delay-is-preserved")]
    public async Task CancellationDuringDelay_PropagatesTheExactCancellationTokenWithoutAnotherAttemptAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        using var cancellation = new CancellationTokenSource();
        var attempts = 0;
        Task operation = Retry.Interval(1, TimeSpan.FromDays(1)).RetryAsync(() =>
        {
            Interlocked.Increment(ref attempts);
            throw new RetryFailureException("transient");
        }, timeProvider, cancellation.Token);
        await timeProvider.WaitForTimerCountAsync(1)
            .WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);

        cancellation.Cancel();

        OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
        Assert.Equal(cancellation.Token, actual.CancellationToken);
        Assert.Equal(1, attempts);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-HELPER", "pre-retry-and-terminal-callbacks-are-exact")]
    public async Task PolicyCallbacks_RunBeforeEveryRetryAndOnceForTheTerminalFailureAsync()
    {
        var trace = new List<string>();
        var policy = new TrackingRetryPolicy(trace, 1);
        var attempts = 0;

        await Assert.ThrowsAsync<RetryFailureException>(() => policy.RetryAsync(
            () =>
            {
                Interlocked.Increment(ref attempts);
                throw new RetryFailureException("failure");
            },
            TestContext.Current.CancellationToken));

        Assert.Equal(2, attempts);
        Assert.Equal(["before:1", "terminal:failure"], trace);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-HELPER", "pre-cancelled-token-prevents-invocation")]
    public async Task PreCancelledToken_PreventsTheOperationFromStartingAsync()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var invoked = false;

        OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Retry.Immediate(1).RetryAsync(() =>
            {
                invoked = true;
                return Task.CompletedTask;
            }, cancellation.Token));

        Assert.Equal(cancellation.Token, actual.CancellationToken);
        Assert.False(invoked);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-HELPER", "null-arguments-rejected")]
    public async Task Arguments_RejectANullPolicyMethodAndTimeProviderAsync()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            PipeRetryExtensions.RetryAsync(null!, () => Task.CompletedTask, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            Retry.None.RetryAsync((Func<Task>)null!, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            Retry.None.RetryAsync(() => Task.CompletedTask, null!, TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-HELPER", "unhandled-failure-not-retried")]
    public async Task UnhandledFailure_IsNotRetriedAndKeepsItsIdentityAsync()
    {
        var expected = new RetryFailureException("ignored");
        var attempts = 0;
        IRetryPolicy policy = Retry.Except<RetryFailureException>().Immediate(2);

        RetryFailureException actual = await Assert.ThrowsAsync<RetryFailureException>(() => policy.RetryAsync(
            () =>
            {
                Interlocked.Increment(ref attempts);
                throw expected;
            },
            TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
        Assert.Equal(1, attempts);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-HELPER", "null-policy-context-rejected")]
    public async Task NullPolicyContext_IsRejectedAsync()
    {
        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new NullPolicyContextPolicy().RetryAsync(
                () => Task.CompletedTask,
                TestContext.Current.CancellationToken));

        Assert.Equal("The retry policy returned a null policy context.", actual.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-HELPER", "null-retry-context-rejected")]
    public async Task NullRetryContext_IsRejectedAsync()
    {
        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new NullRetryContextPolicy().RetryAsync(
                () => Task.FromException(new RetryFailureException("failure")),
                TestContext.Current.CancellationToken));

        Assert.Equal("The retry policy returned a null retry context.", actual.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-HELPER", "null-replacement-context-rejected")]
    public async Task PolicyContextWithoutAPipeContext_IsRejectedAsync()
    {
        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new NullPipeContextPolicy().RetryAsync(
                () => Task.CompletedTask,
                TestContext.Current.CancellationToken));

        Assert.Equal("The retry policy returned a policy context without a pipe context.", actual.Message);
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private sealed class NullPolicyContextPolicy : IRetryPolicy
    {
        public void Probe(ProbeContext context)
        {
        }

        public RetryPolicyContext<T> CreatePolicyContext<T>(T context)
            where T : class, PipeContext => null!;

        public bool IsHandled(Exception exception) => true;
    }

    private sealed class NullRetryContextPolicy : IRetryPolicy
    {
        public void Probe(ProbeContext context)
        {
        }

        public RetryPolicyContext<T> CreatePolicyContext<T>(T context)
            where T : class, PipeContext => new NullRetryPolicyContext<T>(context);

        public bool IsHandled(Exception exception) => true;
    }

    private sealed class NullRetryPolicyContext<T>(T context) : RetryPolicyContext<T>
        where T : class, PipeContext
    {
        public T Context { get; } = context;

        public bool CanRetry(Exception exception, out RetryContext<T> retryContext)
        {
            retryContext = null!;
            return true;
        }

        public Task RetryFaultedAsync(Exception exception, CancellationToken cancellationToken = default) { if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask; }
        public void Cancel()
        {
        }

        public void Dispose()
        {
        }
    }

    private sealed class NullPipeContextPolicy : IRetryPolicy
    {
        public void Probe(ProbeContext context)
        {
        }

        public RetryPolicyContext<T> CreatePolicyContext<T>(T context)
            where T : class, PipeContext => new NullPipePolicyContext<T>();

        public bool IsHandled(Exception exception) => true;
    }

    private sealed class NullPipePolicyContext<T> : RetryPolicyContext<T>
        where T : class, PipeContext
    {
        public T Context => null!;

        public bool CanRetry(Exception exception, out RetryContext<T> retryContext)
        {
            retryContext = null!;
            return false;
        }

        public Task RetryFaultedAsync(Exception exception, CancellationToken cancellationToken = default) { if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask; }
        public void Cancel()
        {
        }

        public void Dispose()
        {
        }
    }

    private sealed class TrackingRetryPolicy(List<string> trace, int retryLimit) : IRetryPolicy
    {
        public void Probe(ProbeContext context)
        {
        }

        public RetryPolicyContext<T> CreatePolicyContext<T>(T context)
            where T : class, PipeContext => new TrackingPolicyContext<T>(context, trace, retryLimit);

        public bool IsHandled(Exception exception) => true;
    }

    private sealed class TrackingPolicyContext<T> : RetryPolicyContext<T>
        where T : class, PipeContext
    {
        private readonly int _retryLimit;
        private readonly List<string> _trace;

        public TrackingPolicyContext(T context, List<string> trace, int retryLimit)
        {
            Context = context;
            _trace = trace;
            _retryLimit = retryLimit;
        }

        public T Context { get; }

        public bool CanRetry(Exception exception, out RetryContext<T> retryContext)
        {
            retryContext = new TrackingRetryContext<T>(Context, exception, 0, _trace, _retryLimit);
            return _retryLimit > 0;
        }

        public Task RetryFaultedAsync(Exception exception, CancellationToken cancellationToken = default) { if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask; }
        public void Cancel()
        {
        }

        public void Dispose()
        {
        }
    }

    private sealed class TrackingRetryContext<T> : BaseRetryContext<T>, RetryContext<T>
        where T : class, PipeContext
    {
        private readonly int _retryLimit;
        private readonly List<string> _trace;

        public TrackingRetryContext(T context, Exception exception, int retryCount, List<string> trace, int retryLimit)
            : base(context, exception, retryCount, CancellationToken.None)
        {
            _trace = trace;
            _retryLimit = retryLimit;
        }

        public override Task PreRetryAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); _trace.Add($"before:{RetryAttempt}");
            return Task.CompletedTask;
        }

        public override Task RetryFaultedAsync(Exception terminalException, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); _trace.Add($"terminal:{terminalException.Message}");
            return Task.CompletedTask;
        }

        public bool CanRetry(Exception terminalException, out RetryContext<T> retryContext)
        {
            retryContext = new TrackingRetryContext<T>(Context, terminalException, RetryCount + 1, _trace, _retryLimit);
            return RetryAttempt < _retryLimit;
        }
    }

    private sealed class RetryFailureException(string message) : Exception(message);
}
