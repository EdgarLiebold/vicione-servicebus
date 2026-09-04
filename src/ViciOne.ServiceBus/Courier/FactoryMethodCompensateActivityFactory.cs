using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Courier;

/// <summary>
/// Provides a factory method compensate activity factory implementation.
/// </summary>
/// <typeparam name="TActivity">The t activity type.</typeparam>
/// <typeparam name="TLog">The t log type.</typeparam>
public class FactoryMethodCompensateActivityFactory<TActivity, TLog> :
    ICompensateActivityFactory<TActivity, TLog>
    where TActivity : class, ICompensateActivity<TLog>
    where TLog : class
{
    readonly Func<TLog, TActivity> _compensateFactory;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="compensateFactory">The compensate factory value.</param>
    public FactoryMethodCompensateActivityFactory(Func<TLog, TActivity> compensateFactory)
    {
        _compensateFactory = compensateFactory;
    }

    /// <summary>
    /// Performs the compensate operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateScope("factoryMethod");
    }
}
