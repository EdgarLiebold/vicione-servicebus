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
    /// <param name="context">The routing-slip execution context containing the deserialized arguments.</param>
    /// <param name="next">The activity-bound execution pipeline.</param>
    /// <param name="cancellationToken">The token that cancels creation before the delegate is invoked.</param>
    /// <returns>A task that completes after pipeline execution and disposal of the created activity.</returns>
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

    /// <summary>Adds this delegate-based factory to the pipeline probe graph.</summary>
    /// <param name="context">The probe context that receives the factory scope.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.CreateScope("factoryMethod");
    }
}
