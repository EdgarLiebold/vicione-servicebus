using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Middleware;
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

    private sealed class TestPipeContext : BasePipeContext;

    public sealed class TestMessage;

    private sealed class OwnershipFailureException(string message) : Exception(message);

    private sealed class RecordingRedelivery : MessageRedeliveryContext
    {
        public int Scheduled { get; private set; }

        public Task ScheduleRedeliveryAsync(TimeSpan delay, Action<ConsumeContext, SendContext>? callback = null,
            CancellationToken cancellationToken = default)
        {
            Scheduled++;
            return Task.CompletedTask;
        }
    }

    private sealed class TestActivityContext(ConsumeContext context) : ConsumeContextProxy(context), Advanced.ActivityContext
    {
        public IReadOnlyDictionary<string, object> Variables { get; } = new Dictionary<string, object>();
        public Guid TrackingNumber { get; } = Guid.NewGuid();
        public string ActivityName => "ownership-test";
        public Guid ExecutionId { get; } = Guid.NewGuid();
        public DateTimeOffset Timestamp => DateTimeOffset.UnixEpoch;
        public TimeSpan Elapsed => TimeSpan.Zero;

        public Task NotifyActivityConsumedAsync(TimeSpan duration, string consumerType, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Lifecycle failure must not acknowledge an activity delivery.");
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

    private sealed class LifecycleObserver(string? failurePhase = null, Exception? failure = null, bool failOnce = false) : IRetryObserver
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
        public int FactoryCalls { get; private set; }
        public int FailureCalls { get; private set; }
        public int Disposals { get; private set; }

        public void Probe(ProbeContext context) => policy.Probe(context);

        public RetryPolicyContext<T> CreatePolicyContext<T>(T context) where T : class, PipeContext
        {
            FactoryCalls++;
            Fail("factory");
            return _failurePhase == "null-policy" ? null! : new PolicyContext<T>(policy.CreatePolicyContext(context), this);
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
            public T Context => owner._failurePhase == "null-context" ? null! : context.Context;

            public bool CanRetry(Exception exception, out RetryContext<T> retryContext)
            {
                owner.Fail("initial-decision");
                bool allowed = context.CanRetry(exception, out RetryContext<T> decision);
                retryContext = new Decision<T>(decision, owner);
                if (owner._failurePhase is "null-initial-allowed" or "null-initial-denied")
                {
                    retryContext = null!;
                    return owner._failurePhase == "null-initial-allowed";
                }
                return allowed;
            }

            public Task RetryFaultedAsync(Exception exception, CancellationToken cancellationToken = default) =>
                context.RetryFaultedAsync(exception, cancellationToken);
            public void Cancel() => context.Cancel();

            public void Dispose()
            {
                context.Dispose();
                owner.Disposals++;
                if (owner._cleanupFailure != null && owner.Disposals == 1)
                    throw owner._cleanupFailure;
            }
        }

        private sealed class Decision<T>(RetryContext<T> context, FaultingPolicy owner) : RetryContext<T>
            where T : class, PipeContext
        {
            public T Context => context.Context;
            public CancellationToken CancellationToken => context.CancellationToken;
            public Exception Exception => context.Exception;
            public int RetryAttempt => context.RetryAttempt;
            public int RetryCount => context.RetryCount;
            public TimeSpan? Delay => context.Delay;
            public Type ContextType => context.ContextType;

            public bool CanRetry(Exception exception, out RetryContext<T> retryContext)
            {
                owner.Fail("next-decision");
                bool allowed = context.CanRetry(exception, out RetryContext<T> decision);
                retryContext = new Decision<T>(decision, owner);
                if (owner._failurePhase is "null-next-allowed" or "null-next-denied")
                {
                    retryContext = null!;
                    return owner._failurePhase == "null-next-allowed";
                }
                return allowed;
            }

            public Task RetryFaultedAsync(Exception exception, CancellationToken cancellationToken = default) =>
                context.RetryFaultedAsync(exception, cancellationToken);
            public Task PreRetryAsync(CancellationToken cancellationToken = default) => context.PreRetryAsync(cancellationToken);
        }
    }
}
