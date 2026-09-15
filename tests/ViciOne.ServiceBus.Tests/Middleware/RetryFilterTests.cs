using System.Diagnostics.CodeAnalysis;
using ViciOne.ServiceBus.Contracts;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.RetryPolicies;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.Retry;
using ViciOne.ServiceBus.Tests.Testing;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware;

public sealed class RetryFilterTests
{
    private static readonly DateTimeOffset StartTime =
        new(2030, 1, 2, 3, 4, 5, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-OBSERVERS", "exhausted-lifecycle-and-exact-failure")]
    public async Task ExhaustedRetry_EmitsTheExactObserverLifecycleAndRethrowsTheTerminalFailureAsync()
    {
        var observer = new RecordingRetryObserver();
        var expected = new RetryFailureException("terminal");
        var attempts = 0;
        IPipe<TestPipeContext> pipe = Pipe.New<TestPipeContext>(configuration =>
        {
            configuration.UseRetry(retry =>
            {
                retry.Immediate(4);
                retry.ConnectRetryObserver(observer);
            });
            configuration.UseExecute(_ =>
            {
                Interlocked.Increment(ref attempts);
                throw expected;
            });
        });

        RetryFailureException actual = await Assert.ThrowsAsync<RetryFailureException>(() =>
            pipe.SendAsync(new TestPipeContext()));

        Assert.Same(expected, actual);
        Assert.Equal(5, attempts);
        Assert.Equal(
        [
            "create",
            "fault:0", "before:0",
            "fault:1", "before:1",
            "fault:2", "before:2",
            "fault:3", "before:3",
            "terminal:4"
        ], observer.Events);
        Assert.Same(expected, observer.TerminalException);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-OBSERVERS", "successful-retry-lifecycle")]
    public async Task SuccessfulRetry_EmitsCompletionOnceAndNeverEmitsTerminalFaultAsync()
    {
        var observer = new RecordingRetryObserver();
        var attempts = 0;
        IPipe<TestPipeContext> pipe = Pipe.New<TestPipeContext>(configuration =>
        {
            configuration.UseRetry(retry =>
            {
                retry.Immediate(1);
                retry.ConnectRetryObserver(observer);
            });
            configuration.UseExecute(_ =>
            {
                if (Interlocked.Increment(ref attempts) == 1)
                    throw new RetryFailureException("transient");
            });
        });

        await pipe.SendAsync(new TestPipeContext());

        Assert.Equal(2, attempts);
        Assert.Equal(["create", "fault:0", "before:0", "complete:0"], observer.Events);
        Assert.Null(observer.TerminalException);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-OBSERVERS", "no-retry-terminal-lifecycle")]
    public async Task NoRetry_ReportsOneTerminalFaultWithoutRetryEventsAsync()
    {
        var observer = new RecordingRetryObserver();
        var expected = new RetryFailureException("not retried");
        var attempts = 0;
        IPipe<TestPipeContext> pipe = Pipe.New<TestPipeContext>(configuration =>
        {
            configuration.UseRetry(retry =>
            {
                retry.None();
                retry.ConnectRetryObserver(observer);
            });
            configuration.UseExecute(_ =>
            {
                Interlocked.Increment(ref attempts);
                throw expected;
            });
        });

        RetryFailureException actual = await Assert.ThrowsAsync<RetryFailureException>(() =>
            pipe.SendAsync(new TestPipeContext()));

        Assert.Same(expected, actual);
        Assert.Equal(1, attempts);
        Assert.Equal(["create", "terminal:0"], observer.Events);
        Assert.Same(expected, observer.TerminalException);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-OBSERVERS", "nested-owner-observer-receives-complete-lifecycle")]
    public async Task NestedOwningRetryObserver_ReceivesItsCompleteLifecycleAsync()
    {
        var observer = new RecordingRetryObserver();
        var attempts = 0;
        IPipe<TestPipeContext> pipe = Pipe.New<TestPipeContext>(configuration =>
        {
            configuration.UseRetry(retry => retry.None());
            configuration.UseRetry(retry =>
            {
                retry.Immediate(4);
                retry.ConnectRetryObserver(observer);
            });
            configuration.UseExecute(_ =>
            {
                Interlocked.Increment(ref attempts);
                throw new RetryFailureException("nested owner");
            });
        });

        await Assert.ThrowsAsync<RetryFailureException>(() => pipe.SendAsync(new TestPipeContext()));

        Assert.Equal(5, attempts);
        Assert.Equal(
        [
            "create",
            "fault:0", "before:0",
            "fault:1", "before:1",
            "fault:2", "before:2",
            "fault:3", "before:3",
            "terminal:4"
        ], observer.Events);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-OBSERVERS", "outer-observer-receives-nested-terminal-context")]
    public async Task OuterObserver_ReceivesTheTerminalContextOwnedByTheNestedRetryAsync()
    {
        var observer = new RecordingRetryObserver();
        var attempts = 0;
        IPipe<TestPipeContext> pipe = Pipe.New<TestPipeContext>(configuration =>
        {
            configuration.UseRetry(retry =>
            {
                retry.None();
                retry.ConnectRetryObserver(observer);
            });
            configuration.UseRetry(retry => retry.Immediate(4));
            configuration.UseExecute(_ =>
            {
                Interlocked.Increment(ref attempts);
                throw new RetryFailureException("nested terminal");
            });
        });

        await Assert.ThrowsAsync<RetryFailureException>(() => pipe.SendAsync(new TestPipeContext()));

        Assert.Equal(5, attempts);
        Assert.Equal(["create", "terminal:4"], observer.Events);
        Assert.Equal(4, Assert.IsAssignableFrom<RetryContext>(observer.TerminalContext).RetryCount);
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 5)]
    [RequirementCoverage("REQ-VSB-RETRY-COMPOSITION", "same-exception-policies-do-not-multiply")]
    public async Task NestedRetryPolicies_DoNotMultiplyTheDownstreamRetryBudgetAsync(
        bool retryingPolicyFirst,
        int expectedAttempts)
    {
        var attempts = 0;
        IPipe<TestPipeContext> pipe = Pipe.New<TestPipeContext>(configuration =>
        {
            if (retryingPolicyFirst)
            {
                configuration.UseRetry(retry => retry.Immediate(4));
                configuration.UseRetry(retry => retry.None());
            }
            else
            {
                configuration.UseRetry(retry => retry.None());
                configuration.UseRetry(retry => retry.Immediate(4));
            }

            configuration.UseExecute(_ =>
            {
                Interlocked.Increment(ref attempts);
                throw new RetryFailureException("nested");
            });
        });

        await Assert.ThrowsAsync<RetryFailureException>(() => pipe.SendAsync(new TestPipeContext()));

        Assert.Equal(expectedAttempts, attempts);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-COMPOSITION", "distinct-exceptions-have-independent-budgets")]
    public async Task DistinctNestedPolicies_KeepIndependentBudgetsWithoutRecursiveMultiplicationAsync()
    {
        var attempts = 0;
        IPipe<TestPipeContext> pipe = Pipe.New<TestPipeContext>(configuration =>
        {
            configuration.UseRetry(retry =>
            {
                retry.Handle<EvenAttemptException>();
                retry.Immediate(10);
            });
            configuration.UseRetry(retry =>
            {
                retry.Handle<OddAttemptException>();
                retry.Immediate(5);
            });
            configuration.UseExecute(_ =>
            {
                if (Interlocked.Increment(ref attempts) % 2 == 0)
                    throw new EvenAttemptException();

                throw new OddAttemptException();
            });
        });

        await Assert.ThrowsAsync<EvenAttemptException>(() => pipe.SendAsync(new TestPipeContext()));

        Assert.Equal(22, attempts);
    }

    [Theory]
    [InlineData(DispatchRetryLayout.OuterNoneInnerRetry, 5)]
    [InlineData(DispatchRetryLayout.OuterRetryThenNone, 1)]
    [InlineData(DispatchRetryLayout.OuterRetryInnerNone, 1)]
    [RequirementCoverage("REQ-VSB-RETRY-DISPATCH", "typed-dispatch-retains-single-owner")]
    public async Task RetryAcrossTypedDispatch_PreservesExactlyOneOwningBudgetAsync(
        DispatchRetryLayout layout,
        int expectedAttempts)
    {
        var attempts = 0;
        IPipe<CommandContext> pipe = Pipe.New<CommandContext>(configuration =>
        {
            if (layout == DispatchRetryLayout.OuterNoneInnerRetry)
                configuration.UseRetry(retry => retry.None());
            else
                configuration.UseRetry(retry => retry.Immediate(4));

            if (layout == DispatchRetryLayout.OuterRetryThenNone)
                configuration.UseRetry(retry => retry.None());

            configuration.UseDispatch(new PipeContextConverterFactory(), dispatch =>
            {
                dispatch.Pipe<CommandContext<SetConcurrencyLimit>>(typed =>
                {
                    if (layout == DispatchRetryLayout.OuterNoneInnerRetry)
                        typed.UseRetry(retry => retry.Immediate(4));
                    else if (layout == DispatchRetryLayout.OuterRetryInnerNone)
                        typed.UseRetry(retry => retry.None());

                    typed.UseExecute(_ =>
                    {
                        Interlocked.Increment(ref attempts);
                        throw new RetryFailureException("dispatch");
                    });
                });
            });
        });

        await Assert.ThrowsAsync<RetryFailureException>(() => pipe.SetConcurrencyLimitAsync(32, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(expectedAttempts, attempts);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-DISPATCH", "replacement-context-retains-single-owner")]
    public async Task ReplacementPolicyContext_PreservesTypedDispatchWithoutMultiplyingRetryBudgetsAsync()
    {
        var attempts = new List<int>();
        IPipe<CommandContext> pipe = Pipe.New<CommandContext>(configuration =>
        {
            configuration.UseRetry(retry => retry.SetRetryPolicy(filter =>
                new ReplacingCommandRetryPolicy(new ImmediateRetryPolicy(filter, 4))));
            configuration.UseRetry(retry => retry.SetRetryPolicy(filter =>
                new ReplacingCommandRetryPolicy(new NoRetryPolicy(filter))));
            configuration.UseDispatch(new PipeContextConverterFactory(), dispatch =>
            {
                dispatch.Pipe<CommandContext<SetConcurrencyLimit>>(typed =>
                {
                    typed.UseExecute(context =>
                    {
                        var replacement = Assert.IsType<RetryCommandContext<SetConcurrencyLimit>>(context);
                        attempts.Add(replacement.RetryAttempt);
                        throw new RetryFailureException("dispatch");
                    });
                });
            });
        });

        await Assert.ThrowsAsync<RetryFailureException>(() => pipe.SetConcurrencyLimitAsync(32, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal([0], attempts);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-RETRY-FILTERING", "ignored-direct-and-inner-exception")]
    public async Task IgnoredException_IsNotRetriedWhenDirectOrWrappedAsync(bool wrapped)
    {
        var attempts = 0;
        Exception expected = wrapped
            ? new WrappedRetryFailureException(new RetryFailureException("ignored"))
            : new RetryFailureException("ignored");
        IPipe<TestPipeContext> pipe = Pipe.New<TestPipeContext>(configuration =>
        {
            configuration.UseRetry(retry =>
            {
                retry.Ignore<RetryFailureException>();
                retry.Immediate(4);
            });
            configuration.UseExecute(_ =>
            {
                Interlocked.Increment(ref attempts);
                throw expected;
            });
        });

        Exception actual;
        if (wrapped)
            actual = await Assert.ThrowsAsync<WrappedRetryFailureException>(() => pipe.SendAsync(new TestPipeContext()));
        else
            actual = await Assert.ThrowsAsync<RetryFailureException>(() => pipe.SendAsync(new TestPipeContext()));

        Assert.Same(expected, actual);
        Assert.Equal(1, attempts);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-ITERATION", "large-budget-does-not-grow-stack")]
    public async Task LargeRetryBudget_IsExecutedIterativelyWithoutStackGrowthAsync()
    {
        const int retryCount = 20_000;
        var attempts = 0;
        IPipe<TestPipeContext> pipe = Pipe.New<TestPipeContext>(configuration =>
        {
            configuration.UseRetry(retry => retry.Interval(retryCount, TimeSpan.Zero));
            configuration.UseExecute(_ =>
            {
                Interlocked.Increment(ref attempts);
                throw new RetryFailureException("persistent");
            });
        });

        await Assert.ThrowsAsync<RetryFailureException>(() => pipe.SendAsync(new TestPipeContext()));

        Assert.Equal(retryCount + 1, attempts);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-TIME", "context-time-provider-controls-delay")]
    public async Task RetryDelay_AdvancesOnlyOnTheContextTimeProviderAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        TimeSpan interval = TimeSpan.FromHours(1);
        var attempts = 0;
        IPipe<TestPipeContext> pipe = Pipe.New<TestPipeContext>(configuration =>
        {
            configuration.UseRetry(retry => retry.Interval(1, interval));
            configuration.UseExecute(_ =>
            {
                if (Interlocked.Increment(ref attempts) == 1)
                    throw new RetryFailureException("transient");
            });
        });
        var context = new TestPipeContext();
        context.SetTimeProvider(timeProvider);

        Task send = pipe.SendAsync(context);
        await timeProvider.WaitForTimerCountAsync(1)
            .WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);
        Assert.False(send.IsCompleted);
        Assert.Equal(1, attempts);

        timeProvider.Advance(interval - TimeSpan.FromTicks(1));
        Assert.False(send.IsCompleted);
        Assert.Equal(1, attempts);

        timeProvider.Advance(TimeSpan.FromTicks(1));
        await send;

        Assert.Equal(2, attempts);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-TIME", "source-cancellation-during-delay-is-exact")]
    public async Task SourceCancellationDuringDelay_PropagatesTheExactTokenWithoutAnotherAttemptAsync()
    {
        var timeProvider = new ObservableTimeProvider(StartTime);
        using var cancellation = new CancellationTokenSource();
        var attempts = 0;
        IPipe<TestPipeContext> pipe = Pipe.New<TestPipeContext>(configuration =>
        {
            configuration.UseRetry(retry => retry.Interval(1, TimeSpan.FromDays(1)));
            configuration.UseExecute(_ =>
            {
                Interlocked.Increment(ref attempts);
                throw new RetryFailureException("transient");
            });
        });
        var context = new TestPipeContext(cancellation.Token);
        context.SetTimeProvider(timeProvider);
        Task send = pipe.SendAsync(context);
        await timeProvider.WaitForTimerCountAsync(1)
            .WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);

        cancellation.Cancel();

        OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => send);
        Assert.Equal(cancellation.Token, actual.CancellationToken);
        Assert.Equal(1, attempts);
    }

    [Theory]
    [InlineData(false, 2)]
    [InlineData(true, 3)]
    [RequirementCoverage("REQ-VSB-RETRY-CANCELLATION", "unrequested-token-is-a-dependency-failure")]
    public async Task UnrequestedOperationCanceledException_IsRetriedAsADependencyFailureAsync(
        bool cancellationOccursDuringRetry,
        int expectedAttempts)
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken testCancellationToken = TestContext.Current.CancellationToken;
        using var cancellation = new CancellationTokenSource();
        var attempts = 0;
        IPipe<TestPipeContext> pipe = Pipe.New<TestPipeContext>(configuration =>
        {
            configuration.UseRetry(retry => retry.Immediate(2));
            configuration.UseExecute(_ =>
            {
                int attempt = Interlocked.Increment(ref attempts);
                if (cancellationOccursDuringRetry && attempt == 1)
                    throw new RetryFailureException("enter retry path");

                if ((!cancellationOccursDuringRetry && attempt == 1)
                    || (cancellationOccursDuringRetry && attempt == 2))
                    throw new OperationCanceledException("dependency canceled its operation", cancellation.Token);
            });
        });

        await pipe.SendAsync(new TestPipeContext(cancellation.Token)).WaitAsync(timeout, testCancellationToken);

        Assert.False(cancellation.IsCancellationRequested);
        Assert.Equal(expectedAttempts, attempts);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-CANCELLATION", "unrequested-retry-context-token-is-a-dependency-failure")]
    public async Task UnrequestedRetryContextCancellationToken_IsRetriedAsADependencyFailureAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken testCancellationToken = TestContext.Current.CancellationToken;
        using var dependencyCancellation = new CancellationTokenSource();
        var attempts = 0;
        IFilter<TestPipeContext> filter = RetryFilterTestFactory.Create<TestPipeContext>(
            new RetryTokenPolicy(dependencyCancellation.Token, retryLimit: 2));
        IPipe<TestPipeContext> next = Pipe.Execute<TestPipeContext>(_ =>
        {
            int attempt = Interlocked.Increment(ref attempts);
            if (attempt == 1)
                throw new RetryFailureException("enter retry path");
            if (attempt == 2)
                throw new OperationCanceledException(
                    "dependency canceled its retry operation",
                    dependencyCancellation.Token);
        });

        await filter.SendAsync(new TestPipeContext(), next).WaitAsync(timeout, testCancellationToken);

        Assert.False(dependencyCancellation.IsCancellationRequested);
        Assert.Equal(3, attempts);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-CONTRACT", "policy-cancellation-never-reports-success")]
    public async Task PolicyCancellationBeforeTheRetryAttempt_IsNeverReportedAsSuccessAsync()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var attempts = 0;
        IPipe<TestPipeContext> pipe = Pipe.New<TestPipeContext>(configuration =>
        {
            configuration.UseRetry(retry => retry.SetRetryPolicy(_ =>
                new CancelledRetryContextPolicy(cancellation.Token)));
            configuration.UseExecute(_ =>
            {
                Interlocked.Increment(ref attempts);
                throw new RetryFailureException("initial");
            });
        });

        OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            pipe.SendAsync(new TestPipeContext()));

        Assert.Equal(cancellation.Token, actual.CancellationToken);
        Assert.Equal(1, attempts);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-POLICY", "policy-context-disposed-on-terminal-fault")]
    public async Task PolicyContext_IsDisposedExactlyOnceAfterTerminalFailureAsync()
    {
        var disposals = 0;
        var attempts = 0;
        IPipe<TestPipeContext> pipe = Pipe.New<TestPipeContext>(configuration =>
        {
            configuration.UseRetry(retry => retry.SetRetryPolicy(_ =>
                new TrackingRetryPolicy(1, () => Interlocked.Increment(ref disposals))));
            configuration.UseExecute(_ =>
            {
                Interlocked.Increment(ref attempts);
                throw new RetryFailureException("terminal");
            });
        });

        await Assert.ThrowsAsync<RetryFailureException>(() => pipe.SendAsync(new TestPipeContext()));

        Assert.Equal(2, attempts);
        Assert.Equal(1, disposals);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-CONTRACT", "null-collaborators-rejected")]
    public void Constructor_RejectsNullCollaborators()
    {
        Assert.Throws<ArgumentNullException>(RetryFilterTestFactory.ConstructWithoutPolicy<TestPipeContext>);
        Assert.Throws<ArgumentNullException>(RetryFilterTestFactory.ConstructWithoutObservers<TestPipeContext>);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-CONTRACT", "null-send-arguments-rejected")]
    public async Task Send_RejectsANullContextAndNullNextPipeAsync()
    {
        IFilter<TestPipeContext> filter = RetryFilterTestFactory.Create<TestPipeContext>(Retry.None);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            filter.SendAsync(null!, Pipe.Empty<TestPipeContext>()));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            filter.SendAsync(new TestPipeContext(), null!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-CONTRACT", "null-policy-context-rejected")]
    public async Task Send_RejectsANullPolicyContextAsync()
    {
        IFilter<TestPipeContext> filter = RetryFilterTestFactory.Create<TestPipeContext>(new NullPolicyContextPolicy());

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            filter.SendAsync(new TestPipeContext(), Pipe.Empty<TestPipeContext>()));

        Assert.Equal("The retry policy returned a null policy context.", actual.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-CONTRACT", "null-replacement-context-rejected")]
    public async Task Send_RejectsAPolicyContextWithoutAPipeContextAsync()
    {
        IFilter<TestPipeContext> filter = RetryFilterTestFactory.Create<TestPipeContext>(new NullPipeContextPolicy());

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            filter.SendAsync(new TestPipeContext(), Pipe.Empty<TestPipeContext>()));

        Assert.Equal("The retry policy returned a policy context without a pipe context.", actual.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-CONTRACT", "null-retry-context-rejected")]
    public async Task Send_RejectsANullRetryContextAsync()
    {
        IFilter<TestPipeContext> filter = RetryFilterTestFactory.Create<TestPipeContext>(new NullRetryContextPolicy());

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            filter.SendAsync(new TestPipeContext(), Pipe.Execute<TestPipeContext>(_ =>
                throw new RetryFailureException("failure"))));

        Assert.Equal("The retry policy returned a null retry context.", actual.Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-RETRY-OBSERVERS", "creation-observer-failure-does-not-start-business-work")]
    public async Task CreationObserverFailure_PropagatesWithoutExecutingBusinessWorkAsync(bool returnsFaultedTask)
    {
        var failure = new RetryFailureException("creation observer");
        var observer = new RecordingRetryObserver("create", failure, returnsFaultedTask);
        int attempts = 0;
        int disposals = 0;
        IFilter<TestPipeContext> filter = RetryFilterTestFactory.Create<TestPipeContext>(
            new TrackingRetryPolicy(3, () => disposals++), observer);

        RetryFailureException actual = await Assert.ThrowsAsync<RetryFailureException>(() =>
            filter.SendAsync(new TestPipeContext(), Pipe.Execute<TestPipeContext>(_ => attempts++)));

        Assert.Same(failure, actual);
        Assert.Equal(0, attempts);
        Assert.Equal(1, disposals);
        Assert.Equal(["create"], observer.Events);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-RETRY-OBSERVERS", "completion-observer-failure-does-not-replay-success")]
    public async Task CompletionObserverFailure_DoesNotReplayASuccessfulOperationAsync(bool returnsFaultedTask)
    {
        var failure = new RetryFailureException("completion observer");
        var observer = new RecordingRetryObserver("complete", failure, returnsFaultedTask);
        int attempts = 0;
        int effects = 0;
        int disposals = 0;
        IFilter<TestPipeContext> filter = RetryFilterTestFactory.Create<TestPipeContext>(
            new TrackingRetryPolicy(3, () => disposals++), observer);
        IPipe<TestPipeContext> next = Pipe.Execute<TestPipeContext>(_ =>
        {
            if (++attempts == 1)
                throw new RetryFailureException("business transient");
            effects++;
        });

        RetryFailureException actual = await Assert.ThrowsAsync<RetryFailureException>(() =>
            filter.SendAsync(new TestPipeContext(), next));

        Assert.Same(failure, actual);
        Assert.Equal(2, attempts);
        Assert.Equal(1, effects);
        Assert.Equal(1, disposals);
        Assert.Equal(["create", "fault:0", "before:0", "complete:0"], observer.Events);
        Assert.Null(observer.TerminalException);
    }

    [Theory]
    [InlineData("create", 0)]
    [InlineData("complete", 2)]
    [RequirementCoverage("REQ-VSB-RETRY-OBSERVERS", "pending-observer-failure-is-awaited-without-business-replay")]
    public async Task PendingObserverFailure_IsAwaitedWithoutStartingOrReplayingBusinessWorkAsync(
        string phase, int expectedAttempts)
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var failure = new RetryFailureException("pending observer");
        TimeSpan timeout = OperationTimeout();
        CancellationToken testCancellation = TestContext.Current.CancellationToken;
        int attempts = 0;
        int effects = 0;
        int disposals = 0;
        var observer = new RecordingRetryObserver(phase, failure, failureCallback: ObserveFailureAsync);
        IFilter<TestPipeContext> filter = RetryFilterTestFactory.Create<TestPipeContext>(
            new TrackingRetryPolicy(3, () => disposals++), observer);
        IPipe<TestPipeContext> next = Pipe.Execute<TestPipeContext>(_ =>
        {
            if (++attempts == 1)
                throw new RetryFailureException("business transient");
            effects++;
        });
        Task send = filter.SendAsync(new TestPipeContext(), next);
        try
        {
            await entered.Task.WaitAsync(timeout, testCancellation);
            Assert.False(send.IsCompleted);
            Assert.Equal(expectedAttempts, attempts);
            Assert.Equal(phase == "create" ? 0 : 1, effects);
            Assert.Equal(0, disposals);
            release.SetResult();

            RetryFailureException actual = await Assert.ThrowsAsync<RetryFailureException>(() =>
                send.WaitAsync(timeout, testCancellation));

            Assert.Same(failure, actual);
            Assert.Equal(expectedAttempts, attempts);
            Assert.Equal(phase == "create" ? 0 : 1, effects);
            Assert.Equal(1, disposals);
        }
        finally
        {
            release.TrySetResult();
            await Assert.ThrowsAsync<RetryFailureException>(() => send.WaitAsync(timeout, testCancellation));
        }

        async Task ObserveFailureAsync()
        {
            entered.TrySetResult();
            await release.Task.WaitAsync(timeout, testCancellation);
            throw failure;
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-RETRY-CANCELLATION", "pending-pre-retry-receives-source-and-policy-cancellation")]
    public async Task PendingPreRetryWork_ReceivesSourceOrPolicyCancellationWithoutAnotherOperationAsync(
        bool cancelPolicy)
    {
        using var sourceCancellation = new CancellationTokenSource();
        using var policyCancellation = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        TimeSpan timeout = OperationTimeout();
        CancellationToken testCancellation = TestContext.Current.CancellationToken;
        int attempts = 0;
        int disposals = 0;
        CancellationToken callbackToken = default;
        IFilter<TestPipeContext> filter = RetryFilterTestFactory.Create<TestPipeContext>(
            new TrackingRetryPolicy(2, () => disposals++, PreRetryAsync, policyCancellation.Token));
        Task send = filter.SendAsync(new TestPipeContext(sourceCancellation.Token),
            Pipe.Execute<TestPipeContext>(_ =>
            {
                attempts++;
                throw new RetryFailureException("business transient");
            }));
        try
        {
            await entered.Task.WaitAsync(timeout, testCancellation);
            Assert.False(send.IsCompleted);
            Assert.True(callbackToken.CanBeCanceled);
            if (cancelPolicy)
                policyCancellation.Cancel();
            else
                sourceCancellation.Cancel();

            OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                send.WaitAsync(timeout, testCancellation));

            Assert.Equal(cancelPolicy ? policyCancellation.Token : sourceCancellation.Token, actual.CancellationToken);
            Assert.Equal(1, attempts);
            Assert.Equal(1, disposals);
        }
        finally
        {
            policyCancellation.Cancel();
            release.TrySetResult();
            try
            {
                await send.WaitAsync(timeout, testCancellation);
            }
            catch (OperationCanceledException exception) when (policyCancellation.IsCancellationRequested)
            {
                Assert.Contains(exception.CancellationToken,
                    new[] { sourceCancellation.Token, policyCancellation.Token });
            }
            catch (RetryFailureException exception)
            {
                Assert.Equal("business transient", exception.Message);
            }
        }

        async Task PreRetryAsync(CancellationToken cancellationToken)
        {
            callbackToken = cancellationToken;
            entered.TrySetResult();
            await release.Task.WaitAsync(timeout, cancellationToken);
        }
    }

    private sealed class TestPipeContext(CancellationToken cancellationToken = default) : BasePipeContext(cancellationToken);

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private sealed class RecordingRetryObserver(
        string? failurePhase = null,
        Exception? failure = null,
        bool returnsFaultedTask = false,
        Func<Task>? failureCallback = null) : IRetryObserver
    {
        private readonly List<string> _events = [];

        public IReadOnlyList<string> Events => _events;

        public Exception? TerminalException { get; private set; }

        public RetryContext? TerminalContext { get; private set; }

        public Task PostCreateAsync<T>(RetryPolicyContext<T> context)
            where T : class, PipeContext
        {
            _events.Add("create");
            return NotifyAsync("create");
        }

        public Task PostFaultAsync<T>(RetryContext<T> context)
            where T : class, PipeContext
        {
            _events.Add($"fault:{context.RetryCount}");
            return NotifyAsync("fault");
        }

        public Task PreRetryAsync<T>(RetryContext<T> context)
            where T : class, PipeContext
        {
            _events.Add($"before:{context.RetryCount}");
            return NotifyAsync("before");
        }

        public Task RetryFaultAsync<T>(RetryContext<T> context)
            where T : class, PipeContext
        {
            _events.Add($"terminal:{context.RetryCount}");
            TerminalException = context.Exception;
            TerminalContext = context;
            return NotifyAsync("terminal");
        }

        public Task RetryCompleteAsync<T>(RetryContext<T> context)
            where T : class, PipeContext
        {
            _events.Add($"complete:{context.RetryCount}");
            return NotifyAsync("complete");
        }

        private Task NotifyAsync(string phase)
        {
            if (phase != failurePhase || failure == null)
                return Task.CompletedTask;
            if (failureCallback != null)
                return failureCallback();
            if (returnsFaultedTask)
                return Task.FromException(failure);
            throw failure;
        }
    }

    private sealed class TrackingRetryPolicy(int retryLimit, Action disposed,
        Func<CancellationToken, Task>? preRetry = null, CancellationToken cancellationToken = default) : IRetryPolicy
    {
        public void Probe(ProbeContext context)
        {
        }

        public RetryPolicyContext<T> CreatePolicyContext<T>(T context)
            where T : class, PipeContext => new TrackingRetryPolicyContext<T>(context, retryLimit, disposed, preRetry, cancellationToken);

        public bool IsHandled(Exception exception) => true;
    }

    private sealed class TrackingRetryPolicyContext<T>(T context, int retryLimit, Action disposed,
        Func<CancellationToken, Task>? preRetry, CancellationToken policyCancellation) :
        RetryPolicyContext<T>
        where T : class, PipeContext
    {
        public T Context { get; } = context;

        public bool CanRetry(Exception exception, out RetryContext<T> retryContext)
        {
            retryContext = new TrackingRetryContext<T>(Context, exception, 0, retryLimit, preRetry, policyCancellation);
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

        public void Dispose() => disposed();
    }

    private sealed class TrackingRetryContext<T>(T context, Exception exception, int retryCount, int retryLimit,
        Func<CancellationToken, Task>? preRetry, CancellationToken policyCancellation) :
        BaseRetryContext<T>(context, exception, retryCount, policyCancellation), RetryContext<T>
        where T : class, PipeContext
    {
        public bool CanRetry(Exception exception, out RetryContext<T> retryContext)
        {
            retryContext = new TrackingRetryContext<T>(Context, exception, RetryCount + 1, retryLimit, preRetry, CancellationToken);
            return RetryAttempt < retryLimit;
        }

        public override Task PreRetryAsync(CancellationToken cancellationToken = default) =>
            preRetry == null ? base.PreRetryAsync(cancellationToken) : preRetry(cancellationToken);
    }

    private sealed class RetryTokenPolicy(CancellationToken cancellationToken, int retryLimit) : IRetryPolicy
    {
        public void Probe(ProbeContext context)
        {
        }

        public RetryPolicyContext<T> CreatePolicyContext<T>(T context)
            where T : class, PipeContext =>
            new RetryTokenPolicyContext<T>(context, cancellationToken, retryLimit);

        public bool IsHandled(Exception exception) => true;
    }

    private sealed class RetryTokenPolicyContext<T>(T context, CancellationToken cancellationToken, int retryLimit) :
        RetryPolicyContext<T>
        where T : class, PipeContext
    {
        public T Context { get; } = context;

        public bool CanRetry(Exception exception, out RetryContext<T> retryContext)
        {
            retryContext = new RetryTokenContext<T>(Context, exception, 0, cancellationToken, retryLimit);
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

    private sealed class RetryTokenContext<T>(
        T context,
        Exception exception,
        int retryCount,
        CancellationToken cancellationToken,
        int retryLimit) :
        BaseRetryContext<T>(context, exception, retryCount, cancellationToken), RetryContext<T>
        where T : class, PipeContext
    {
        public bool CanRetry(Exception exception, out RetryContext<T> retryContext)
        {
            retryContext = new RetryTokenContext<T>(
                Context,
                exception,
                RetryCount + 1,
                CancellationToken,
                retryLimit);
            return RetryAttempt < retryLimit;
        }
    }

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

    private sealed class NullPipeContextPolicy : IRetryPolicy
    {
        public void Probe(ProbeContext context)
        {
        }

        public RetryPolicyContext<T> CreatePolicyContext<T>(T context)
            where T : class, PipeContext => new NullPipePolicyContext<T>();

        public bool IsHandled(Exception exception) => true;
    }

    private sealed class CancelledRetryContextPolicy(CancellationToken cancellationToken) : IRetryPolicy
    {
        public void Probe(ProbeContext context)
        {
        }

        public RetryPolicyContext<T> CreatePolicyContext<T>(T context)
            where T : class, PipeContext => new CancelledRetryPolicyContext<T>(context, cancellationToken);

        public bool IsHandled(Exception exception) => true;
    }

    private sealed class CancelledRetryPolicyContext<T>(T context, CancellationToken cancellationToken) : RetryPolicyContext<T>
        where T : class, PipeContext
    {
        public T Context { get; } = context;

        public bool CanRetry(Exception exception, out RetryContext<T> retryContext)
        {
            retryContext = new CancelledRetryContext<T>(Context, exception, cancellationToken);
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

    private sealed class CancelledRetryContext<T>(T context, Exception exception, CancellationToken cancellationToken) :
        BaseRetryContext<T>(context, exception, 0, cancellationToken), RetryContext<T>
        where T : class, PipeContext
    {
        public bool CanRetry(Exception exception, out RetryContext<T> retryContext)
        {
            retryContext = this;
            return false;
        }
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

    private sealed class ReplacingCommandRetryPolicy(IRetryPolicy policy) : IRetryPolicy
    {
        public void Probe(ProbeContext context) => policy.Probe(context);

        public RetryPolicyContext<T> CreatePolicyContext<T>(T context)
            where T : class, PipeContext
        {
            if (context is not CommandContext<SetConcurrencyLimit> commandContext)
                throw new ArgumentException("The context must contain a concurrency-limit command.", nameof(context));

            RetryPolicyContext<T> policyContext = policy.CreatePolicyContext(context);
            return new ReplacingRetryPolicyContext<T>(policyContext,
                retryAttempt => (T)(PipeContext)new RetryCommandContext<SetConcurrencyLimit>(commandContext, retryAttempt));
        }

        public bool IsHandled(Exception exception) => policy.IsHandled(exception);
    }

    private sealed class ReplacingRetryPolicyContext<T>(
        RetryPolicyContext<T> policyContext,
        Func<int, T> contextFactory) : RetryPolicyContext<T>
        where T : class, PipeContext
    {
        public T Context { get; } = contextFactory(0);

        public bool CanRetry(Exception exception, out RetryContext<T> retryContext)
        {
            bool canRetry = policyContext.CanRetry(exception, out RetryContext<T> innerContext);
            retryContext = new ReplacingRetryContext<T>(innerContext, contextFactory);
            return canRetry;
        }

        public Task RetryFaultedAsync(Exception exception, CancellationToken cancellationToken = default) => policyContext.RetryFaultedAsync(exception, cancellationToken: cancellationToken);

        public void Cancel() => policyContext.Cancel();

        public void Dispose() => policyContext.Dispose();
    }

    private sealed class ReplacingRetryContext<T>(
        RetryContext<T> retryContext,
        Func<int, T> contextFactory) : RetryContext<T>
        where T : class, PipeContext
    {
        public T Context { get; } = contextFactory(retryContext.RetryAttempt);

        public CancellationToken CancellationToken => retryContext.CancellationToken;

        public Exception Exception => retryContext.Exception;

        public int RetryAttempt => retryContext.RetryAttempt;

        public int RetryCount => retryContext.RetryCount;

        public TimeSpan? Delay => retryContext.Delay;

        public Type ContextType => retryContext.ContextType;

        public Task RetryFaultedAsync(Exception exception, CancellationToken cancellationToken = default) => retryContext.RetryFaultedAsync(exception, cancellationToken: cancellationToken);

        public Task PreRetryAsync(CancellationToken cancellationToken = default) => retryContext.PreRetryAsync(cancellationToken: cancellationToken);

        public bool CanRetry(Exception exception, out RetryContext<T> nextRetryContext)
        {
            bool canRetry = retryContext.CanRetry(exception, out RetryContext<T> innerContext);
            nextRetryContext = new ReplacingRetryContext<T>(innerContext, contextFactory);
            return canRetry;
        }
    }

    private sealed class RetryCommandContext<T>(CommandContext<T> context, int retryAttempt) :
        CommandContext<T>
        where T : class
    {
        public T Command => context.Command;

        public DateTimeOffset Timestamp => context.Timestamp;

        public CancellationToken CancellationToken => context.CancellationToken;

        public int RetryAttempt { get; } = retryAttempt;

        public bool HasPayloadType(Type payloadType) =>
            payloadType.IsInstanceOfType(this) || context.HasPayloadType(payloadType);

        public bool TryGetPayload<TPayload>([NotNullWhen(true)] out TPayload? payload)
            where TPayload : class
        {
            if (this is TPayload current)
            {
                payload = current;
                return true;
            }

            return context.TryGetPayload(out payload);
        }

        public TPayload GetOrAddPayload<TPayload>(PayloadFactory<TPayload> payloadFactory)
            where TPayload : class => context.GetOrAddPayload(payloadFactory);

        public TPayload AddOrUpdatePayload<TPayload>(
            PayloadFactory<TPayload> addFactory,
            UpdatePayloadFactory<TPayload> updateFactory)
            where TPayload : class => context.AddOrUpdatePayload(addFactory, updateFactory);
    }

    public enum DispatchRetryLayout
    {
        OuterNoneInnerRetry,
        OuterRetryThenNone,
        OuterRetryInnerNone
    }

    private sealed class RetryFailureException(string message) : Exception(message);

    private sealed class WrappedRetryFailureException(Exception innerException) :
        Exception("wrapper", innerException);

    private sealed class EvenAttemptException : Exception;

    private sealed class OddAttemptException : Exception;
}
