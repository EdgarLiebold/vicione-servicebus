// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.RetryPolicies
{
    using System;
    using System.Threading;


    public class NoRetryPolicyContext<TContext> :
        BaseRetryPolicyContext<TContext>
        where TContext : class, PipeContext
    {
        public NoRetryPolicyContext(IRetryPolicy policy, TContext context)
            : base(policy, context)
        {
        }

        public override bool CanRetry(Exception exception, out RetryContext<TContext> retryContext)
        {
            retryContext = new NoRetryContext<TContext>(Context, exception, CancellationToken);

            return false;
        }

        protected override RetryContext<TContext> CreateRetryContext(Exception exception, CancellationToken cancellationToken)
        {
            return new NoRetryContext<TContext>(Context, exception, cancellationToken);
        }
    }
}
