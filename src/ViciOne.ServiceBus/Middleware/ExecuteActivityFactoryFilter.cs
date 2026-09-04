using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Provides an execute activity factory filter implementation.
/// </summary>
/// <typeparam name="TActivity">The t activity type.</typeparam>
/// <typeparam name="TArguments">The t arguments type.</typeparam>
public class ExecuteActivityFactoryFilter<TActivity, TArguments> :
    IFilter<ExecuteContext<TArguments>>
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
    readonly IExecuteActivityFactory<TActivity, TArguments> _factory;
    readonly IPipe<ExecuteActivityContext<TActivity, TArguments>> _pipe;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="factory">The factory value.</param>
    /// <param name="pipe">The pipe value.</param>
    public ExecuteActivityFactoryFilter(IExecuteActivityFactory<TActivity, TArguments> factory, IPipe<ExecuteActivityContext<TActivity, TArguments>> pipe)
    {
        _factory = factory;
        _pipe = pipe;
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task SendAsync(ExecuteContext<TArguments> context, IPipe<ExecuteContext<TArguments>> next)
    {
        await _factory.ExecuteAsync(context, _pipe).ConfigureAwait(false);

        await next.SendAsync(context).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        _factory.Probe(context);
    }
}
