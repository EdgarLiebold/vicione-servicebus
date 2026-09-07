using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Courier;

/// <summary>Creates execution activities from a delegate and owns their disposal after the pipeline completes.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
public sealed class FactoryMethodExecuteActivityFactory<TActivity, TArguments> :
    IExecuteActivityFactory<TActivity, TArguments>
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
    readonly Func<TArguments, TActivity> _executeFactory;

    /// <summary>Initializes the factory with the delegate used for each execution.</summary>
    /// <param name="executeFactory">The delegate that creates an activity from its deserialized arguments.</param>
    public FactoryMethodExecuteActivityFactory(Func<TArguments, TActivity> executeFactory)
    {
        _executeFactory = executeFactory ?? throw new ArgumentNullException(nameof(executeFactory));
    }

    /// <summary>Creates an activity for the supplied arguments and invokes the execution pipeline.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ExecuteAsync(ExecuteContext<TArguments> context, IPipe<ExecuteActivityContext<TActivity, TArguments>> next, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        cancellationToken.ThrowIfCancellationRequested();

        TActivity? activity = null;
        try
        {
            activity = _executeFactory(context.Arguments)
                ?? throw new InvalidOperationException("The execute activity factory returned null.");

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
        ArgumentNullException.ThrowIfNull(context);
        context.CreateScope("factoryMethod");
    }
}
