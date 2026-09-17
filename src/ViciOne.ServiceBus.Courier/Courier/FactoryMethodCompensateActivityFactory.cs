using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Courier;

/// <summary>Creates compensation activities from a delegate and owns their disposal after the pipeline completes.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TLog">The log type.</typeparam>
public sealed class FactoryMethodCompensateActivityFactory<TActivity, TLog> :
    ICompensateActivityFactory<TActivity, TLog>
    where TActivity : class, ICompensateActivity<TLog>
    where TLog : class
{
    readonly Func<TLog, TActivity> _compensateFactory;

    /// <summary>Initializes the factory with the delegate used for each compensation.</summary>
    /// <param name="compensateFactory">The delegate that creates an activity from its deserialized compensation log.</param>
    public FactoryMethodCompensateActivityFactory(Func<TLog, TActivity> compensateFactory)
    {
        _compensateFactory = compensateFactory ?? throw new ArgumentNullException(nameof(compensateFactory));
    }

    /// <summary>Creates an activity for the supplied log and invokes the compensation pipeline.</summary>
    /// <param name="context">The routing-slip compensation context containing the deserialized log.</param>
    /// <param name="next">The activity-bound compensation pipeline.</param>
    /// <param name="cancellationToken">The token that cancels creation before the delegate is invoked.</param>
    /// <returns>A task that completes after pipeline execution and disposal of the created activity.</returns>
    public async Task CompensateAsync(CompensateContext<TLog> context, IPipe<CompensateActivityContext<TActivity, TLog>> next, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        cancellationToken.ThrowIfCancellationRequested();

        TActivity activity = _compensateFactory(context.Log)
            ?? throw new InvalidOperationException("The compensate activity factory returned null.");
        Exception? operationFailure = null;
        try
        {
            CompensateActivityContext<TActivity, TLog> activityContext = context.CreateActivityContext(activity);

            await next.SendAsync(activityContext).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            operationFailure = exception;
        }

        await OwnedActivityLifetime.ReleaseAfterOperationAsync(activity, operationFailure).ConfigureAwait(false);
    }

    /// <summary>Adds this delegate-based factory to the pipeline probe graph.</summary>
    /// <param name="context">The probe context that receives the factory scope.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.CreateScope("factoryMethod");
    }
}
