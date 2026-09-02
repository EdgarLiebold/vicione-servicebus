using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.RetryPolicies;

public sealed class BaseRetryPolicyContextTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-POLICY", "explicit-cancel-prevents-subsequent-retry")]
    public void Cancel_BeforeTheFirstFailurePreventsEverySubsequentRetry()
    {
        IRetryPolicy policy = Retry.Immediate(2);
        using RetryPolicyContext<TestPipeContext> context = policy.CreatePolicyContext(new TestPipeContext());

        context.Cancel();

        Assert.False(context.CanRetry(new RetryFailureException(), out RetryContext<TestPipeContext> retry));
        Assert.True(retry.CancellationToken.IsCancellationRequested);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-POLICY", "source-cancellation-propagates-to-retry-context")]
    public void SourceCancellation_PropagatesToAnAlreadyCreatedRetryContext()
    {
        using var cancellation = new CancellationTokenSource();
        IRetryPolicy policy = Retry.Immediate(2);
        using RetryPolicyContext<TestPipeContext> context =
            policy.CreatePolicyContext(new TestPipeContext(cancellation.Token));
        Assert.True(context.CanRetry(new RetryFailureException(), out RetryContext<TestPipeContext> retry));

        cancellation.Cancel();

        Assert.True(retry.CancellationToken.IsCancellationRequested);
    }

    private sealed class TestPipeContext(CancellationToken cancellationToken = default) : BasePipeContext(cancellationToken);

    private sealed class RetryFailureException : Exception;
}
