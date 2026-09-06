using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Courier;

/// <summary>
/// Provides a factory method execute activity factory implementation.
/// </summary>
/// <typeparam name="TActivity">The t activity type.</typeparam>
/// <typeparam name="TArguments">The t arguments type.</typeparam>
public class FactoryMethodExecuteActivityFactory<TActivity, TArguments> :
    IExecuteActivityFactory<TActivity, TArguments>
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
    readonly Func<TArguments, TActivity> _executeFactory;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="executeFactory">The execute factory value.</param>
    public FactoryMethodExecuteActivityFactory(Func<TArguments, TActivity> executeFactory)
    {
        _executeFactory = executeFactory;
    }

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateScope("factoryMethod");
    }
}
