using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using ViciOne.ServiceBus.Tests.InternalAccess.Retry;
using Xunit;

namespace ViciOne.ServiceBus.Tests.RetryPolicies;

public sealed class ConsumeContextRetryPolicyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-RETRY-CONTRACT", "untyped-consume-policy-rejects-null-inputs")]
    public void ConstructionAndClassification_RejectNullInputs()
    {
        ArgumentNullException missingPolicy = Assert.Throws<ArgumentNullException>(() =>
            RetryFilterTestFactory.CreateConsumeContextPolicy(null!, TestContext.Current.CancellationToken));
        IRetryPolicy policy = RetryFilterTestFactory.CreateConsumeContextPolicy(
            Retry.Immediate(1), TestContext.Current.CancellationToken);
        ArgumentNullException missingException = Assert.Throws<ArgumentNullException>(() => policy.IsHandled(null!));
        ArgumentNullException missingProbe = Assert.Throws<ArgumentNullException>(() => policy.Probe(null!));

        Assert.Equal("retryPolicy", missingPolicy.ParamName);
        Assert.Equal("exception", missingException.ParamName);
        Assert.Equal("context", missingProbe.ParamName);
        Assert.True(policy.IsHandled(new RetryFailureException("handled")));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-RETRY-CONTRACT", "untyped-consume-policy-requires-consume-context")]
    public void CreatePolicyContext_RequiresAConsumeContext()
    {
        IRetryPolicy policy = RetryFilterTestFactory.CreateConsumeContextPolicy(
            Retry.Immediate(1), TestContext.Current.CancellationToken);

        ArgumentNullException missingContext = Assert.Throws<ArgumentNullException>(() =>
            policy.CreatePolicyContext<ConsumeContext>(null!));
        ArgumentException wrongContext = Assert.Throws<ArgumentException>(() =>
            policy.CreatePolicyContext(new TestPipeContext()));

        Assert.Equal("context", missingContext.ParamName);
        Assert.Equal("context", wrongContext.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-RETRY-CONTRACT", "untyped-consume-policy-projects-current-failure-and-terminal-state")]
    public async Task HandledFailures_ProduceConsumeAwareStateAndTerminalNotificationAsync()
    {
        ConsumeContext consumeContext = CreateConsumeContext();
        IRetryPolicy policy = RetryFilterTestFactory.CreateConsumeContextPolicy(
            Retry.Immediate(1), TestContext.Current.CancellationToken);
        var initialFailure = new RetryFailureException("initial");
        var terminalFailure = new RetryFailureException("terminal");
        using RetryPolicyContext<ConsumeContext> policyContext = policy.CreatePolicyContext(consumeContext);

        Assert.IsAssignableFrom<ConsumeRetryContext>(policyContext.Context);
        Assert.Equal("exception", Assert.Throws<ArgumentNullException>(() =>
            policyContext.CanRetry(null!, out _)).ParamName);
        Assert.True(policyContext.CanRetry(initialFailure, out RetryContext<ConsumeContext> retry));
        Assert.IsAssignableFrom<ConsumeRetryContext>(retry.Context);
        Assert.Same(initialFailure, retry.Exception);
        Assert.Equal(1, retry.RetryAttempt);

        Assert.False(retry.CanRetry(terminalFailure, out RetryContext<ConsumeContext> terminal));
        Assert.Same(terminalFailure, terminal.Exception);
        Assert.Null(terminal.Delay);
        await policyContext.RetryFaultedAsync(terminalFailure, TestContext.Current.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-RETRY-CONTRACT", "untyped-consume-policy-observes-bus-cancellation")]
    public void BusCancellation_PreventsTheInitialRetry()
    {
        using var cancellation = new CancellationTokenSource();
        IRetryPolicy policy = RetryFilterTestFactory.CreateConsumeContextPolicy(Retry.Immediate(1), cancellation.Token);
        using RetryPolicyContext<ConsumeContext> policyContext = policy.CreatePolicyContext(CreateConsumeContext());

        cancellation.Cancel();

        Assert.False(policyContext.CanRetry(new RetryFailureException("stopping"), out RetryContext<ConsumeContext> terminal));
        Assert.True(terminal.CancellationToken.IsCancellationRequested);
        Assert.Null(terminal.Delay);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-RETRY-CONTRACT", "typed-consume-policy-rejects-null-and-wrong-contexts")]
    public void TypedPolicy_RejectsNullInputsAndTheWrongContextType()
    {
        Assert.Equal("retryPolicy", Assert.Throws<ArgumentNullException>(() =>
            RetryFilterTestFactory.CreateTypedConsumeContextPolicy<object>(null!,
                TestContext.Current.CancellationToken)).ParamName);
        IRetryPolicy policy = RetryFilterTestFactory.CreateTypedConsumeContextPolicy<object>(
            Retry.Immediate(1), TestContext.Current.CancellationToken);

        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            policy.CreatePolicyContext<ConsumeContext<object>>(null!)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentException>(() =>
            policy.CreatePolicyContext(new TestPipeContext())).ParamName);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-MESSAGE-RETRY-CONTRACT", "consume-policy-rejects-missing-wrapped-context-state")]
    public void WrappedPolicyOutput_RejectsMissingPolicyOrConsumeContext(bool typed, bool missingConsumeContext)
    {
        var brokenPolicy = new MissingContextPolicy(missingConsumeContext);
        ConsumeContext<object> source = InMemoryOutboxTestContextFactory.Create(
            new object(), TestContext.Current.CancellationToken);
        IRetryPolicy policy = typed
            ? RetryFilterTestFactory.CreateTypedConsumeContextPolicy<object>(brokenPolicy,
                TestContext.Current.CancellationToken)
            : RetryFilterTestFactory.CreateConsumeContextPolicy(brokenPolicy,
                TestContext.Current.CancellationToken);

        InvalidOperationException actual = Assert.Throws<InvalidOperationException>(() =>
        {
            if (typed)
                policy.CreatePolicyContext(source);
            else
                policy.CreatePolicyContext((ConsumeContext)source);
        });

        Assert.Equal(missingConsumeContext
            ? "The retry policy returned a policy context without a consume context."
            : "The retry policy returned a null consume policy context.", actual.Message);
        Assert.Equal(missingConsumeContext ? 1 : 0, brokenPolicy.Disposals);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-RETRY-CONTRACT", "untyped-consume-policy-probes-the-wrapped-policy")]
    public void Probe_ReportsTheWrappedPolicyInAConsumeScope()
    {
        IRetryPolicy policy = RetryFilterTestFactory.CreateConsumeContextPolicy(
            Retry.Immediate(1), TestContext.Current.CancellationToken);

        IProbeResult result = policy.GetProbeResult(TestContext.Current.CancellationToken);

        IDictionary<string, object> scope = Assert.IsAssignableFrom<IDictionary<string, object>>(
            Assert.Contains("retry-consumeContext", result.Results));
        Assert.Equal("Immediate", Assert.Contains("policy", scope));
        Assert.Equal(1, Assert.Contains("limit", scope));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-MESSAGE-RETRY-CONTRACT", "consume-adapters-reject-null-callback-tasks")]
    public async Task CallbackTask_RejectsNullUnderlyingResultsInBothConsumeProjectionsAsync(
        bool typed, bool nullPreRetry)
    {
        var inner = new CallbackPolicy(nullPreRetry, !nullPreRetry);
        IRetryPolicy policy = typed
            ? RetryFilterTestFactory.CreateTypedConsumeContextPolicy<object>(inner, TestContext.Current.CancellationToken)
            : RetryFilterTestFactory.CreateConsumeContextPolicy(inner, TestContext.Current.CancellationToken);
        ConsumeContext<object> source = InMemoryOutboxTestContextFactory.Create(new object(), TestContext.Current.CancellationToken);
        var failure = new RetryFailureException("callback failure");
        if (typed)
            await AssertNullCallbackAsync(policy, source, failure, nullPreRetry);
        else
            await AssertNullCallbackAsync(policy, (ConsumeContext)source, failure, nullPreRetry);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-MESSAGE-RETRY-CONTRACT", "failed-initial-projection-disposes-acquired-policy-context")]
    public void InitialProjectionFailure_DisposesTheAcquiredPolicyContext(bool returnsNull)
    {
        int disposals = 0;
        var inner = new CallbackPolicy(false, false, () => disposals++);
        var failure = new RetryFailureException("projection failure");
        IRetryPolicy policy = RetryFilterTestFactory.CreateProjectedConsumeContextPolicy<object>(
            inner, _ => returnsNull ? null : throw failure, TestContext.Current.CancellationToken);
        ConsumeContext<object> source = InMemoryOutboxTestContextFactory.Create(new object(), TestContext.Current.CancellationToken);

        if (returnsNull)
        {
            InvalidOperationException actual = Assert.Throws<InvalidOperationException>(() => policy.CreatePolicyContext(source));
            Assert.Equal("The consume retry context factory returned null.", actual.Message);
        }
        else
        {
            RetryFailureException actual = Assert.Throws<RetryFailureException>(() => policy.CreatePolicyContext(source));
            Assert.Same(failure, actual);
        }
        Assert.Equal(1, disposals);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-MESSAGE-RETRY-CONTRACT", "failed-policy-representation-disposes-context-and-cancellation-registration")]
    public void RepresentationFailure_ReleasesTheContextAndItsCancellationRegistration(bool typed)
    {
        int disposals = 0;
        int cancellations = 0;
        using var cancellation = new CancellationTokenSource();
        var inner = new CallbackPolicy(false, false, () => disposals++, () => cancellations++);
        IRetryPolicy policy = typed
            ? RetryFilterTestFactory.CreateTypedConsumeContextPolicy<object>(inner, cancellation.Token)
            : RetryFilterTestFactory.CreateConsumeContextPolicy(inner, cancellation.Token);
        ConsumeContext<object> source = InMemoryOutboxTestContextFactory.Create(new object(), TestContext.Current.CancellationToken);

        InvalidOperationException actual = Assert.Throws<InvalidOperationException>(() =>
        {
            if (typed)
                policy.CreatePolicyContext((ConsumeContext)source);
            else
                policy.CreatePolicyContext(source);
        });
        cancellation.Cancel();

        Assert.StartsWith("The retry policy context cannot be represented as ", actual.Message, StringComparison.Ordinal);
        Assert.Equal(1, disposals);
        Assert.Equal(0, cancellations);
    }

    [Theory]
    [InlineData(false, "admission")]
    [InlineData(true, "admission")]
    [InlineData(true, "projection")]
    [InlineData(false, "representation")]
    [InlineData(true, "representation")]
    [RequirementCoverage("REQ-VSB-MESSAGE-RETRY-CONTRACT", "failed-consume-policy-acquisition-preserves-primary-and-cleanup-identities")]
    public void FailedPolicyAcquisition_PreservesPrimaryAndCleanupAndReleasesRegistration(bool typed, string phase)
    {
        var primary = new RetryFailureException("consume policy acquisition failure");
        var cleanup = new RetryFailureException("acquired policy cleanup failure");
        int disposals = 0;
        int cancellations = 0;
        using var cancellation = new CancellationTokenSource();
        var inner = new CallbackPolicy(false, false, () =>
        {
            disposals++;
            throw cleanup;
        }, () => cancellations++, phase == "admission" ? primary : null);
        IRetryPolicy policy = phase == "projection"
            ? RetryFilterTestFactory.CreateProjectedConsumeContextPolicy<object>(inner,
                _ => throw primary, cancellation.Token)
            : typed
                ? RetryFilterTestFactory.CreateTypedConsumeContextPolicy<object>(inner, cancellation.Token)
                : RetryFilterTestFactory.CreateConsumeContextPolicy(inner, cancellation.Token);
        ConsumeContext<object> source = InMemoryOutboxTestContextFactory.Create(new object(), TestContext.Current.CancellationToken);

        AggregateException actual = Assert.Throws<AggregateException>(() =>
        {
            if (phase == "representation" ? !typed : typed)
                policy.CreatePolicyContext(source);
            else
                policy.CreatePolicyContext((ConsumeContext)source);
        });
        cancellation.Cancel();

        Assert.Collection(actual.InnerExceptions, exception =>
        {
            if (phase == "representation")
            {
                InvalidOperationException representation = Assert.IsType<InvalidOperationException>(exception);
                Assert.StartsWith("The retry policy context cannot be represented as ", representation.Message, StringComparison.Ordinal);
            }
            else
                Assert.Same(primary, exception);
        }, exception => Assert.Same(cleanup, exception));
        Assert.Equal(1, inner.FactoryCalls);
        Assert.Equal(1, disposals);
        Assert.Equal(0, cancellations);
    }

    private static async Task AssertNullCallbackAsync<TContext>(IRetryPolicy policy, TContext source,
        Exception failure, bool nullPreRetry)
        where TContext : class, PipeContext
    {
        using RetryPolicyContext<TContext> context = policy.CreatePolicyContext(source);
        Assert.True(context.CanRetry(failure, out RetryContext<TContext> retry));
        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            nullPreRetry ? retry.PreRetryAsync(TestContext.Current.CancellationToken)
                : retry.RetryFaultedAsync(failure, TestContext.Current.CancellationToken));
        Assert.Equal(nullPreRetry
            ? "The retry context returned a null pre-retry task."
            : "The retry context returned a null fault task.", actual.Message);
    }

    private sealed class CallbackPolicy(bool nullPreRetry, bool nullFault, Action? disposed = null,
        Action? canceled = null, Exception? contextFailure = null) : IRetryPolicy
    {
        private readonly IRetryPolicy _inner = Retry.Immediate(1);
        public int FactoryCalls { get; private set; }

        public void Probe(ProbeContext context) => _inner.Probe(context);

        public bool IsHandled(Exception exception) => _inner.IsHandled(exception);

        public RetryPolicyContext<T> CreatePolicyContext<T>(T context)
            where T : class, PipeContext
        {
            FactoryCalls++;
            return new CallbackPolicyContext<T>(_inner.CreatePolicyContext(context), nullPreRetry, nullFault,
                disposed, canceled, contextFailure);
        }
    }

    private sealed class CallbackPolicyContext<T>(RetryPolicyContext<T> inner,
        bool nullPreRetry, bool nullFault, Action? disposed, Action? canceled, Exception? contextFailure) : RetryPolicyContext<T>
        where T : class, PipeContext
    {
        public T Context => contextFailure == null ? inner.Context : throw contextFailure;

        public bool CanRetry(Exception exception, out RetryContext<T> retryContext)
        {
            bool canRetry = inner.CanRetry(exception, out RetryContext<T> retry);
            retryContext = new CallbackRetryContext<T>(retry, nullPreRetry, nullFault);
            return canRetry;
        }

        public Task RetryFaultedAsync(Exception exception, CancellationToken cancellationToken = default) =>
            inner.RetryFaultedAsync(exception, cancellationToken);

        public void Cancel()
        {
            inner.Cancel();
            canceled?.Invoke();
        }

        public void Dispose()
        {
            inner.Dispose();
            disposed?.Invoke();
        }
    }

    private sealed class CallbackRetryContext<T>(RetryContext<T> inner, bool nullPreRetry, bool nullFault) : RetryContext<T>
        where T : class, PipeContext
    {
        public T Context => inner.Context;
        public Exception Exception => inner.Exception;
        public int RetryCount => inner.RetryCount;
        public int RetryAttempt => inner.RetryAttempt;
        public Type ContextType => inner.ContextType;
        public TimeSpan? Delay => inner.Delay;
        public CancellationToken CancellationToken => inner.CancellationToken;

        public Task PreRetryAsync(CancellationToken cancellationToken = default) =>
            nullPreRetry ? null! : inner.PreRetryAsync(cancellationToken);

        public Task RetryFaultedAsync(Exception exception, CancellationToken cancellationToken = default) =>
            nullFault ? null! : inner.RetryFaultedAsync(exception, cancellationToken);

        public bool CanRetry(Exception exception, out RetryContext<T> retryContext) => inner.CanRetry(exception, out retryContext);
    }

    private static ConsumeContext CreateConsumeContext() =>
        (ConsumeContext)InMemoryOutboxTestContextFactory.Create(new object());

    private sealed class TestPipeContext : BasePipeContext;

    private sealed class RetryFailureException(string message) : Exception(message);

    private sealed class MissingContextPolicy(bool missingConsumeContext) : IRetryPolicy
    {
        public int Disposals { get; private set; }

        public void Probe(ProbeContext context) =>
            throw new NotSupportedException("The missing-context policy is not probed.");

        public RetryPolicyContext<T> CreatePolicyContext<T>(T context)
            where T : class, PipeContext =>
            missingConsumeContext ? new MissingConsumePolicyContext<T>(() => Disposals++) : null!;

        public bool IsHandled(Exception exception) =>
            throw new NotSupportedException("The missing-context policy does not classify failures.");
    }

    private sealed class MissingConsumePolicyContext<T>(Action disposed) : RetryPolicyContext<T>
        where T : class, PipeContext
    {
        public T Context => null!;

        public bool CanRetry(Exception exception, out RetryContext<T> retryContext) =>
            throw new NotSupportedException("A missing consume context cannot make retry decisions.");

        public Task RetryFaultedAsync(Exception exception, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("A missing consume context cannot notify faults.");

        public void Cancel() =>
            throw new NotSupportedException("A missing consume context cannot be cancelled.");

        public void Dispose() => disposed();
    }
}
