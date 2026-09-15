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

    private static ConsumeContext CreateConsumeContext() =>
        (ConsumeContext)InMemoryOutboxTestContextFactory.Create(new object());

    private sealed class TestPipeContext : BasePipeContext;

    private sealed class RetryFailureException(string message) : Exception(message);

    private sealed class MissingContextPolicy(bool missingConsumeContext) : IRetryPolicy
    {
        public void Probe(ProbeContext context) =>
            throw new NotSupportedException("The missing-context policy is not probed.");

        public RetryPolicyContext<T> CreatePolicyContext<T>(T context)
            where T : class, PipeContext =>
            missingConsumeContext ? new MissingConsumePolicyContext<T>() : null!;

        public bool IsHandled(Exception exception) =>
            throw new NotSupportedException("The missing-context policy does not classify failures.");
    }

    private sealed class MissingConsumePolicyContext<T> : RetryPolicyContext<T>
        where T : class, PipeContext
    {
        public T Context => null!;

        public bool CanRetry(Exception exception, out RetryContext<T> retryContext) =>
            throw new NotSupportedException("A missing consume context cannot make retry decisions.");

        public Task RetryFaultedAsync(Exception exception, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("A missing consume context cannot notify faults.");

        public void Cancel() =>
            throw new NotSupportedException("A missing consume context cannot be cancelled.");

        public void Dispose()
        {
        }
    }
}
