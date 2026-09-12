using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Resolves a compensation activity, invokes its activity-bound pipeline, and resumes the compensation pipeline.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TLog">The log type.</typeparam>
internal sealed class CompensateActivityFactoryFilter<TActivity, TLog> :
    IFilter<CompensateContext<TLog>>
    where TActivity : class, ICompensateActivity<TLog>
    where TLog : class
{
    readonly ICompensateActivityFactory<TActivity, TLog> _factory;
    readonly IPipe<CompensateActivityContext<TActivity, TLog>> _pipe;

    /// <summary>Creates the bridge between compensation and activity-bound pipelines.</summary>
    /// <param name="factory">The factory that resolves and owns compensation activity instances.</param>
    /// <param name="pipe">The pipeline invoked with the resolved activity.</param>
    public CompensateActivityFactoryFilter(ICompensateActivityFactory<TActivity, TLog> factory, IPipe<CompensateActivityContext<TActivity, TLog>> pipe)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _pipe = pipe ?? throw new ArgumentNullException(nameof(pipe));
    }

    /// <summary>Creates the activity, executes its compensation pipeline, and then continues the compensation-context pipeline.</summary>
    /// <param name="context">The compensation context used to resolve the activity.</param>
    /// <param name="next">The compensation pipeline invoked after the activity-bound pipeline.</param>
    /// <returns>A task that completes after both pipelines finish.</returns>
    public async Task SendAsync(CompensateContext<TLog> context, IPipe<CompensateContext<TLog>> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        await _factory.CompensateAsync(context, _pipe, context.CancellationToken).ConfigureAwait(false);

        await next.SendAsync(context).ConfigureAwait(false);
    }

    /// <summary>Adds the configured compensation factory to the pipeline probe graph.</summary>
    /// <param name="context">The probe context forwarded to the factory.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _factory.Probe(context);
    }
}
