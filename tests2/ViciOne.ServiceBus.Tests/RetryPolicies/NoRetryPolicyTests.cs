using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.RetryPolicies;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.RetryPolicies;

public sealed class NoRetryPolicyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-POLICY", "none-rejects-null-filter")]
    public void Constructor_RejectsANullExceptionFilter()
    {
        Assert.Throws<ArgumentNullException>(() => new NoRetryPolicy(null!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-POLICY", "none-never-retries")]
    public void PolicyContext_ReturnsTheTerminalFailureContextWithoutPermittingARetry()
    {
        IRetryPolicy policy = Retry.None;
        var source = new TestPipeContext();
        var expected = new RetryFailureException();
        using RetryPolicyContext<TestPipeContext> policyContext = policy.CreatePolicyContext(source);

        bool canRetry = policyContext.CanRetry(expected, out RetryContext<TestPipeContext> retryContext);

        Assert.False(canRetry);
        Assert.Same(source, retryContext.Context);
        Assert.Same(expected, retryContext.Exception);
        Assert.Equal(0, retryContext.RetryCount);
        Assert.Equal(1, retryContext.RetryAttempt);
        Assert.False(retryContext.CanRetry(expected, out RetryContext<TestPipeContext> terminalContext));
        Assert.Same(retryContext, terminalContext);
    }

    private sealed class TestPipeContext : BasePipeContext;

    private sealed class RetryFailureException : Exception;
}
