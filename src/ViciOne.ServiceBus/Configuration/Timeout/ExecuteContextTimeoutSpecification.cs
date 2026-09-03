namespace ViciOne.ServiceBus.Configuration
{
    using System;
    using System.Threading;
    using Middleware.Timeout;


    internal sealed class ExecuteContextTimeoutSpecification<TArguments> :
        TimeoutPipeSpecification<ExecuteContext<TArguments>, TimeoutExecuteContext<TArguments>>
        where TArguments : class
    {
        protected override TimeoutExecuteContext<TArguments> CreateContext(ExecuteContext<TArguments> context, CancellationToken cancellationToken,
            TimeSpan timeout)
        {
            return new TimeoutExecuteContext<TArguments>(context, cancellationToken, timeout);
        }
    }
}
