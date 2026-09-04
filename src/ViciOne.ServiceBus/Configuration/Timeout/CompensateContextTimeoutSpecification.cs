using System;
using System.Threading;
using ViciOne.ServiceBus.Middleware.Timeout;

namespace ViciOne.ServiceBus.Configuration;

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
