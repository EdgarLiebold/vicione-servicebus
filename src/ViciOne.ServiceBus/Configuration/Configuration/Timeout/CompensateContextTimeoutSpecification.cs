namespace ViciOne.ServiceBus.Configuration
{
    using System;
    using System.Threading;
    using Middleware.Timeout;


    internal sealed class CompensateContextTimeoutSpecification<TArguments> :
        TimeoutPipeSpecification<CompensateContext<TArguments>, TimeoutCompensateContext<TArguments>>
        where TArguments : class
    {
        protected override TimeoutCompensateContext<TArguments> CreateContext(CompensateContext<TArguments> context, CancellationToken cancellationToken,
            TimeSpan timeout)
        {
            return new TimeoutCompensateContext<TArguments>(context, cancellationToken, timeout);
        }
    }
}
