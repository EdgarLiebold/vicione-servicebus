using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Resolves an execution activity, invokes its activity-bound pipeline, and resumes the execution pipeline.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
internal sealed class ExecuteActivityFactoryFilter<TActivity, TArguments> :
    IFilter<ExecuteContext<TArguments>>
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
    readonly IExecuteActivityFactory<TActivity, TArguments> _factory;
    readonly IPipe<ExecuteActivityContext<TActivity, TArguments>> _pipe;

    /// <summary>Creates the bridge between execution and activity-bound pipelines.</summary>
    /// <param name="factory">The factory that resolves and owns execution activity instances.</param>
    /// <param name="pipe">The pipeline invoked with the resolved activity.</param>
    public ExecuteActivityFactoryFilter(IExecuteActivityFactory<TActivity, TArguments> factory, IPipe<ExecuteActivityContext<TActivity, TArguments>> pipe)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _pipe = pipe ?? throw new ArgumentNullException(nameof(pipe));
    }

    /// <summary>Creates the activity, executes its pipeline, and then continues the execution-context pipeline.</summary>
    /// <param name="context">The execution context used to resolve the activity.</param>
    /// <param name="next">The execution pipeline invoked after the activity-bound pipeline.</param>
    /// <returns>A task that completes after both pipelines finish.</returns>
    public async Task SendAsync(ExecuteContext<TArguments> context, IPipe<ExecuteContext<TArguments>> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        await _factory.ExecuteAsync(context, _pipe, context.CancellationToken).ConfigureAwait(false);

        await next.SendAsync(context).ConfigureAwait(false);
    }

    /// <summary>Adds the configured execution factory to the pipeline probe graph.</summary>
    /// <param name="context">The probe context forwarded to the factory.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _factory.Probe(context);
    }
}
