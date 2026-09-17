using System;
using System.Runtime.ExceptionServices;
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

        TActivity activity = _executeFactory(context.Arguments)
            ?? throw new InvalidOperationException("The execute activity factory returned null.");
        Exception? operationFailure = null;
        try
        {
            ExecuteActivityContext<TActivity, TArguments> activityContext = context.CreateActivityContext(activity);

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


/// <summary>Completes an owned activity operation and releases the activity without losing either failure.</summary>
static class OwnedActivityLifetime
{
    /// <summary>Releases the activity and propagates the operation and release outcomes.</summary>
    /// <param name="activity">The owned activity.</param>
    /// <param name="operationFailure">The failure selected by activity-context creation or pipeline execution.</param>
    /// <returns>A task that completes after activity release and outcome propagation.</returns>
    public static async Task ReleaseAfterOperationAsync(object activity, Exception? operationFailure)
    {
        Exception? releaseFailure = null;
        try
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
        catch (Exception exception)
        {
            releaseFailure = exception;
        }

        if (operationFailure is not null && releaseFailure is not null)
        {
            throw new AggregateException(
                "Activity pipeline and release encountered multiple failures.",
                operationFailure,
                releaseFailure);
        }

        if (operationFailure is not null)
            ExceptionDispatchInfo.Capture(operationFailure).Throw();
        if (releaseFailure is not null)
            ExceptionDispatchInfo.Capture(releaseFailure).Throw();
    }
}
