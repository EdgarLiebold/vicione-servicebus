using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Payloads;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using ViciOne.ServiceBus.Tests.InternalAccess.Retry;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware;

public sealed class RetryOperationOwnershipTests
{
    [Theory]
    [InlineData(false, true, "create", 0, 0)]
    [InlineData(false, true, "fault", 1, 0)]
    [InlineData(false, true, "before", 1, 0)]
    [InlineData(false, true, "complete", 2, 1)]
    [InlineData(false, true, "terminal", 2, 0)]
    [InlineData(false, false, "create", 0, 0)]
    [InlineData(false, false, "fault", 1, 0)]
    [InlineData(false, false, "terminal", 1, 0)]
    [InlineData(true, true, "create", 0, 0)]
    [InlineData(true, true, "fault", 1, 0)]
    [InlineData(true, true, "before", 1, 0)]
    [InlineData(true, true, "complete", 2, 1)]
    [InlineData(true, true, "terminal", 2, 0)]
    [InlineData(true, false, "create", 0, 0)]
    [InlineData(true, false, "fault", 1, 0)]
    [InlineData(true, false, "terminal", 1, 0)]
    [RequirementCoverage("REQ-VSB-RETRY-OBSERVERS", "message-and-activity-redelivery-preserve-nested-lifecycle-ownership")]
    public async Task CrossFilterLifecycleFailure_DoesNotScheduleRedeliveryOrRepeatBusinessWorkAsync(
        bool activity, bool innerImmediateRetry, string phase, int expectedAttempts, int expectedEffects)
    {
        ConsumeContext<TestMessage> consume = InMemoryOutboxTestContextFactory.Create(new TestMessage(),
            cancellationToken: TestContext.Current.CancellationToken);
        if (activity)
        {
            await AssertCrossFilterFailureAsync<Advanced.ActivityContext>(new TestActivityContext(consume.Advanced()),
                RetryFilterTestFactory.CreateActivityRedelivery, innerImmediateRetry, phase,
                expectedAttempts, expectedEffects);
        }
        else
        {
            await AssertCrossFilterFailureAsync(consume, RetryFilterTestFactory.CreateRedelivery<TestMessage>,
                innerImmediateRetry, phase, expectedAttempts, expectedEffects);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-RETRY-POLICY", "cleanup-failure-does-not-replay-committed-business-work")]
    public async Task NestedPolicyCleanupFailure_PropagatesExactlyWithoutReplayingSuccessfulWorkAsync(bool afterRetry)
    {
        var failure = new OwnershipFailureException("cleanup");
        var policy = new FaultingPolicy(Advanced.Retry.Immediate(1), cleanupFailure: failure);
        var outer = new LifecycleObserver();
        int attempts = 0;
        int effects = 0;
        IPipe<TestPipeContext> pipe = CreateNestedPipe(policy, outer, new LifecycleObserver(), () =>
        {
            if (++attempts == 1 && afterRetry)
                throw new OwnershipFailureException("transient business failure");
            effects++;
        });

        OwnershipFailureException actual = await Assert.ThrowsAsync<OwnershipFailureException>(() =>
            pipe.SendAsync(new TestPipeContext()));

        Assert.Same(failure, actual);
        Assert.Equal(afterRetry ? 2 : 1, attempts);
        Assert.Equal(1, effects);
        Assert.Equal(1, policy.FactoryCalls);
        Assert.Equal(1, policy.Disposals);
        Assert.Equal(["create"], outer.Events);
    }

    [Theory]
    [InlineData("create", 0, 0)]
    [InlineData("terminal", 1, 0)]
    [InlineData("complete", 2, 1)]
    [RequirementCoverage("REQ-VSB-RETRY-POLICY", "primary-and-cleanup-failures-preserve-both-exact-identities")]
    public async Task PrimaryAndCleanupFailure_PreserveBothFailuresWithoutReplayingBusinessWorkAsync(
        string phase, int expectedAttempts, int expectedEffects)
    {
        var primary = new OwnershipFailureException("primary");
        var cleanup = new OwnershipFailureException("cleanup");
        var policy = new FaultingPolicy(phase == "terminal" ? Advanced.Retry.None : Advanced.Retry.Immediate(1),
            cleanupFailure: cleanup);
        var outer = new LifecycleObserver();
        var inner = new LifecycleObserver(phase == "terminal" ? null : phase, primary);
        int attempts = 0;
        int effects = 0;
        IPipe<TestPipeContext> pipe = CreateNestedPipe(policy, outer, inner, () =>
        {
            if (++attempts == 1)
                throw primary;
            effects++;
        });

        AggregateException actual = await Assert.ThrowsAsync<AggregateException>(() => pipe.SendAsync(new TestPipeContext()));

        Assert.Collection(actual.InnerExceptions,
            exception => Assert.Same(primary, exception), exception => Assert.Same(cleanup, exception));
        Assert.Equal(expectedAttempts, attempts);
        Assert.Equal(expectedEffects, effects);
        Assert.Equal(1, policy.FactoryCalls);
        Assert.Equal(1, policy.Disposals);
        Assert.Equal(["create"], outer.Events);
    }

    [Theory]
    [InlineData("factory", 0, 0)]
    [InlineData("initial-decision", 1, 1)]
    [InlineData("next-decision", 2, 1)]
    [InlineData("classification", 1, 1)]
    [RequirementCoverage("REQ-VSB-RETRY-CONTRACT", "policy-infrastructure-failures-do-not-enter-business-retry")]
    public async Task PolicyInfrastructureFailure_IsNotReclassifiedAsABusinessFailureAsync(
        string phase, int expectedAttempts, int expectedDisposals)
    {
        var failure = new OwnershipFailureException(phase);
        var policy = new FaultingPolicy(phase == "classification" ? Advanced.Retry.None : Advanced.Retry.Immediate(1),
            phase, failure);
        var outer = new LifecycleObserver();
        int attempts = 0;
        IPipe<TestPipeContext> pipe = CreateNestedPipe(policy, outer, new LifecycleObserver(), () =>
        {
            attempts++;
            throw new OwnershipFailureException("business failure");
        });

        OwnershipFailureException actual = await Assert.ThrowsAsync<OwnershipFailureException>(() =>
            pipe.SendAsync(new TestPipeContext()));

        Assert.Same(failure, actual);
        Assert.Equal(expectedAttempts, attempts);
        Assert.Equal(1, policy.FailureCalls);
        Assert.Equal(1, policy.FactoryCalls);
        Assert.Equal(expectedDisposals, policy.Disposals);
        Assert.Equal(["create"], outer.Events);
    }

    [Theory]
    [InlineData("null-policy", 0, 0)]
    [InlineData("null-context", 0, 1)]
    [InlineData("null-initial-allowed", 1, 1)]
    [InlineData("null-initial-denied", 1, 1)]
    [InlineData("null-next-allowed", 2, 1)]
    [InlineData("null-next-denied", 2, 1)]
    [RequirementCoverage("REQ-VSB-RETRY-CONTRACT", "invalid-policy-output-does-not-consume-an-outer-business-budget")]
    public async Task InvalidPolicyOutput_IsRejectedOnceWithoutBusinessReplayAsync(
        string phase, int expectedAttempts, int expectedDisposals)
    {
        var policy = new FaultingPolicy(Advanced.Retry.Immediate(1), phase);
        var outer = new LifecycleObserver();
        int attempts = 0;
        IPipe<TestPipeContext> pipe = CreateNestedPipe(policy, outer, new LifecycleObserver(), () =>
        {
            attempts++;
            throw new OwnershipFailureException("business failure");
        });

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            pipe.SendAsync(new TestPipeContext()));

        Assert.Equal(phase == "null-policy" ? "The retry policy returned a null policy context."
            : phase == "null-context" ? "The retry policy returned a policy context without a pipe context."
            : "The retry policy returned a null retry context.", actual.Message);
        Assert.Equal(expectedAttempts, attempts);
        Assert.Equal(1, policy.FactoryCalls);
        Assert.Equal(expectedDisposals, policy.Disposals);
        Assert.Equal(["create"], outer.Events);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-OBSERVERS", "terminal-observer-failure-does-not-poison-a-reused-context")]
    public async Task TerminalObserverFailure_DoesNotSuppressALaterBusinessRetryOnTheSameContextAsync()
    {
        var sharedFailure = new OwnershipFailureException("observer then business failure");
        var inner = new LifecycleObserver("terminal", sharedFailure, failOnce: true);
        var outer = new LifecycleObserver();
        var policy = new FaultingPolicy(Advanced.Retry.Immediate(1));
        var source = new TestPipeContext();
        bool laterOperation = false;
        int attempts = 0;
        int effects = 0;
        IPipe<TestPipeContext> pipe = CreateNestedPipe(policy, outer, inner, () =>
        {
            attempts++;
            if (!laterOperation || attempts == 1)
                throw laterOperation ? sharedFailure : new OwnershipFailureException("terminal business failure");
            effects++;
        });

        Assert.Same(sharedFailure, await Assert.ThrowsAsync<OwnershipFailureException>(() => pipe.SendAsync(source)));
        Assert.Equal(2, attempts);
        Assert.Equal(0, effects);
        Assert.Same(inner.TerminalContext, source.GetPayload<RetryContext>());

        laterOperation = true;
        attempts = 0;
        await pipe.SendAsync(source);

        Assert.Equal(2, attempts);
        Assert.Equal(1, effects);
        Assert.Equal(2, policy.FactoryCalls);
        Assert.Equal(2, policy.Disposals);
        Assert.Equal(["create", "create"], outer.Events);
        Assert.Same(inner.TerminalContext, source.GetPayload<RetryContext>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-RETRY-COMPOSITION", "terminal-diagnostics-are-current-without-replacing-caller-owned-payloads")]
    public async Task TerminalDiagnostics_TrackTheCurrentFailureWithoutReplacingCallerOwnedPayloadsAsync(bool callerOwned)
    {
        var source = new TestPipeContext();
        var callerFailure = new OwnershipFailureException("caller diagnostic");
        using RetryPolicyContext<TestPipeContext> callerPolicy = Advanced.Retry.None.CreatePolicyContext(source);
        Assert.False(callerPolicy.CanRetry(callerFailure, out RetryContext<TestPipeContext> callerDiagnostic));
        if (callerOwned)
            source.GetOrAddPayload<RetryContext>(() => callerDiagnostic);

        var observer = new LifecycleObserver();
        var policy = new FaultingPolicy(Advanced.Retry.None);
        Exception current = new OwnershipFailureException("first terminal");
        int attempts = 0;
        IPipe<TestPipeContext> pipe = Pipe.New<TestPipeContext>(configuration =>
        {
            configuration.UseFilter(RetryFilterTestFactory.Create<TestPipeContext>(policy, observer));
            configuration.UseExecute(_ =>
            {
                attempts++;
                throw current;
            });
        });

        for (int operation = 0; operation < 2; operation++)
        {
            Assert.Same(current, await Assert.ThrowsAsync<OwnershipFailureException>(() => pipe.SendAsync(source)));
            Assert.Same(current, Assert.IsAssignableFrom<RetryContext>(observer.TerminalContext).Exception);
            Assert.Same(callerOwned ? callerDiagnostic : observer.TerminalContext, source.GetPayload<RetryContext>());
            current = new OwnershipFailureException("next terminal");
        }

        Assert.Equal(2, attempts);
        Assert.Equal(2, policy.FactoryCalls);
        Assert.Equal(2, policy.Disposals);
        Assert.Equal(["create", "terminal", "create", "terminal"], observer.Events);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-COMPOSITION", "concurrent-operations-do-not-share-exception-ownership")]
    public async Task ConcurrentOperations_OnTheSameContextKeepLifecycleAndBusinessFailureOwnershipIndependentAsync()
    {
        var source = new TestPipeContext();
        var sharedFailure = new OwnershipFailureException("same instance in independent operations");
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var outer = new LifecycleObserver();
        IPipe<TestPipeContext> held = Pipe.New<TestPipeContext>(configuration =>
        {
            configuration.UseRetry(retry =>
            {
                retry.Immediate(1);
                retry.ConnectRetryObserver(outer);
            });
            configuration.UseFilter(new HoldingFailureFilter(sharedFailure, entered, release));
            configuration.UseFilter(RetryFilterTestFactory.Create<TestPipeContext>(Advanced.Retry.Immediate(1),
                new LifecycleObserver("create", sharedFailure)));
            configuration.UseExecute(_ => throw new InvalidOperationException("Business work must not start after creation fails."));
        });
        int attempts = 0;
        int effects = 0;
        IPipe<TestPipeContext> independent = Pipe.New<TestPipeContext>(configuration =>
        {
            configuration.UseRetry(retry => retry.Immediate(1));
            configuration.UseExecute(_ =>
            {
                if (++attempts == 1)
                    throw sharedFailure;
                effects++;
            });
        });

        Task pending = held.SendAsync(source);
        try
        {
            await entered.Task.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);
            Assert.False(pending.IsCompleted);
            await independent.SendAsync(source);
            Assert.Equal(2, attempts);
            Assert.Equal(1, effects);
        }
        finally
        {
            release.TrySetResult();
            Assert.Same(sharedFailure, await Assert.ThrowsAsync<OwnershipFailureException>(() =>
                pending.WaitAsync(OperationTimeout(), CancellationToken.None)));
        }

        Assert.Equal(["create"], outer.Events);
    }

    [Theory]
    [InlineData("initial")]
    [InlineData("attempt")]
    [InlineData("message")]
    [InlineData("activity")]
    [RequirementCoverage("REQ-VSB-RETRY-CONTRACT", "terminal-decision-getter-failure-remains-infrastructure-owned")]
    public async Task TerminalDecisionGetterFailure_DoesNotRestartBusinessWorkAsync(string family)
    {
        var failure = new OwnershipFailureException("terminal exception getter");
        var policy = new FaultingPolicy(family == "attempt" ? Advanced.Retry.Immediate(1) : Advanced.Retry.None,
            "decision-exception", failure);
        int expectedAttempts = family == "attempt" ? 2 : 1;
        if (family is "initial" or "attempt")
        {
            await AssertDecisionGetterFailureAsync(new TestPipeContext(TestContext.Current.CancellationToken),
                RetryFilterTestFactory.Create<TestPipeContext>, policy, failure, expectedAttempts);
        }
        else
        {
            ConsumeContext<TestMessage> source = InMemoryOutboxTestContextFactory.Create(new TestMessage(),
                cancellationToken: TestContext.Current.CancellationToken);
            if (family == "activity")
                await AssertDecisionGetterFailureAsync<Advanced.ActivityContext>(new TestActivityContext(source.Advanced()),
                    RetryFilterTestFactory.CreateActivityRedelivery, policy, failure, expectedAttempts);
            else
                await AssertDecisionGetterFailureAsync(source, RetryFilterTestFactory.CreateRedelivery<TestMessage>,
                    policy, failure, expectedAttempts);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-CONTRACT", "actual-retry-context-acquisition-is-validated-within-ownership")]
    public async Task InvalidActualRetryContext_IsRejectedWithoutConsumingAnOuterBusinessBudgetAsync()
    {
        var policy = new FaultingPolicy(Advanced.Retry.Immediate(1), "null-actual-context");
        var outer = new LifecycleObserver();
        int attempts = 0;
        IPipe<TestPipeContext> pipe = CreateNestedPipe(policy, outer, new LifecycleObserver(), () =>
        {
            attempts++;
            throw new OwnershipFailureException("business failure");
        });

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            pipe.SendAsync(new TestPipeContext(TestContext.Current.CancellationToken)));

        Assert.Equal("The retry policy returned a retry context without a pipe context.", actual.Message);
        Assert.Equal(1, attempts);
        Assert.Equal(1, policy.FactoryCalls);
        Assert.Equal(1, policy.Disposals);
        Assert.Equal(["create"], outer.Events);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-RETRY-COMPOSITION", "independent-children-under-an-active-parent-keep-failure-ownership-separate")]
    public async Task IndependentChildOperations_UnderAnActiveParentDoNotSuppressLegitimateBusinessRetryAsync(bool parallel)
    {
        var source = new TestPipeContext(TestContext.Current.CancellationToken);
        var shared = new OwnershipFailureException("caught child observer then independent child business failure");
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        IPipe<TestPipeContext> first = Pipe.New<TestPipeContext>(configuration =>
        {
            configuration.UseRetry(retry => retry.Immediate(1));
            if (parallel)
                configuration.UseFilter(new HoldingFailureFilter(shared, entered, release));
            configuration.UseFilter(RetryFilterTestFactory.Create<TestPipeContext>(Advanced.Retry.Immediate(1),
                new LifecycleObserver("create", shared)));
            configuration.UseExecute(_ => throw new InvalidOperationException("The failed child must not execute business work."));
        });
        int childAttempts = 0;
        int childEffects = 0;
        IPipe<TestPipeContext> second = Pipe.New<TestPipeContext>(configuration =>
        {
            configuration.UseRetry(retry => retry.Immediate(1));
            configuration.UseExecute(_ =>
            {
                if (++childAttempts == 1)
                    throw shared;
                childEffects++;
            });
        });
        var observer = new LifecycleObserver();
        int parentAttempts = 0;
        IPipe<TestPipeContext> parent = Pipe.New<TestPipeContext>(configuration =>
        {
            configuration.UseRetry(retry =>
            {
                retry.Immediate(1);
                retry.ConnectRetryObserver(observer);
            });
            configuration.UseExecuteAwaited(async current =>
            {
                parentAttempts++;
                Task? pending = null;
                try
                {
                    if (parallel)
                    {
                        pending = first.SendAsync(current);
                        await entered.Task.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);
                        Assert.False(pending.IsCompleted);
                    }
                    else
                        Assert.Same(shared, await Assert.ThrowsAsync<OwnershipFailureException>(() => first.SendAsync(current)));

                    await second.SendAsync(current);
                    Assert.Equal(2, childAttempts);
                    Assert.Equal(1, childEffects);
                }
                finally
                {
                    release.TrySetResult();
                    if (pending != null)
                        Assert.Same(shared, await Assert.ThrowsAsync<OwnershipFailureException>(() =>
                            pending.WaitAsync(OperationTimeout(), CancellationToken.None)));
                }
            });
        });

        await parent.SendAsync(source);

        Assert.Equal(1, parentAttempts);
        Assert.Equal(2, childAttempts);
        Assert.Equal(1, childEffects);
        Assert.Equal(["create"], observer.Events);
    }

    [Theory]
    [InlineData(false, false, "schedule")]
    [InlineData(false, true, "schedule")]
    [InlineData(true, false, "schedule")]
    [InlineData(true, true, "schedule")]
    [InlineData(false, false, "acknowledge")]
    [InlineData(false, true, "acknowledge")]
    [InlineData(true, false, "acknowledge")]
    [InlineData(true, true, "acknowledge")]
    [RequirementCoverage("REQ-VSB-RETRY-CANCELLATION", "pending-redelivery-and-acknowledgment-receive-source-and-policy-cancellation")]
    public async Task PendingRedeliveryLifecycle_ReceivesSourceOrPolicyCancellationWithoutSchedulingAgainAsync(
        bool activity, bool cancelPolicy, string stage)
    {
        using var sourceCancellation = new CancellationTokenSource();
        var policy = new FaultingPolicy(Advanced.Retry.Immediate(1));
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken supplied = default;
        int acknowledgments = 0;
        int attempts = 0;
        Task ObserveAsync(CancellationToken token)
        {
            supplied = token;
            Task work = release.Task.WaitAsync(token);
            entered.TrySetResult();
            return work;
        }
        var redelivery = new RecordingRedelivery(stage == "schedule" ? ObserveAsync : null);
        Task AcknowledgeAsync(CancellationToken token)
        {
            acknowledgments++;
            return stage == "acknowledge" ? ObserveAsync(token) : Task.CompletedTask;
        }
        ConsumeContext<TestMessage> consume = InMemoryOutboxTestContextFactory.Create(new TestMessage(),
            cancellationToken: sourceCancellation.Token, receiveElapsedTime: TimeSpan.FromSeconds(3));
        consume.GetOrAddPayload<MessageRedeliveryContext>(() => redelivery);
        Task SendAsync<T>(T context, IFilter<T> filter) where T : class, PipeContext => Pipe.New<T>(configuration =>
        {
            configuration.UseFilter(filter);
            configuration.UseExecute(_ =>
            {
                attempts++;
                throw new OwnershipFailureException("business failure");
            });
        }).SendAsync(context);
        Task operation = activity
            ? SendAsync<Advanced.ActivityContext>(new TestActivityContext(consume.Advanced(), AcknowledgeAsync),
                RetryFilterTestFactory.CreateActivityRedelivery(policy))
            : SendAsync<ConsumeContext<TestMessage>>(new AcknowledgingConsumeContext(consume, AcknowledgeAsync),
                RetryFilterTestFactory.CreateRedelivery<TestMessage>(policy));
        try
        {
            await entered.Task.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);
            Assert.False(operation.IsCompleted);
            CancellationToken expected = cancelPolicy ? policy.LastDecisionToken : sourceCancellation.Token;
            if (cancelPolicy)
                policy.CancelActiveContext();
            else
                sourceCancellation.Cancel();

            Assert.True(supplied.IsCancellationRequested);
            OperationCanceledException actual = await Assert.ThrowsAsync<OperationCanceledException>(() =>
                operation.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken));
            Assert.Equal(expected, actual.CancellationToken);
            Assert.Equal(1, attempts);
            Assert.Equal(1, redelivery.Scheduled);
            Assert.Equal(stage == "schedule" ? 0 : 1, acknowledgments);
            Assert.Equal(TimeSpan.Zero, redelivery.LastDelay);
            Assert.Equal(1, policy.FactoryCalls);
            Assert.Equal(1, policy.Disposals);
        }
        finally
        {
            sourceCancellation.Cancel();
            release.TrySetResult();
            await DrainAsync(operation);
        }
    }

    [Theory]
    [InlineData("initial", false)]
    [InlineData("initial", true)]
    [InlineData("attempt", false)]
    [InlineData("attempt", true)]
    [InlineData("nested", false)]
    [InlineData("nested", true)]
    [InlineData("message", false)]
    [InlineData("message", true)]
    [InlineData("activity", false)]
    [InlineData("activity", true)]
    [RequirementCoverage("REQ-VSB-RETRY-CANCELLATION", "terminal-and-nested-direct-fault-callbacks-receive-operation-cancellation")]
    public async Task PendingDirectFaultCallback_ReceivesSourceOrSelectedPolicyCancellationAsync(string family, bool cancelPolicy)
    {
        using var sourceCancellation = new CancellationTokenSource();
        using var nestedDecisionCancellation = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken supplied = default;
        int callbacks = 0;
        int attempts = 0;
        Task FaultAsync(CancellationToken token)
        {
            callbacks++;
            supplied = token;
            Task work = release.Task.WaitAsync(token);
            entered.TrySetResult();
            return work;
        }
        var policy = new FaultingPolicy(family == "attempt" ? Advanced.Retry.Immediate(1) : Advanced.Retry.None)
        {
            FaultCallback = FaultAsync
        };
        var nestedPolicy = new FaultingPolicy(Advanced.Retry.Immediate(1))
        {
            DecisionCancellationToken = nestedDecisionCancellation.Token
        };
        Task SendAsync<T>(T context, IFilter<T> filter) where T : class, PipeContext => Pipe.New<T>(configuration =>
        {
            configuration.UseFilter(filter);
            if (family == "nested")
                configuration.UseFilter(RetryFilterTestFactory.Create<T>(nestedPolicy));
            configuration.UseExecute(_ =>
            {
                attempts++;
                throw new OwnershipFailureException("terminal business failure");
            });
        }).SendAsync(context);
        Task operation;
        if (family is "message" or "activity")
        {
            ConsumeContext<TestMessage> consume = InMemoryOutboxTestContextFactory.Create(new TestMessage(),
                cancellationToken: sourceCancellation.Token);
            operation = family == "message"
                ? SendAsync(consume, RetryFilterTestFactory.CreateRedelivery<TestMessage>(policy))
                : SendAsync<Advanced.ActivityContext>(new TestActivityContext(consume.Advanced()),
                    RetryFilterTestFactory.CreateActivityRedelivery(policy));
        }
        else
            operation = SendAsync(new TestPipeContext(sourceCancellation.Token), RetryFilterTestFactory.Create<TestPipeContext>(policy));
        try
        {
            await entered.Task.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken);
            Assert.False(operation.IsCompleted);
            FaultingPolicy cancellationOwner = family == "nested" ? nestedPolicy : policy;
            CancellationToken expected = cancelPolicy ? cancellationOwner.LastDecisionToken : sourceCancellation.Token;
            if (cancelPolicy && family == "nested")
                nestedDecisionCancellation.Cancel();
            else if (cancelPolicy)
                cancellationOwner.CancelActiveContext();
            else
                sourceCancellation.Cancel();

            Assert.True(supplied.IsCancellationRequested);
            OperationCanceledException actual = await Assert.ThrowsAsync<OperationCanceledException>(() =>
                operation.WaitAsync(OperationTimeout(), TestContext.Current.CancellationToken));
            Assert.Equal(expected, actual.CancellationToken);
            Assert.Equal(family is "attempt" or "nested" ? 2 : 1, attempts);
            Assert.Equal(1, callbacks);
            Assert.Equal(1, policy.FactoryCalls);
            Assert.Equal(1, policy.Disposals);
            if (family == "nested")
            {
                Assert.Equal(1, nestedPolicy.FactoryCalls);
                Assert.Equal(1, nestedPolicy.Disposals);
            }
        }
        finally
        {
            sourceCancellation.Cancel();
            release.TrySetResult();
            await DrainAsync(operation);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-CONTRACT", "consume-boundary-elapsed-metadata-is-explicit-and-valid")]
    public void ConsumeBoundaryElapsedTime_IsExactAndRejectsNegativeValues()
    {
        ConsumeContext<TestMessage> context = InMemoryOutboxTestContextFactory.Create(new TestMessage(),
            cancellationToken: TestContext.Current.CancellationToken, receiveElapsedTime: TimeSpan.FromTicks(7));
        Assert.Equal(TimeSpan.FromTicks(7), context.Advanced().ReceiveContext.ElapsedTime);
        Assert.Equal("receiveElapsedTime", Assert.Throws<ArgumentOutOfRangeException>(() =>
            InMemoryOutboxTestContextFactory.Create(new TestMessage(), cancellationToken: TestContext.Current.CancellationToken,
                receiveElapsedTime: TimeSpan.FromTicks(-1))).ParamName);
    }

    [Theory]
    [InlineData(false, "success")]
    [InlineData(true, "success")]
    [InlineData(false, "business")]
    [InlineData(true, "business")]
    [InlineData(false, "observer")]
    [InlineData(true, "observer")]
    [InlineData(false, "factory")]
    [InlineData(true, "factory")]
    [InlineData(false, "cleanup")]
    [InlineData(true, "cleanup")]
    [InlineData(false, "compound")]
    [InlineData(true, "compound")]
    [RequirementCoverage("REQ-VSB-RETRY-POLICY", "operation-ownership-storage-is-released-after-success-and-every-failure-boundary")]
    public async Task CompletedOperation_ReleasesAllOwnershipEntriesAsync(bool nested, string boundary)
    {
        var context = new TestPipeContext(TestContext.Current.CancellationToken);
        var primary = new OwnershipFailureException("primary operation failure");
        var cleanup = new OwnershipFailureException("policy cleanup failure");
        var policy = new FaultingPolicy(Advanced.Retry.None, boundary == "factory" ? "factory" : null,
            primary, boundary is "cleanup" or "compound" ? cleanup : null);
        var outerPolicy = new FaultingPolicy(Advanced.Retry.None);
        var observer = new LifecycleObserver(boundary is "observer" or "compound" ? "create" : null, primary);
        int attempts = 0;
        IPipe<TestPipeContext> pipe = Pipe.New<TestPipeContext>(configuration =>
        {
            if (nested)
                configuration.UseFilter(RetryFilterTestFactory.Create<TestPipeContext>(outerPolicy));
            configuration.UseFilter(RetryFilterTestFactory.Create<TestPipeContext>(policy, observer));
            configuration.UseExecute(current =>
            {
                attempts++;
                Assert.Equal(nested ? 2 : 1, RetryFilterTestFactory.GetRetainedOperationCount(current));
                if (boundary == "business")
                    throw primary;
            });
        });

        Assert.Equal(0, RetryFilterTestFactory.GetRetainedOperationCount(context));
        Exception? actual = await Record.ExceptionAsync(() => pipe.SendAsync(context));

        if (boundary == "success")
            Assert.Null(actual);
        else if (boundary == "compound")
            Assert.Collection(Assert.IsType<AggregateException>(actual).InnerExceptions,
                failure => Assert.Same(primary, failure), failure => Assert.Same(cleanup, failure));
        else
            Assert.Same(boundary == "cleanup" ? cleanup : primary, actual);
        Assert.Equal(boundary is "success" or "business" or "cleanup" ? 1 : 0, attempts);
        Assert.Equal(1, policy.FactoryCalls);
        Assert.Equal(boundary == "factory" ? 0 : 1, policy.Disposals);
        Assert.Equal(nested ? 1 : 0, outerPolicy.FactoryCalls);
        Assert.Equal(nested ? 1 : 0, outerPolicy.Disposals);
        Assert.Equal(0, RetryFilterTestFactory.GetRetainedOperationCount(context));
    }

    [Theory]
    [InlineData(false, "success")]
    [InlineData(true, "success")]
    [InlineData(false, "schedule-failure")]
    [InlineData(true, "schedule-failure")]
    [InlineData(false, "acknowledgment-failure")]
    [InlineData(true, "acknowledgment-failure")]
    [InlineData(false, "null-acknowledgment")]
    [InlineData(true, "null-acknowledgment")]
    [InlineData(false, "foreign-cancellation")]
    [InlineData(true, "foreign-cancellation")]
    [InlineData(false, "after-schedule-cancellation")]
    [InlineData(true, "after-schedule-cancellation")]
    [RequirementCoverage("REQ-VSB-RETRY-CONTRACT", "redelivery-success-and-stage-failures-preserve-acknowledgment-and-exact-failure-ownership")]
    public async Task RedeliveryStages_PreserveSuccessfulSchedulingAndDistinctFailureSemanticsAsync(bool activity, string outcome)
    {
        using var sourceCancellation = new CancellationTokenSource();
        using var foreignCancellation = new CancellationTokenSource();
        foreignCancellation.Cancel();
        var business = new OwnershipFailureException("original business failure");
        Exception failure = outcome == "foreign-cancellation"
            ? new OperationCanceledException(foreignCancellation.Token) : new OwnershipFailureException("delivery stage failure");
        int acknowledgments = 0;
        int attempts = 0;
        CancellationToken acknowledgmentToken = default;
        var redelivery = new RecordingRedelivery(_ =>
        {
            if (outcome is "schedule-failure" or "foreign-cancellation")
                throw failure;
            if (outcome == "after-schedule-cancellation")
                sourceCancellation.Cancel();
            return Task.CompletedTask;
        });
        Task AcknowledgeAsync(CancellationToken token)
        {
            acknowledgments++;
            acknowledgmentToken = token;
            return outcome switch
            {
                "acknowledgment-failure" => throw failure,
                "null-acknowledgment" => null!,
                _ => Task.CompletedTask
            };
        }
        var policy = new FaultingPolicy(Advanced.Retry.Immediate(1));
        var outer = new FaultingPolicy(Advanced.Retry.Immediate(1));
        var observers = new LifecycleObserver();
        var outerObservers = new LifecycleObserver();
        ConsumeContext<TestMessage> consume = InMemoryOutboxTestContextFactory.Create(new TestMessage(),
            cancellationToken: sourceCancellation.Token, receiveElapsedTime: TimeSpan.FromSeconds(2));
        consume.GetOrAddPayload<MessageRedeliveryContext>(() => redelivery);
        Task SendAsync<T>(T context, IFilter<T> filter) where T : class, PipeContext => Pipe.New<T>(configuration =>
        {
            configuration.UseFilter(RetryFilterTestFactory.Create<T>(outer, outerObservers));
            configuration.UseFilter(filter);
            configuration.UseExecute(_ =>
            {
                attempts++;
                throw business;
            });
        }).SendAsync(context);
        Task operation = activity
            ? SendAsync<Advanced.ActivityContext>(new TestActivityContext(consume.Advanced(), AcknowledgeAsync),
                RetryFilterTestFactory.CreateActivityRedelivery(policy, observers))
            : SendAsync<ConsumeContext<TestMessage>>(new AcknowledgingConsumeContext(consume, AcknowledgeAsync),
                RetryFilterTestFactory.CreateRedelivery<TestMessage>(policy, observers));

        Exception? actual = await Record.ExceptionAsync(() => operation);

        if (outcome == "success")
            Assert.Null(actual);
        else if (outcome is "schedule-failure" or "foreign-cancellation")
        {
            TransportException transport = Assert.IsType<TransportException>(actual);
            Assert.Collection(Assert.IsType<AggregateException>(transport.InnerException).InnerExceptions,
                error => Assert.Same(failure, error), error => Assert.Same(business, error));
        }
        else if (outcome == "after-schedule-cancellation")
            Assert.Equal(sourceCancellation.Token, Assert.IsType<OperationCanceledException>(actual).CancellationToken);
        else if (outcome == "null-acknowledgment")
            Assert.Equal("The redelivery acknowledgment returned a null task.", Assert.IsType<InvalidOperationException>(actual).Message);
        else
            Assert.Same(failure, actual);
        Assert.Equal(1, attempts);
        Assert.Equal(1, redelivery.Scheduled);
        Assert.Equal(TimeSpan.Zero, redelivery.LastDelay);
        Assert.True(redelivery.LastToken.CanBeCanceled);
        Assert.Equal(outcome is "success" or "acknowledgment-failure" or "null-acknowledgment" ? 1 : 0, acknowledgments);
        if (acknowledgments > 0)
            Assert.Equal(redelivery.LastToken, acknowledgmentToken);
        Assert.Equal(["create", "fault"], observers.Events);
        Assert.Equal(["create"], outerObservers.Events);
        Assert.Equal(1, policy.FactoryCalls);
        Assert.Equal(1, policy.Disposals);
        Assert.Equal(1, outer.FactoryCalls);
        Assert.Equal(1, outer.Disposals);
        Assert.Equal(0, RetryFilterTestFactory.GetRetainedOperationCount(consume));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-RETRY-CONTRACT", "policy-token-getter-failure-is-not-swallowed-by-cancellation-classification")]
    public async Task RetryCancellationClassification_PreservesTokenGetterFailureWithoutAnotherBusinessAttemptAsync(bool nested)
    {
        using var foreignCancellation = new CancellationTokenSource();
        foreignCancellation.Cancel();
        var failure = new OwnershipFailureException("selected policy token getter failure");
        var policy = new FaultingPolicy(Advanced.Retry.Immediate(2), "decision-token", failure)
        {
            DecisionCancellationToken = CancellationToken.None
        };
        var outerPolicy = new FaultingPolicy(Advanced.Retry.Immediate(1));
        var observers = new LifecycleObserver();
        var outerObservers = new LifecycleObserver();
        var source = new TestPipeContext();
        int attempts = 0;
        int effects = 0;
        IPipe<TestPipeContext> pipe = Pipe.New<TestPipeContext>(configuration =>
        {
            if (nested)
                configuration.UseFilter(RetryFilterTestFactory.Create<TestPipeContext>(outerPolicy, outerObservers));
            configuration.UseFilter(RetryFilterTestFactory.Create<TestPipeContext>(policy, observers));
            configuration.UseExecute(_ =>
            {
                if (++attempts == 1)
                    throw new OwnershipFailureException("initial transient business failure");
                if (attempts == 2)
                    throw new OperationCanceledException(foreignCancellation.Token);
                effects++;
            });
        });

        OwnershipFailureException actual = await Assert.ThrowsAsync<OwnershipFailureException>(() => pipe.SendAsync(source));

        Assert.Same(failure, actual);
        Assert.Equal(2, attempts);
        Assert.Equal(0, effects);
        Assert.Equal(2, policy.DecisionTokenReads);
        Assert.Equal(1, policy.FailureCalls);
        Assert.Equal(0, policy.NextDecisionCalls);
        Assert.Equal(1, policy.FactoryCalls);
        Assert.Equal(1, policy.Disposals);
        Assert.Equal(["create", "fault", "before"], observers.Events);
        Assert.Equal(nested ? new[] { "create" } : [], outerObservers.Events);
        Assert.Equal(nested ? 1 : 0, outerPolicy.FactoryCalls);
        Assert.Equal(nested ? 1 : 0, outerPolicy.Disposals);
        Assert.Equal(0, RetryFilterTestFactory.GetRetainedOperationCount(source));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-RETRY-CONTRACT", "projected-terminal-ownership-does-not-repeat-payload-lookups")]
    public async Task TerminalBusinessFailure_PreservesDecisionWithoutRepeatingPayloadLookupsAsync(
        bool afterRetry, bool cleanupFails)
    {
        var failure = new OwnershipFailureException("terminal projected business failure");
        var cleanup = new OwnershipFailureException("projected policy cleanup failure");
        var projected = new PayloadLookupFailureContext(1,
            new OwnershipFailureException("unnecessary terminal payload lookup"));
        var policy = new FaultingPolicy(afterRetry ? Advanced.Retry.Immediate(1) : Advanced.Retry.None,
            cleanupFailure: cleanupFails ? cleanup : null)
        {
            ProjectedContext = projected
        };
        var outerPolicy = new FaultingPolicy(Advanced.Retry.Immediate(1));
        var observer = new LifecycleObserver();
        var outerObserver = new LifecycleObserver();
        var source = new TestPipeContext();
        int attempts = 0;
        int effects = 0;
        IPipe<BasePipeContext> pipe = Pipe.New<BasePipeContext>(configuration =>
        {
            configuration.UseFilter(RetryFilterTestFactory.Create<BasePipeContext>(outerPolicy, outerObserver));
            configuration.UseFilter(RetryFilterTestFactory.Create<BasePipeContext>(policy, observer));
            configuration.UseExecute(_ =>
            {
                if (++attempts <= (afterRetry ? 2 : 1))
                    throw failure;
                effects++;
            });
        });

        if (cleanupFails)
        {
            AggregateException actual = await Assert.ThrowsAsync<AggregateException>(() => pipe.SendAsync(source));
            Assert.Collection(actual.InnerExceptions,
                exception => Assert.Same(failure, exception), exception => Assert.Same(cleanup, exception));
        }
        else
        {
            OwnershipFailureException actual = await Assert.ThrowsAsync<OwnershipFailureException>(() => pipe.SendAsync(source));
            Assert.Same(failure, actual);
        }

        Assert.Equal(afterRetry ? 2 : 1, attempts);
        Assert.Equal(0, effects);
        Assert.Equal(0, projected.LookupCalls);
        Assert.Equal(afterRetry ? 1 : 0, policy.NextDecisionCalls);
        Assert.Equal(1, policy.FactoryCalls);
        Assert.Equal(1, policy.Disposals);
        Assert.Equal(afterRetry ? new[] { "create", "fault", "before", "terminal" } : ["create", "terminal"], observer.Events);
        Assert.Equal(cleanupFails ? new[] { "create" } : ["create", "terminal"], outerObserver.Events);
        Assert.Same(failure, observer.TerminalContext!.Exception);
        if (!cleanupFails)
            Assert.Same(observer.TerminalContext, outerObserver.TerminalContext);
        Assert.Equal(1, outerPolicy.FactoryCalls);
        Assert.Equal(1, outerPolicy.Disposals);
        Assert.Equal(0, RetryFilterTestFactory.GetRetainedOperationCount(source));
        projected.DisarmLookupFailure();
        Assert.Equal(0, RetryFilterTestFactory.GetRetainedOperationCount(projected));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-RETRY-CONTRACT", "necessary-projection-admission-failure-is-owned-and-failure-atomic")]
    public async Task ProjectionAdmissionFailure_PreservesPrimaryAndCleanupWithoutOuterReplayAsync(
        bool retryProjection, bool cleanupFails)
    {
        var primary = new OwnershipFailureException("necessary projection admission");
        var cleanup = new OwnershipFailureException("projection admission cleanup");
        var projected = new FaultingPayloadContext(primary);
        projected.ArmGetOrAddFailure();
        var policy = new FaultingPolicy(Advanced.Retry.Immediate(1), cleanupFailure: cleanupFails ? cleanup : null)
        {
            ProjectedContext = retryProjection ? null : projected,
            RetryProjectedContext = retryProjection ? projected : null
        };
        var outerPolicy = new FaultingPolicy(Advanced.Retry.Immediate(1));
        var inner = new LifecycleObserver();
        var outer = new LifecycleObserver();
        var source = new TestPipeContext();
        int attempts = 0;
        int effects = 0;
        IPipe<BasePipeContext> pipe = Pipe.New<BasePipeContext>(configuration =>
        {
            configuration.UseFilter(RetryFilterTestFactory.Create<BasePipeContext>(outerPolicy, outer));
            configuration.UseFilter(RetryFilterTestFactory.Create<BasePipeContext>(policy, inner));
            configuration.UseExecute(_ =>
            {
                if (++attempts == 1 && retryProjection)
                    throw new OwnershipFailureException("transient admission business failure");
                effects++;
            });
        });

        if (cleanupFails)
        {
            AggregateException actual = await Assert.ThrowsAsync<AggregateException>(() => pipe.SendAsync(source));
            Assert.Collection(actual.InnerExceptions,
                exception => Assert.Same(primary, exception), exception => Assert.Same(cleanup, exception));
        }
        else
            Assert.Same(primary, await Assert.ThrowsAsync<OwnershipFailureException>(() => pipe.SendAsync(source)));

        Assert.Equal(retryProjection ? 1 : 0, attempts);
        Assert.Equal(0, effects);
        Assert.Equal(1, projected.GetOrAddCalls);
        Assert.False(projected.GetOrAddFailurePending);
        Assert.Equal(1, policy.FactoryCalls);
        Assert.Equal(1, policy.Disposals);
        Assert.Equal(0, policy.NextDecisionCalls);
        Assert.Equal(retryProjection ? new[] { "create", "fault" } : [], inner.Events);
        Assert.Equal(["create"], outer.Events);
        Assert.Equal(1, outerPolicy.FactoryCalls);
        Assert.Equal(1, outerPolicy.Disposals);
        Assert.Equal(0, RetryFilterTestFactory.GetRetainedOperationCount(source));
        Assert.Equal(0, RetryFilterTestFactory.GetRetainedOperationCount(projected));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-RETRY-CONTRACT", "failure-marking-uses-acquired-alias-and-projection-references")]
    public async Task FailureMarking_PreservesPrimaryWithoutRepeatingGetOrAddAsync(bool projection, bool cleanupFails)
    {
        var primary = new OwnershipFailureException("observer primary");
        var cleanup = new OwnershipFailureException("observer cleanup");
        var context = new FaultingPayloadContext(new OwnershipFailureException("unnecessary marking payload callback"));
        var projected = new FaultingPayloadContext(new OwnershipFailureException("unnecessary projection payload callback"));
        BasePipeContext source = context;
        var policy = new FaultingPolicy(Advanced.Retry.Immediate(1), cleanupFailure: cleanupFails ? cleanup : null)
        {
            ProjectedContext = projection ? projected : null
        };
        var outerPolicy = new FaultingPolicy(Advanced.Retry.Immediate(1));
        var inner = new LifecycleObserver("create", primary, failOnce: true, notification: phase =>
        {
            if (phase == "create")
                context.ArmGetOrAddFailure();
        });
        var outer = new LifecycleObserver();
        int effects = 0;
        IPipe<BasePipeContext> pipe = Pipe.New<BasePipeContext>(configuration =>
        {
            configuration.UseFilter(RetryFilterTestFactory.Create<BasePipeContext>(outerPolicy, outer));
            configuration.UseFilter(RetryFilterTestFactory.Create<BasePipeContext>(policy, inner));
            configuration.UseExecute(_ => effects++);
        });

        if (cleanupFails)
        {
            AggregateException actual = await Assert.ThrowsAsync<AggregateException>(() => pipe.SendAsync(source));
            Assert.Collection(actual.InnerExceptions,
                exception => Assert.Same(primary, exception), exception => Assert.Same(cleanup, exception));
        }
        else
            Assert.Same(primary, await Assert.ThrowsAsync<OwnershipFailureException>(() => pipe.SendAsync(source)));

        Assert.Equal(0, effects);
        Assert.Equal(1, context.GetOrAddCalls);
        Assert.True(context.GetOrAddFailurePending);
        Assert.Equal(1, policy.FactoryCalls);
        Assert.Equal(1, policy.Disposals);
        Assert.Equal(1, outerPolicy.FactoryCalls);
        Assert.Equal(1, outerPolicy.Disposals);
        Assert.Equal(["create"], inner.Events);
        Assert.Equal(["create"], outer.Events);
        Assert.Equal(0, RetryFilterTestFactory.GetRetainedOperationCount(source));
        Assert.Equal(0, RetryFilterTestFactory.GetRetainedOperationCount(context));
        Assert.Equal(projection ? 1 : 0, projected.GetOrAddCalls);
        Assert.Equal(0, RetryFilterTestFactory.GetRetainedOperationCount(projected));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-RETRY-CONTRACT", "post-cleanup-transfer-preserves-failures-without-payload-callbacks")]
    public async Task PostCleanupTransfer_PreservesPrimaryWithoutRepeatingPayloadLookupAsync(bool cleanupFails)
    {
        var primary = new OwnershipFailureException("pre-cleanup observer primary");
        var cleanup = new OwnershipFailureException("transfer cleanup");
        var source = new FaultingPayloadContext(new OwnershipFailureException("unnecessary transfer payload callback"));
        var policy = new FaultingPolicy(Advanced.Retry.Immediate(1), cleanupFailure: cleanupFails ? cleanup : null)
        {
            Cleanup = source.ArmLookupFailure
        };
        var outerPolicy = new FaultingPolicy(Advanced.Retry.Immediate(1));
        var inner = new LifecycleObserver("create", primary, failOnce: true);
        var outer = new LifecycleObserver();
        int effects = 0;
        IPipe<BasePipeContext> pipe = Pipe.New<BasePipeContext>(configuration =>
        {
            configuration.UseFilter(RetryFilterTestFactory.Create<BasePipeContext>(outerPolicy, outer));
            configuration.UseFilter(RetryFilterTestFactory.Create<BasePipeContext>(policy, inner));
            configuration.UseExecute(_ => effects++);
        });

        if (cleanupFails)
        {
            AggregateException actual = await Assert.ThrowsAsync<AggregateException>(() => pipe.SendAsync(source));
            Assert.Collection(actual.InnerExceptions,
                exception => Assert.Same(primary, exception), exception => Assert.Same(cleanup, exception));
        }
        else
            Assert.Same(primary, await Assert.ThrowsAsync<OwnershipFailureException>(() => pipe.SendAsync(source)));

        Assert.Equal(0, effects);
        Assert.Equal(0, source.LookupCalls);
        Assert.True(source.LookupFailurePending);
        Assert.Equal(1, policy.FactoryCalls);
        Assert.Equal(1, policy.Disposals);
        Assert.Equal(1, outerPolicy.FactoryCalls);
        Assert.Equal(1, outerPolicy.Disposals);
        Assert.Equal(["create"], inner.Events);
        Assert.Equal(["create"], outer.Events);
        source.DisarmLookupFailure();
        Assert.Equal(0, RetryFilterTestFactory.GetRetainedOperationCount(source));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-CONTRACT", "already-associated-child-admission-does-not-repeat-payload-acquisition")]
    public async Task AssociatedChildAdmission_ReusesAcquiredContextWithoutAnotherPayloadCallbackAsync()
    {
        var source = new FaultingPayloadContext(new OwnershipFailureException("unnecessary child admission payload callback"));
        var outerPolicy = new FaultingPolicy(Advanced.Retry.Immediate(1));
        var innerPolicy = new FaultingPolicy(Advanced.Retry.Immediate(1));
        var outer = new LifecycleObserver(notification: phase =>
        {
            if (phase == "create")
                source.ArmGetOrAddFailure();
        });
        var inner = new LifecycleObserver();
        int effects = 0;
        IPipe<BasePipeContext> pipe = Pipe.New<BasePipeContext>(configuration =>
        {
            configuration.UseFilter(RetryFilterTestFactory.Create<BasePipeContext>(outerPolicy, outer));
            configuration.UseFilter(RetryFilterTestFactory.Create<BasePipeContext>(innerPolicy, inner));
            configuration.UseExecute(_ => effects++);
        });

        await pipe.SendAsync(source);

        Assert.Equal(1, effects);
        Assert.Equal(1, source.GetOrAddCalls);
        Assert.True(source.GetOrAddFailurePending);
        Assert.Equal(1, outerPolicy.FactoryCalls);
        Assert.Equal(1, innerPolicy.FactoryCalls);
        Assert.Equal(1, outerPolicy.Disposals);
        Assert.Equal(1, innerPolicy.Disposals);
        Assert.Equal(["create"], outer.Events);
        Assert.Equal(["create"], inner.Events);
        Assert.Equal(0, RetryFilterTestFactory.GetRetainedOperationCount(source));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-RETRY-CONTRACT", "diagnostic-update-failure-is-marked-without-secondary-payload-callback")]
    public async Task DiagnosticUpdateFailure_PreservesExactPrimaryAndCleanupWithoutAnotherPayloadCallbackAsync(bool cleanupFails)
    {
        var primary = new OwnershipFailureException("necessary diagnostic update");
        var cleanup = new OwnershipFailureException("diagnostic cleanup");
        var projected = new FaultingPayloadContext(new OwnershipFailureException("unnecessary diagnostic failure marking"))
        {
            DiagnosticFailure = primary
        };
        var policy = new FaultingPolicy(Advanced.Retry.None, cleanupFailure: cleanupFails ? cleanup : null)
        {
            ProjectedContext = projected
        };
        var outerPolicy = new FaultingPolicy(Advanced.Retry.Immediate(1));
        var source = projected;
        var inner = new LifecycleObserver();
        var outer = new LifecycleObserver();
        int attempts = 0;
        IPipe<BasePipeContext> pipe = Pipe.New<BasePipeContext>(configuration =>
        {
            configuration.UseFilter(RetryFilterTestFactory.Create<BasePipeContext>(outerPolicy, outer));
            configuration.UseFilter(RetryFilterTestFactory.Create<BasePipeContext>(policy, inner));
            configuration.UseExecute(_ =>
            {
                attempts++;
                throw new OwnershipFailureException("terminal diagnostic business failure");
            });
        });

        if (cleanupFails)
        {
            AggregateException actual = await Assert.ThrowsAsync<AggregateException>(() => pipe.SendAsync(source));
            Assert.Collection(actual.InnerExceptions,
                exception => Assert.Same(primary, exception), exception => Assert.Same(cleanup, exception));
        }
        else
            Assert.Same(primary, await Assert.ThrowsAsync<OwnershipFailureException>(() => pipe.SendAsync(source)));

        Assert.Equal(1, attempts);
        Assert.Equal(1, projected.DiagnosticCalls);
        Assert.Equal(1, projected.GetOrAddCalls);
        Assert.True(projected.GetOrAddFailurePending);
        Assert.Equal(1, policy.FactoryCalls);
        Assert.Equal(1, policy.Disposals);
        Assert.Equal(1, outerPolicy.FactoryCalls);
        Assert.Equal(1, outerPolicy.Disposals);
        Assert.Equal(["create"], inner.Events);
        Assert.Equal(["create"], outer.Events);
        Assert.Equal(0, RetryFilterTestFactory.GetRetainedOperationCount(source));
        Assert.Equal(0, RetryFilterTestFactory.GetRetainedOperationCount(projected));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-RETRY-COMPOSITION", "released-context-alias-retains-live-marker-ownership-without-callbacks")]
    public void ReleasedContextAlias_RetainsLiveMarkerOwnershipAndIndependentChildState(bool armCallbacks)
    {
        var cache = new ListPayloadCache();
        var primary = new OwnershipFailureException("live alias lifecycle failure");
        var first = new FaultingPayloadContext(new OwnershipFailureException("released alias payload callback"), cache);
        var second = new FaultingPayloadContext(new OwnershipFailureException("active alias payload callback"), cache);
        var root = new TestPipeContext();
        using RetryPolicyContext<BasePipeContext> policy = Advanced.Retry.None.CreatePolicyContext<BasePipeContext>(first);
        Assert.False(policy.CanRetry(primary, out RetryContext<BasePipeContext> terminal));

        var result = RetryFilterTestFactory.ObserveReleasedAliasOwnership(root, first, second, primary, terminal, () =>
        {
            if (armCallbacks)
            {
                first.ArmGetOrAddFailure();
                first.ArmLookupFailure();
            }
        });

        Assert.True(result.LifecycleOwned);
        Assert.True(result.TerminalOwned);
        Assert.Same(terminal, result.Terminal);
        Assert.True(result.ReenteredLifecycleOwned);
        Assert.False(result.IndependentChildOwned);
        Assert.Equal(0, result.RetainedAssociations);
        if (armCallbacks)
        {
            Assert.Equal(1, first.GetOrAddCalls);
            Assert.Equal(0, first.LookupCalls);
            Assert.True(first.GetOrAddFailurePending);
            Assert.True(first.LookupFailurePending);
            first.DisarmLookupFailure();
        }
        Assert.Equal(0, RetryFilterTestFactory.GetRetainedOperationCount(root));
        Assert.Equal(0, RetryFilterTestFactory.GetRetainedOperationCount(first));
        Assert.Equal(0, RetryFilterTestFactory.GetRetainedOperationCount(second));
    }

    private static async Task DrainAsync(Task operation)
    {
        try
        {
            await operation.WaitAsync(OperationTimeout(), CancellationToken.None);
        }
        catch (Exception exception) when (exception is not TimeoutException)
        {
            // Teardown observes terminal faults while releasing pending test work.
        }
    }

    private static async Task AssertDecisionGetterFailureAsync<T>(T context,
        Func<IRetryPolicy, IRetryObserver?, IFilter<T>> create, FaultingPolicy policy, Exception expected, int expectedAttempts)
        where T : class, PipeContext
    {
        var outer = new LifecycleObserver();
        int attempts = 0;
        IPipe<T> pipe = Pipe.New<T>(configuration =>
        {
            configuration.UseFilter(RetryFilterTestFactory.Create<T>(Advanced.Retry.Immediate(1), outer));
            configuration.UseFilter(create(policy, new LifecycleObserver()));
            configuration.UseExecute(_ =>
            {
                attempts++;
                throw new OwnershipFailureException("business failure");
            });
        });

        Assert.Same(expected, await Assert.ThrowsAsync<OwnershipFailureException>(() => pipe.SendAsync(context)));
        Assert.Equal(expectedAttempts, attempts);
        Assert.Equal(1, policy.FactoryCalls);
        Assert.Equal(1, policy.FailureCalls);
        Assert.Equal(1, policy.Disposals);
        Assert.Equal(["create"], outer.Events);
    }

    private static async Task AssertCrossFilterFailureAsync<T>(T source,
        Func<IRetryPolicy, IRetryObserver?, IFilter<T>> createRedelivery,
        bool innerImmediateRetry, string phase, int expectedAttempts, int expectedEffects)
        where T : class, PipeContext
    {
        var failure = new OwnershipFailureException("nested " + phase);
        var inner = new LifecycleObserver(phase, failure);
        var outer = new LifecycleObserver();
        var innerPolicy = new FaultingPolicy(!innerImmediateRetry && phase == "terminal"
            ? Advanced.Retry.None : Advanced.Retry.Immediate(1));
        var outerPolicy = new FaultingPolicy(Advanced.Retry.Immediate(1));
        var redelivery = new RecordingRedelivery();
        source.GetOrAddPayload<MessageRedeliveryContext>(() => redelivery);
        int attempts = 0;
        int effects = 0;
        IPipe<T> pipe = Pipe.New<T>(configuration =>
        {
            configuration.UseFilter(innerImmediateRetry ? createRedelivery(outerPolicy, outer)
                : RetryFilterTestFactory.Create<T>(outerPolicy, outer));
            configuration.UseFilter(innerImmediateRetry ? RetryFilterTestFactory.Create<T>(innerPolicy, inner)
                : createRedelivery(innerPolicy, inner));
            configuration.UseExecute(_ =>
            {
                if (++attempts == 1 || phase == "terminal")
                    throw new OwnershipFailureException("business failure");
                effects++;
            });
        });

        OwnershipFailureException actual = await Assert.ThrowsAsync<OwnershipFailureException>(() => pipe.SendAsync(source));

        Assert.Same(failure, actual);
        Assert.Equal(expectedAttempts, attempts);
        Assert.Equal(expectedEffects, effects);
        Assert.Equal(0, redelivery.Scheduled);
        Assert.Equal(1, innerPolicy.FactoryCalls);
        Assert.Equal(1, outerPolicy.FactoryCalls);
        Assert.Equal(1, innerPolicy.Disposals);
        Assert.Equal(1, outerPolicy.Disposals);
        Assert.Equal(["create"], outer.Events);
        Assert.Equal(phase switch
        {
            "create" => new[] { "create" },
            "fault" => ["create", "fault"],
            "before" => ["create", "fault", "before"],
            "complete" => ["create", "fault", "before", "complete"],
            _ => innerImmediateRetry ? ["create", "fault", "before", "terminal"] : ["create", "terminal"]
        }, inner.Events);
    }

    private static IPipe<TestPipeContext> CreateNestedPipe(IRetryPolicy innerPolicy, IRetryObserver outer,
        IRetryObserver inner, Action execute) => Pipe.New<TestPipeContext>(configuration =>
    {
        configuration.UseFilter(RetryFilterTestFactory.Create<TestPipeContext>(Advanced.Retry.Immediate(1), outer));
        configuration.UseFilter(RetryFilterTestFactory.Create<TestPipeContext>(innerPolicy, inner));
        configuration.UseExecute(_ => execute());
    });

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions().OperationTimeout!.Value;

    private sealed class TestPipeContext : BasePipeContext
    {
        public TestPipeContext()
        {
        }

        public TestPipeContext(CancellationToken cancellationToken) : base(cancellationToken)
        {
        }
    }

    private sealed class PayloadLookupFailureContext(int failureLookup, Exception failure) : BasePipeContext
    {
        private bool _failureEnabled = true;
        public int LookupCalls { get; private set; }

        public void DisarmLookupFailure() => _failureEnabled = false;

        public override bool TryGetPayload<T>([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out T? payload)
            where T : class
        {
            if (++LookupCalls == failureLookup && _failureEnabled)
                throw failure;
            return base.TryGetPayload(out payload);
        }
    }

    private sealed class FaultingPayloadContext(Exception failure, IPayloadCache? cache = null)
        : BasePipeContext(cache ?? new ListPayloadCache())
    {
        public int GetOrAddCalls { get; private set; }
        public int LookupCalls { get; private set; }
        public int DiagnosticCalls { get; private set; }
        public bool GetOrAddFailurePending { get; private set; }
        public bool LookupFailurePending { get; private set; }
        public Exception? DiagnosticFailure { get; init; }

        public void ArmGetOrAddFailure() => GetOrAddFailurePending = true;
        public void ArmLookupFailure() => LookupFailurePending = true;
        public void DisarmLookupFailure() => LookupFailurePending = false;

        public override T GetOrAddPayload<T>(PayloadFactory<T> payloadFactory) where T : class
        {
            GetOrAddCalls++;
            if (GetOrAddFailurePending)
            {
                GetOrAddFailurePending = false;
                throw failure;
            }
            return base.GetOrAddPayload(payloadFactory);
        }

        public override bool TryGetPayload<T>([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out T? payload)
            where T : class
        {
            LookupCalls++;
            if (LookupFailurePending)
            {
                LookupFailurePending = false;
                throw failure;
            }
            return base.TryGetPayload(out payload);
        }

        public override T AddOrUpdatePayload<T>(PayloadFactory<T> addFactory, UpdatePayloadFactory<T> updateFactory)
            where T : class
        {
            DiagnosticCalls++;
            if (DiagnosticFailure != null && DiagnosticCalls == 1)
            {
                ArmGetOrAddFailure();
                throw DiagnosticFailure;
            }
            return base.AddOrUpdatePayload(addFactory, updateFactory);
        }
    }

    public sealed class TestMessage;

    private sealed class OwnershipFailureException(string message) : Exception(message);

    private sealed class RecordingRedelivery(Func<CancellationToken, Task>? schedule = null) : MessageRedeliveryContext
    {
        public int Scheduled { get; private set; }
        public CancellationToken LastToken { get; private set; }
        public TimeSpan LastDelay { get; private set; }

        public Task ScheduleRedeliveryAsync(TimeSpan delay, Action<ConsumeContext, SendContext>? callback = null,
            CancellationToken cancellationToken = default)
        {
            Scheduled++;
            LastToken = cancellationToken;
            LastDelay = delay;
            return schedule == null ? Task.CompletedTask : schedule(cancellationToken);
        }
    }

    private sealed class TestActivityContext(ConsumeContext context, Func<CancellationToken, Task>? acknowledge = null) :
        ConsumeContextProxy(context), Advanced.ActivityContext
    {
        public IReadOnlyDictionary<string, object> Variables { get; } = new Dictionary<string, object>();
        public Guid TrackingNumber { get; } = Guid.NewGuid();
        public string ActivityName => "ownership-test";
        public Guid ExecutionId { get; } = Guid.NewGuid();
        public DateTimeOffset Timestamp => DateTimeOffset.UnixEpoch;
        public TimeSpan Elapsed => TimeSpan.Zero;

        public Task NotifyActivityConsumedAsync(TimeSpan duration, string consumerType, CancellationToken cancellationToken = default) =>
            acknowledge == null ? throw new InvalidOperationException("Lifecycle failure must not acknowledge an activity delivery.")
                : acknowledge(cancellationToken);
    }

    private sealed class AcknowledgingConsumeContext(ConsumeContext<TestMessage> context,
        Func<CancellationToken, Task> acknowledge) : ConsumeContextProxy<TestMessage>(context)
    {
        public override Task NotifyConsumedAsync<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType,
            CancellationToken cancellationToken = default) => acknowledge(cancellationToken);
    }

    private sealed class HoldingFailureFilter(Exception expected, TaskCompletionSource entered, TaskCompletionSource release) : IFilter<TestPipeContext>
    {
        public void Probe(ProbeContext context) => context.CreateFilterScope("held-lifecycle-failure");

        public async Task SendAsync(TestPipeContext context, IPipe<TestPipeContext> next)
        {
            try
            {
                await next.SendAsync(context);
            }
            catch (Exception exception) when (ReferenceEquals(exception, expected))
            {
                entered.TrySetResult();
                await release.Task;
                throw;
            }
        }
    }

    private sealed class LifecycleObserver(string? failurePhase = null, Exception? failure = null, bool failOnce = false,
        Action<string>? notification = null) : IRetryObserver
    {
        private int _failures;
        public List<string> Events { get; } = [];
        public RetryContext? TerminalContext { get; private set; }

        public Task PostCreateAsync<T>(RetryPolicyContext<T> context) where T : class, PipeContext => NotifyAsync("create");
        public Task PostFaultAsync<T>(RetryContext<T> context) where T : class, PipeContext => NotifyAsync("fault");
        public Task PreRetryAsync<T>(RetryContext<T> context) where T : class, PipeContext => NotifyAsync("before");
        public Task RetryCompleteAsync<T>(RetryContext<T> context) where T : class, PipeContext => NotifyAsync("complete");

        public Task RetryFaultAsync<T>(RetryContext<T> context) where T : class, PipeContext
        {
            TerminalContext = context;
            return NotifyAsync("terminal");
        }

        private Task NotifyAsync(string phase)
        {
            Events.Add(phase);
            notification?.Invoke(phase);
            if (phase == failurePhase && failure != null && (!failOnce || ++_failures == 1))
                throw failure;
            return Task.CompletedTask;
        }
    }

    private sealed class FaultingPolicy(IRetryPolicy policy, string? failurePhase = null,
        Exception? failure = null, Exception? cleanupFailure = null) : IRetryPolicy
    {
        private readonly string? _failurePhase = failurePhase;
        private readonly Exception? _cleanupFailure = cleanupFailure;
        private Action? _cancelActiveContext;
        public int FactoryCalls { get; private set; }
        public int FailureCalls { get; private set; }
        public int Disposals { get; private set; }
        public int DecisionTokenReads { get; private set; }
        public int NextDecisionCalls { get; private set; }
        public CancellationToken LastDecisionToken { get; private set; }
        public CancellationToken? DecisionCancellationToken { get; init; }
        public PipeContext? ProjectedContext { get; init; }
        public PipeContext? RetryProjectedContext { get; init; }
        public Action? Cleanup { get; init; }
        public Func<CancellationToken, Task>? FaultCallback { get; init; }

        public void CancelActiveContext() => (_cancelActiveContext
            ?? throw new InvalidOperationException("No policy context has been acquired.")).Invoke();

        public void Probe(ProbeContext context) => policy.Probe(context);

        public RetryPolicyContext<T> CreatePolicyContext<T>(T context) where T : class, PipeContext
        {
            FactoryCalls++;
            Fail("factory");
            if (_failurePhase == "null-policy")
                return null!;
            RetryPolicyContext<T> acquired = policy.CreatePolicyContext(context);
            _cancelActiveContext = acquired.Cancel;
            return new PolicyContext<T>(acquired, this);
        }

        public bool IsHandled(Exception exception)
        {
            Fail("classification");
            return policy.IsHandled(exception);
        }

        private void Fail(string phase)
        {
            if (_failurePhase == phase && failure != null)
            {
                FailureCalls++;
                if (FailureCalls == 1)
                    throw failure;
            }
        }

        private sealed class PolicyContext<T>(RetryPolicyContext<T> context, FaultingPolicy owner) : RetryPolicyContext<T>
            where T : class, PipeContext
        {
            public T Context => owner._failurePhase == "null-context" ? null!
                : owner.ProjectedContext == null ? context.Context : (T)owner.ProjectedContext;

            public bool CanRetry(Exception exception, out RetryContext<T> retryContext)
            {
                owner.Fail("initial-decision");
                bool allowed = context.CanRetry(exception, out RetryContext<T> decision);
                owner.LastDecisionToken = owner.DecisionCancellationToken ?? decision.CancellationToken;
                retryContext = new Decision<T>(decision, owner);
                if (owner._failurePhase is "null-initial-allowed" or "null-initial-denied")
                {
                    retryContext = null!;
                    return owner._failurePhase == "null-initial-allowed";
                }
                return allowed;
            }

            public Task RetryFaultedAsync(Exception exception, CancellationToken cancellationToken = default) =>
                owner.FaultCallback == null ? context.RetryFaultedAsync(exception, cancellationToken)
                    : owner.FaultCallback(cancellationToken);
            public void Cancel() => context.Cancel();

            public void Dispose()
            {
                context.Dispose();
                owner.Disposals++;
                owner.Cleanup?.Invoke();
                if (owner._cleanupFailure != null && owner.Disposals == 1)
                    throw owner._cleanupFailure;
            }
        }

        private sealed class Decision<T>(RetryContext<T> context, FaultingPolicy owner) : RetryContext<T>
            where T : class, PipeContext
        {
            private int _contextReads;

            public T Context => owner._failurePhase == "null-actual-context" && ++_contextReads > 1 ? null!
                : owner.RetryProjectedContext is { } projected ? (T)projected
                : owner.ProjectedContext == null ? context.Context : (T)owner.ProjectedContext;
            public CancellationToken CancellationToken
            {
                get
                {
                    owner.DecisionTokenReads++;
                    if (owner._failurePhase == "decision-token" && owner.DecisionTokenReads == 2)
                        owner.Fail("decision-token");
                    return owner.DecisionCancellationToken ?? context.CancellationToken;
                }
            }
            public Exception Exception
            {
                get
                {
                    owner.Fail("decision-exception");
                    return context.Exception;
                }
            }
            public int RetryAttempt => context.RetryAttempt;
            public int RetryCount => context.RetryCount;
            public TimeSpan? Delay => context.Delay;
            public Type ContextType => context.ContextType;

            public bool CanRetry(Exception exception, out RetryContext<T> retryContext)
            {
                owner.NextDecisionCalls++;
                owner.Fail("next-decision");
                bool allowed = context.CanRetry(exception, out RetryContext<T> decision);
                owner.LastDecisionToken = owner.DecisionCancellationToken ?? decision.CancellationToken;
                retryContext = new Decision<T>(decision, owner);
                if (owner._failurePhase is "null-next-allowed" or "null-next-denied")
                {
                    retryContext = null!;
                    return owner._failurePhase == "null-next-allowed";
                }
                return allowed;
            }

            public Task RetryFaultedAsync(Exception exception, CancellationToken cancellationToken = default) =>
                owner.FaultCallback == null ? context.RetryFaultedAsync(exception, cancellationToken)
                    : owner.FaultCallback(cancellationToken);
            public Task PreRetryAsync(CancellationToken cancellationToken = default) => context.PreRetryAsync(cancellationToken);
        }
    }
}
