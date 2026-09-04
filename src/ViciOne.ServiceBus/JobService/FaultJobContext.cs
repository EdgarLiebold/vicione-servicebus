using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Contracts.JobService;

#nullable enable
namespace ViciOne.ServiceBus.JobService;

public class FaultJobContext<TJob> :
    ConsumeContextProxy,
    ConsumeContext<TJob>
    where TJob : class
{
    readonly ConsumeContext<FaultJob> _context;

    public FaultJobContext(ConsumeContext<FaultJob> context, TJob job)
        : base(context.Advanced())
    {
        _context = context;

        Job = job;
    }

    public TJob Job { get; }

    public TJob Message => Job;

    public Task NotifyConsumedAsync(TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
    {
        return _context.Advanced().NotifyConsumedAsync(this, duration, consumerType, cancellationToken: cancellationToken);
    }

    public Task NotifyFaultedAsync(TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
    {
        return _context.Advanced().NotifyFaultedAsync(this, duration, consumerType, exception, cancellationToken: cancellationToken);
    }
}
