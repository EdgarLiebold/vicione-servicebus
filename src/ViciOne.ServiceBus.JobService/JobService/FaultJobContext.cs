using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Adapts a serialized job failure to the strongly typed consumer fault pipeline.</summary>
/// <typeparam name="TJob">The job type.</typeparam>
internal sealed class FaultJobContext<TJob> :
    ConsumeContextProxy,
    ConsumeContext<TJob>
    where TJob : class
{
    readonly ConsumeContext<IFaultJob> _context;

    /// <summary>Creates a typed view over a serialized job-failure context.</summary>
    /// <param name="context">The failure context whose transport metadata is preserved.</param>
    /// <param name="job">The deserialized job payload.</param>
    public FaultJobContext(ConsumeContext<IFaultJob> context, TJob job)
        : base(GetAdvancedContext(context))
    {
        _context = context;
        ArgumentNullException.ThrowIfNull(job);

        Job = job;
    }

    /// <summary>Gets the deserialized job payload.</summary>
    public TJob Job { get; }

    /// <summary>Gets the job payload exposed to the typed consume pipeline.</summary>
    public TJob Message => Job;

    /// <summary>Forwards successful fault-pipeline consumption to the original transport context.</summary>
    /// <param name="duration">The time spent in the typed fault pipeline.</param>
    /// <param name="consumerType">The diagnostic consumer type name.</param>
    /// <param name="cancellationToken">The token that cancels observer notification.</param>
    /// <returns>A task that completes when consume observers have been notified.</returns>
    public Task NotifyConsumedAsync(TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
    {
        return _context.Advanced().NotifyConsumedAsync(this, duration, consumerType, cancellationToken: cancellationToken);
    }

    /// <summary>Forwards a typed fault-pipeline failure to the original transport context.</summary>
    /// <param name="duration">The time spent before the typed fault pipeline failed.</param>
    /// <param name="consumerType">The diagnostic consumer type name.</param>
    /// <param name="exception">The pipeline failure.</param>
    /// <param name="cancellationToken">The token that cancels observer notification.</param>
    /// <returns>A task that completes when fault observers have been notified.</returns>
    public Task NotifyFaultedAsync(TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
    {
        return _context.Advanced().NotifyFaultedAsync(this, duration, consumerType, exception, cancellationToken: cancellationToken);
    }

    static ConsumeContext GetAdvancedContext(ConsumeContext<IFaultJob> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Advanced();
    }
}
