using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Processes compensate activity factory pipeline stages.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TLog">The log type.</typeparam>
internal sealed class CompensateActivityFactoryFilter<TActivity, TLog> :
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
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _pipe = pipe ?? throw new ArgumentNullException(nameof(pipe));
    }

    /// <summary>Creates the activity, executes its compensation pipeline, and then continues the compensation-context pipeline.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SendAsync(CompensateContext<TLog> context, IPipe<CompensateContext<TLog>> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        await _factory.CompensateAsync(context, _pipe, context.CancellationToken).ConfigureAwait(false);

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
