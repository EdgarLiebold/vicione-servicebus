using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Courier;

/// <summary>Creates factory method compensate activity instances.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TLog">The log type.</typeparam>
public class FactoryMethodCompensateActivityFactory<TActivity, TLog> :
    ICompensateActivityFactory<TActivity, TLog>
    where TActivity : class, ICompensateActivity<TLog>
    where TLog : class
{
    readonly Func<TLog, TActivity> _compensateFactory;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="compensateFactory">The compensate factory.</param>
    public FactoryMethodCompensateActivityFactory(Func<TLog, TActivity> compensateFactory)
    {
        _compensateFactory = compensateFactory;
    }

    /// <summary>Compensates the completed activity.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task CompensateAsync(CompensateContext<TLog> context, IPipe<CompensateActivityContext<TActivity, TLog>> next, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); TActivity? activity = null;
        try
        {
            activity = _compensateFactory(context.Log);

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
        context.CreateScope("factoryMethod");
    }
}
