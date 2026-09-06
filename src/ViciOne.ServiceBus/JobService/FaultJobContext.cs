using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService;

/// <summary>
/// Provides a fault job context implementation.
/// </summary>
/// <typeparam name="TJob">The t job type.</typeparam>
public class FaultJobContext<TJob> :
    ConsumeContextProxy,
    ConsumeContext<TJob>
    where TJob : class
{
    readonly ConsumeContext<FaultJob> _context;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="job">The job value.</param>
    public FaultJobContext(ConsumeContext<FaultJob> context, TJob job)
        : base(context.Advanced())
    {
        _context = context;

        Job = job;
    }

    /// <summary>
    /// Gets the job value.
    /// </summary>
    public TJob Job { get; }

    /// <summary>
    /// Gets the message value.
    /// </summary>
    public TJob Message => Job;

    /// <summary>
    /// Performs the notify consumed operation.
    /// </summary>
    /// <param name="duration">The duration value.</param>
    /// <param name="consumerType">The consumer type value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task NotifyConsumedAsync(TimeSpan duration, string consumerType, CancellationToken cancellationToken = default)
    {
        return _context.Advanced().NotifyConsumedAsync(this, duration, consumerType, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs the notify faulted operation.
    /// </summary>
    /// <param name="duration">The duration value.</param>
    /// <param name="consumerType">The consumer type value.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task NotifyFaultedAsync(TimeSpan duration, string consumerType, Exception exception, CancellationToken cancellationToken = default)
    {
        return _context.Advanced().NotifyFaultedAsync(this, duration, consumerType, exception, cancellationToken: cancellationToken);
    }
}
