using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Processes compensate activity factory pipeline stages.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TLog">The log type.</typeparam>
public class CompensateActivityFactoryFilter<TActivity, TLog> :
    IFilter<CompensateContext<TLog>>
    where TActivity : class, ICompensateActivity<TLog>
    where TLog : class
{
    readonly ICompensateActivityFactory<TActivity, TLog> _factory;
    readonly IPipe<CompensateActivityContext<TActivity, TLog>> _pipe;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="factory">The factory invoked by the operation.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    public CompensateActivityFactoryFilter(ICompensateActivityFactory<TActivity, TLog> factory, IPipe<CompensateActivityContext<TActivity, TLog>> pipe)
    {
        _factory = factory;
        _pipe = pipe;
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendAsync(CompensateContext<TLog> context, IPipe<CompensateContext<TLog>> next)
    {
        await _factory.CompensateAsync(context, _pipe).ConfigureAwait(false);

        await next.SendAsync(context).ConfigureAwait(false);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        _factory.Probe(context);
    }
}
