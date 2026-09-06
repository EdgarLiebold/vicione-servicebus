using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Processes execute activity factory pipeline stages.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
public class ExecuteActivityFactoryFilter<TActivity, TArguments> :
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
        _factory = factory;
        _pipe = pipe;
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendAsync(ExecuteContext<TArguments> context, IPipe<ExecuteContext<TArguments>> next)
    {
        await _factory.ExecuteAsync(context, _pipe).ConfigureAwait(false);

        await next.SendAsync(context).ConfigureAwait(false);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        _factory.Probe(context);
    }
}
