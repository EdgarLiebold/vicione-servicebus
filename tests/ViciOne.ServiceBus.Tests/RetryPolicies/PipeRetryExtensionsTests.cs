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

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-HELPER", "null-operation-task-rejected")]
    public async Task Operation_RejectsANullTaskForVoidAndResultOverloadsAsync()
    {
        InvalidOperationException missingVoidTask = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Retry.None.RetryAsync((Func<Task>)(() => null!), TestContext.Current.CancellationToken));
        InvalidOperationException missingResultTask = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Retry.None.RetryAsync((Func<Task<int>>)(() => null!), TestContext.Current.CancellationToken));

        Assert.Equal("The retry operation returned a null task.", missingVoidTask.Message);
        Assert.Equal("The retry operation returned a null task.", missingResultTask.Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-RETRY-HELPER", "log-switch-overloads-preserve-retry-and-result")]
    public async Task LogSwitchOverloads_PreserveRetryExecutionAndTheSuccessfulResultAsync(bool log)
    {
        var voidAttempts = 0;
        var resultAttempts = 0;

        await Retry.Immediate(1).RetryAsync(() =>
        {
            if (++voidAttempts == 1)
                throw new RetryFailureException("void failure");

            return Task.CompletedTask;
        }, log, TestContext.Current.CancellationToken);
        int result = await Retry.Immediate(1).RetryAsync(() =>
        {
            if (++resultAttempts == 1)
                throw new RetryFailureException("result failure");

            return Task.FromResult(42);
        }, log, TestContext.Current.CancellationToken);

        Assert.Equal(2, voidAttempts);
        Assert.Equal(2, resultAttempts);
        Assert.Equal(42, result);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-HELPER", "operation-owned-cancellation-is-not-retried")]
    public async Task OperationOwnedCancellation_IsNotRetriedAndPreservesItsIdentityAsync()
    {
        using var operationCancellation = new CancellationTokenSource();
        operationCancellation.Cancel();
        var expected = new OperationCanceledException("operation stopped", operationCancellation.Token);
        var attempts = 0;

        OperationCanceledException actual = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            Retry.Immediate(2).RetryAsync(() =>
            {
                attempts++;
                return Task.FromException<int>(expected);
            }, false, TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
        Assert.Equal(operationCancellation.Token, actual.CancellationToken);
        Assert.Equal(1, attempts);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-HELPER", "caller-cancellation-during-operation-is-normalized")]
    public async Task CallerCancellationDuringOperation_StopsWithoutRetryAndPreservesCallerTokenAsync()
    {
        using var callerCancellation = new CancellationTokenSource();
        using var operationCancellation = new CancellationTokenSource();
        operationCancellation.Cancel();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var trace = new List<string>();
        var attempts = 0;
        var decisions = 0;
        var policy = new TrackingRetryPolicy(trace, 1, onCanRetry: () => decisions++);

        Task operation = policy.RetryAsync(async () =>
        {
            attempts++;
            entered.TrySetResult();
            await release.Task;
            throw new OperationCanceledException("operation stopped", operationCancellation.Token);
        }, false, callerCancellation.Token);
        try
        {
            await entered.Task.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);
            Assert.False(operation.IsCompleted);

            callerCancellation.Cancel();
            release.TrySetResult();

            OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                operation.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken));
            Assert.Equal(callerCancellation.Token, actual.CancellationToken);
            Assert.NotEqual(operationCancellation.Token, actual.CancellationToken);
            Assert.Equal(0, decisions);
            Assert.Equal(1, attempts);
            Assert.Empty(trace);
        }
        finally
        {
            callerCancellation.Cancel();
            release.TrySetResult();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-RETRY-HELPER", "independent-cancellation-during-pre-retry-is-normalized")]
    public async Task IndependentCancellationDuringPreRetry_PreservesTheOriginatingTokenAndStopsRetriesAsync(bool cancelCaller)
    {
        using var callerCancellation = new CancellationTokenSource();
        using var policyCancellation = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var trace = new List<string>();
        var attempts = 0;
        var policy = new TrackingRetryPolicy(trace, 1,
            beforeRetry: token =>
            {
                entered.TrySetResult();
                return Task.Delay(Timeout.InfiniteTimeSpan, token);
            }, retryToken: policyCancellation.Token);

        Task operation = policy.RetryAsync(() =>
        {
            attempts++;
            throw new RetryFailureException("transient");
        }, false, callerCancellation.Token);
        try
        {
            await entered.Task.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);
            Assert.False(operation.IsCompleted);
            Assert.Equal(1, attempts);

            CancellationTokenSource cancelled = cancelCaller ? callerCancellation : policyCancellation;
            CancellationTokenSource remaining = cancelCaller ? policyCancellation : callerCancellation;
            cancelled.Cancel();

            OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                operation.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken));
            Assert.Equal(cancelled.Token, actual.CancellationToken);
            Assert.False(remaining.IsCancellationRequested);
            Assert.Equal(1, attempts);
            Assert.Equal(["before:1"], trace);
        }
        finally
        {
            callerCancellation.Cancel();
            policyCancellation.Cancel();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-HELPER", "caller-cancellation-precedes-policy-cancellation")]
    public async Task BothTokensCancelledDuringPreRetry_ReportsCallerTokenAsync()
    {
        using var callerCancellation = new CancellationTokenSource();
        using var policyCancellation = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var trace = new List<string>();
        var attempts = 0;
        CancellationToken preparationToken = default;
        var policy = new TrackingRetryPolicy(trace, 1,
            beforeRetry: token =>
            {
                preparationToken = token;
                entered.TrySetResult();
                return release.Task;
            }, retryToken: policyCancellation.Token);

        Task operation = policy.RetryAsync(() =>
        {
            attempts++;
            throw new RetryFailureException("transient");
        }, false, callerCancellation.Token);
        try
        {
            await entered.Task.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);
            Assert.False(operation.IsCompleted);
            Assert.Equal(1, attempts);

            callerCancellation.Cancel();
            policyCancellation.Cancel();
            Assert.False(operation.IsCompleted);
            release.TrySetException(new OperationCanceledException("pre-retry stopped", preparationToken));

            OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                operation.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken));
            Assert.Equal(callerCancellation.Token, actual.CancellationToken);
            Assert.Equal(1, attempts);
            Assert.Equal(["before:1"], trace);
        }
        finally
        {
            callerCancellation.Cancel();
            policyCancellation.Cancel();
            release.TrySetCanceled(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-HELPER", "asynchronous-pre-retry-callback-is-awaited")]
    public async Task PreRetryCallback_IsAwaitedBeforeTheNextOperationAsync()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var trace = new List<string>();
        var attempts = 0;
        var policy = new TrackingRetryPolicy(trace, 1, cancellationToken =>
        {
            entered.TrySetResult();
            return release.Task.WaitAsync(cancellationToken);
        });

        Task<int> operation = policy.RetryAsync(() =>
        {
            if (Interlocked.Increment(ref attempts) == 1)
                throw new RetryFailureException("initial");

            return Task.FromResult(42);
        }, false, TestContext.Current.CancellationToken);
        try
        {
            await entered.Task.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);
            Assert.False(operation.IsCompleted);
            Assert.Equal(1, attempts);
            Assert.Equal(["before:1"], trace);
        }
        finally
        {
            release.TrySetResult();
        }

        Assert.Equal(42, await operation.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken));
        Assert.Equal(2, attempts);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-HELPER", "asynchronous-terminal-callback-is-awaited")]
    public async Task TerminalCallback_IsAwaitedAndReceivesTheExactFailureAndTokenAsync()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var trace = new List<string>();
        var expected = new RetryFailureException("terminal");
        Exception? observedFailure = null;
        CancellationToken observedToken = default;
        var policy = new TrackingRetryPolicy(trace, 0, terminal: (exception, cancellationToken) =>
        {
            observedFailure = exception;
            observedToken = cancellationToken;
            entered.TrySetResult();
            return release.Task.WaitAsync(cancellationToken);
        });

        Task operation = policy.RetryAsync(() => Task.FromException(expected), false,
            TestContext.Current.CancellationToken);
        try
        {
            await entered.Task.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);
            Assert.False(operation.IsCompleted);
            Assert.Same(expected, observedFailure);
            Assert.Equal(TestContext.Current.CancellationToken, observedToken);
            Assert.Equal(["terminal:terminal"], trace);
        }
        finally
        {
            release.TrySetResult();
        }

        RetryFailureException actual = await Assert.ThrowsAsync<RetryFailureException>(() =>
            operation.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken));
        Assert.Same(expected, actual);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-RETRY-HELPER", "null-callback-tasks-are-rejected")]
    public async Task CallbackTask_RejectsANullPreRetryOrTerminalTaskAsync(bool terminal)
    {
        var trace = new List<string>();
        var policy = new TrackingRetryPolicy(trace, terminal ? 0 : 1,
            beforeRetry: terminal ? null : _ => null!,
            terminal: terminal ? (_, _) => null! : null);
        var attempts = 0;

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            policy.RetryAsync(() =>
            {
                attempts++;
                return Task.FromException(new RetryFailureException("failure"));
            }, false, TestContext.Current.CancellationToken));

        Assert.Equal(terminal
            ? "The retry context returned a null fault task."
            : "The retry context returned a null pre-retry task.", actual.Message);
        Assert.Equal(1, attempts);
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 0)]
    [InlineData(false, 1)]
    [InlineData(true, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 2)]
    [RequirementCoverage("REQ-VSB-RETRY-HELPER", "primary-failure-precedes-policy-cleanup-failure")]
    public async Task PolicyCleanupFailure_PreservesAdmissionOperationOrCancellationCauseAsync(
        bool valueResult, int failureKind)
    {
        using var operationCancellation = new CancellationTokenSource();
        operationCancellation.Cancel();
        Exception operationFailure = failureKind == 1
            ? new OperationCanceledException("operation canceled", operationCancellation.Token)
            : new RetryFailureException("operation failed");
        var cleanupFailure = new CleanupFailureException();
        var policy = new DisposalFailurePolicy(cleanupFailure, missingContext: failureKind == 2);
        var attempts = 0;

        Task execution = valueResult
            ? policy.RetryAsync(() =>
            {
                attempts++;
                return Task.FromException<int>(operationFailure);
            }, false, TestContext.Current.CancellationToken)
            : policy.RetryAsync(() =>
            {
                attempts++;
                return Task.FromException(operationFailure);
            }, false, TestContext.Current.CancellationToken);

        AggregateException actual = await Assert.ThrowsAsync<AggregateException>(() => execution);
        Assert.Equal(2, actual.InnerExceptions.Count);
        if (failureKind == 2)
            Assert.Equal("The retry policy returned a policy context without a pipe context.",
                Assert.IsType<InvalidOperationException>(actual.InnerExceptions[0]).Message);
        else
            Assert.Same(operationFailure, actual.InnerExceptions[0]);
        Assert.Same(cleanupFailure, actual.InnerExceptions[1]);
        Assert.Equal(failureKind == 2 ? 0 : 1, attempts);
        Assert.Equal(failureKind == 0 ? 1 : 0, policy.DecisionCount);
        Assert.Equal(1, policy.DisposeCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-RETRY-HELPER", "successful-operation-exposes-exact-policy-cleanup-failure")]
    public async Task SuccessfulOperation_PropagatesExactPolicyCleanupFailureAsync(bool valueResult)
    {
        var cleanupFailure = new CleanupFailureException();
        var policy = new DisposalFailurePolicy(cleanupFailure, missingContext: false);
        var attempts = 0;
        Task execution = valueResult
            ? policy.RetryAsync(() =>
            {
                attempts++;
                return Task.FromResult(42);
            }, false, TestContext.Current.CancellationToken)
            : policy.RetryAsync(() =>
            {
                attempts++;
                return Task.CompletedTask;
            }, false, TestContext.Current.CancellationToken);

        CleanupFailureException actual = await Assert.ThrowsAsync<CleanupFailureException>(() => execution);
        Assert.Same(cleanupFailure, actual);
        Assert.Equal(1, attempts);
        Assert.Equal(0, policy.DecisionCount);
        Assert.Equal(1, policy.DisposeCount);
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

        public Task RetryFaultedAsync(Exception exception, CancellationToken cancellationToken = default)
        {
            return cancellationToken.IsCancellationRequested
                ? Task.FromCanceled(cancellationToken)
                : Task.CompletedTask;
        }

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

        public Task RetryFaultedAsync(Exception exception, CancellationToken cancellationToken = default)
        {
            return cancellationToken.IsCancellationRequested
                ? Task.FromCanceled(cancellationToken)
                : Task.CompletedTask;
        }

        public void Cancel()
        {
        }

        public void Dispose()
        {
        }
    }

    private sealed class TrackingRetryPolicy(List<string> trace, int retryLimit,
        Func<CancellationToken, Task>? beforeRetry = null,
        Func<Exception, CancellationToken, Task>? terminal = null,
        CancellationToken retryToken = default,
        Action? onCanRetry = null) : IRetryPolicy
    {
        public void Probe(ProbeContext context)
        {
        }

        public RetryPolicyContext<T> CreatePolicyContext<T>(T context)
            where T : class, PipeContext => new TrackingPolicyContext<T>(context, trace, retryLimit, beforeRetry, terminal,
                retryToken, onCanRetry);

        public bool IsHandled(Exception exception) => true;
    }

    private sealed class TrackingPolicyContext<T> : RetryPolicyContext<T>
        where T : class, PipeContext
    {
        private readonly int _retryLimit;
        private readonly List<string> _trace;
        private readonly Func<CancellationToken, Task>? _beforeRetry;
        private readonly Func<Exception, CancellationToken, Task>? _terminal;
        private readonly CancellationToken _retryToken;
        private readonly Action? _onCanRetry;

        public TrackingPolicyContext(T context, List<string> trace, int retryLimit,
            Func<CancellationToken, Task>? beforeRetry, Func<Exception, CancellationToken, Task>? terminal,
            CancellationToken retryToken, Action? onCanRetry)
        {
            Context = context;
            _trace = trace;
            _retryLimit = retryLimit;
            _beforeRetry = beforeRetry;
            _terminal = terminal;
            _retryToken = retryToken;
            _onCanRetry = onCanRetry;
        }

        public T Context { get; }

        public bool CanRetry(Exception exception, out RetryContext<T> retryContext)
        {
            _onCanRetry?.Invoke();
            retryContext = new TrackingRetryContext<T>(Context, exception, 0, _trace, _retryLimit, _beforeRetry, _terminal,
                _retryToken);
            return _retryLimit > 0;
        }

        public Task RetryFaultedAsync(Exception exception, CancellationToken cancellationToken = default)
        {
            return cancellationToken.IsCancellationRequested
                ? Task.FromCanceled(cancellationToken)
                : Task.CompletedTask;
        }

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
        private readonly Func<CancellationToken, Task>? _beforeRetry;
        private readonly Func<Exception, CancellationToken, Task>? _terminal;
        private readonly CancellationToken _retryToken;

        public TrackingRetryContext(T context, Exception exception, int retryCount, List<string> trace, int retryLimit,
            Func<CancellationToken, Task>? beforeRetry, Func<Exception, CancellationToken, Task>? terminal,
            CancellationToken retryToken)
            : base(context, exception, retryCount, retryToken)
        {
            _trace = trace;
            _retryLimit = retryLimit;
            _beforeRetry = beforeRetry;
            _terminal = terminal;
            _retryToken = retryToken;
        }

        public override Task PreRetryAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled(cancellationToken);

            _trace.Add($"before:{RetryAttempt}");
            return _beforeRetry is null ? Task.CompletedTask : _beforeRetry(cancellationToken);
        }

        public override Task RetryFaultedAsync(Exception terminalException, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled(cancellationToken);

            _trace.Add($"terminal:{terminalException.Message}");
            return _terminal is null ? Task.CompletedTask : _terminal(terminalException, cancellationToken);
        }

        public bool CanRetry(Exception terminalException, out RetryContext<T> retryContext)
        {
            retryContext = new TrackingRetryContext<T>(Context, terminalException, RetryCount + 1, _trace, _retryLimit,
                _beforeRetry, _terminal, _retryToken);
            return RetryAttempt < _retryLimit;
        }
    }

    private sealed class DisposalFailurePolicy(Exception cleanupFailure, bool missingContext) : IRetryPolicy
    {
        private readonly Exception _cleanupFailure = cleanupFailure;
        private readonly bool _missingContext = missingContext;

        public int DecisionCount { get; private set; }
        public int DisposeCount { get; private set; }

        public void Probe(ProbeContext context)
        {
        }

        public RetryPolicyContext<T> CreatePolicyContext<T>(T context)
            where T : class, PipeContext => new DisposalFailureContext<T>(this, context);

        public bool IsHandled(Exception exception) => false;

        private sealed class DisposalFailureContext<T>(DisposalFailurePolicy owner, T context) : RetryPolicyContext<T>
            where T : class, PipeContext
        {
            public T Context => owner._missingContext ? null! : context;

            public bool CanRetry(Exception exception, out RetryContext<T> retryContext)
            {
                owner.DecisionCount++;
                retryContext = new TrackingRetryContext<T>(context, exception, 0, [], 0,
                    null, null, default);
                return false;
            }

            public Task RetryFaultedAsync(Exception exception, CancellationToken cancellationToken = default) =>
                Task.CompletedTask;

            public void Cancel()
            {
            }

            public void Dispose()
            {
                owner.DisposeCount++;
                throw owner._cleanupFailure;
            }
        }
    }

    private sealed class CleanupFailureException : Exception;

    private sealed class RetryFailureException(string message) : Exception(message);
}
