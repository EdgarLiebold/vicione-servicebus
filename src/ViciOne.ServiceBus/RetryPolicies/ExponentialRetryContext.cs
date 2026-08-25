namespace ViciOne.ServiceBus.RetryPolicies
{
    using System;
    using System.Threading;


    public class ExponentialRetryContext<TContext> :
        BaseRetryContext<TContext>,
        RetryContext<TContext>
        where TContext : class, PipeContext
    {
        readonly TimeSpan _delay;
        readonly ExponentialRetryPolicy _policy;

        public ExponentialRetryContext(ExponentialRetryPolicy policy, TContext context, Exception exception, int retryCount,
            CancellationToken cancellationToken)
            : base(context, exception, retryCount, cancellationToken)
        {
            _policy = policy;
            _delay = policy.GetRetryInterval(retryCount);
        }

        public override TimeSpan? Delay => _delay;

        bool RetryContext<TContext>.CanRetry(Exception exception, out RetryContext<TContext> retryContext)
        {
            retryContext = new ExponentialRetryContext<TContext>(_policy, Context, Exception, RetryCount + 1, CancellationToken);

            return RetryAttempt < _policy.RetryLimit && _policy.IsHandled(exception);
        }
    }
}
