// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.RetryPolicies
{
    using System;


    public class ImmediateRetryPolicy :
        IRetryPolicy
    {
        readonly IExceptionFilter _filter;

        public ImmediateRetryPolicy(IExceptionFilter filter, int retryLimit)
        {
            _filter = filter;
            RetryLimit = retryLimit;
        }

        public int RetryLimit { get; }

        void IProbeSite.Probe(ProbeContext context)
        {
            context.Set(new
            {
                Policy = "Immediate",
                Limit = RetryLimit
            });

            _filter.Probe(context);
        }

        RetryPolicyContext<T> IRetryPolicy.CreatePolicyContext<T>(T context)
        {
            return new ImmediateRetryPolicyContext<T>(this, context);
        }

        public bool IsHandled(Exception exception)
        {
            return _filter.Match(exception);
        }
    }
}
