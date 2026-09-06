using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Carries state for fault job operations.</summary>
/// <typeparam name="TJob">The job type.</typeparam>
public class FaultJobContext<TJob> :
    ConsumeContextProxy,
    ConsumeContext<TJob>
    where TJob : class
{
    readonly ConsumeContext<FaultJob> _context;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="job">The job.</param>
    public FaultJobContext(ConsumeContext<FaultJob> context, TJob job)
        : base(context.Advanced())
    {
        _context = context;

        Job = job;
    }

    /// <summary>Gets the job.</summary>
    public TJob Job { get; }

    /// <summary>Gets the message.</summary>
    public TJob Message => Job;

    /// <summary>Reports that notify has been consumed.</summary>
    /// <param name="duration">The duration.</param>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task NotifyConsumedAsync(TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
    {
        return _context.Advanced().NotifyConsumedAsync(this, duration, consumerType, cancellationToken: cancellationToken);
    }

    /// <summary>Reports that notify has faulted.</summary>
    /// <param name="duration">The duration.</param>
    /// <param name="consumerType">The runtime consumer type used by the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task NotifyFaultedAsync(TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
    {
        return _context.Advanced().NotifyFaultedAsync(this, duration, consumerType, exception, cancellationToken: cancellationToken);
    }
}
