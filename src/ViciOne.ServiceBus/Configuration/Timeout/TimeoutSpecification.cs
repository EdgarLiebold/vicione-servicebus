using System;
using System.Threading;
using ViciOne.ServiceBus.Middleware.Timeout;

namespace ViciOne.ServiceBus.Configuration;

internal sealed class TimeoutSpecification<T> :
    TimeoutPipeSpecification<ConsumeContext<T>, TimeoutConsumeContext<T>>
    where T : class
{
    protected override TimeoutConsumeContext<T> CreateContext(ConsumeContext<T> context, CancellationToken cancellationToken, TimeSpan timeout)
    {
        return new TimeoutConsumeContext<T>(context, cancellationToken, timeout);
    }
}
