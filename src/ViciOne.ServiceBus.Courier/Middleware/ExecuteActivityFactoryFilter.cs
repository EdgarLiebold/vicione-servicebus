using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Processes execute activity factory pipeline stages.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
internal sealed class ExecuteActivityFactoryFilter<TActivity, TArguments> :
    IFilter<ExecuteContext<TArguments>>
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
    readonly IExecuteActivityFactory<TActivity, TArguments> _factory;
    readonly IPipe<ExecuteActivityContext<TActivity, TArguments>> _pipe;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="factory">The factory invoked by the operation.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    public ExecuteActivityFactoryFilter(IExecuteActivityFactory<TActivity, TArguments> factory, IPipe<ExecuteActivityContext<TActivity, TArguments>> pipe)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _pipe = pipe ?? throw new ArgumentNullException(nameof(pipe));
    }

    /// <summary>Creates the activity, executes its pipeline, and then continues the execution-context pipeline.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendAsync(ExecuteContext<TArguments> context, IPipe<ExecuteContext<TArguments>> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        await _factory.ExecuteAsync(context, _pipe, context.CancellationToken).ConfigureAwait(false);

        await next.SendAsync(context).ConfigureAwait(false);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _factory.Probe(context);
    }
}
