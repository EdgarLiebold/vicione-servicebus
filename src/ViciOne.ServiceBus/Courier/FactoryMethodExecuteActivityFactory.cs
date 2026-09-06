using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Courier;

/// <summary>Creates factory method execute activity instances.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
public class FactoryMethodExecuteActivityFactory<TActivity, TArguments> :
    IExecuteActivityFactory<TActivity, TArguments>
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
    readonly Func<TArguments, TActivity> _executeFactory;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="executeFactory">The execute factory.</param>
    public FactoryMethodExecuteActivityFactory(Func<TArguments, TActivity> executeFactory)
    {
        _executeFactory = executeFactory;
    }

    /// <summary>Runs the configured action.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ExecuteAsync(ExecuteContext<TArguments> context, IPipe<ExecuteActivityContext<TActivity, TArguments>> next, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); TActivity? activity = null;
        try
        {
            activity = _executeFactory(context.Arguments);

            ExecuteActivityContext<TActivity, TArguments> activityContext = context.CreateActivityContext(activity);

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
