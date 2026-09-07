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
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task CompensateAsync(CompensateContext<TLog> context, IPipe<CompensateActivityContext<TActivity, TLog>> next, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        cancellationToken.ThrowIfCancellationRequested();

        TActivity? activity = null;
        try
        {
            activity = _compensateFactory(context.Log)
                ?? throw new InvalidOperationException("The compensate activity factory returned null.");

            CompensateActivityContext<TActivity, TLog> activityContext = context.CreateActivityContext(activity);

            await next.SendAsync(activityContext).ConfigureAwait(false);
        }
        finally
        {
            switch (activity)
            {
                case IAsyncDisposable asyncDisposable:
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                    break;
                case IDisposable disposable:
                    disposable.Dispose();
                    break;
            }
        }
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.CreateScope("factoryMethod");
    }
}
