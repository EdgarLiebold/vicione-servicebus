using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Provides a compensate activity factory filter implementation.
/// </summary>
/// <typeparam name="TActivity">The t activity type.</typeparam>
/// <typeparam name="TLog">The t log type.</typeparam>
public class CompensateActivityFactoryFilter<TActivity, TLog> :
    IFilter<CompensateContext<TLog>>
    where TActivity : class, ICompensateActivity<TLog>
    where TLog : class
{
    readonly ICompensateActivityFactory<TActivity, TLog> _factory;
    readonly IPipe<CompensateActivityContext<TActivity, TLog>> _pipe;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="factory">The factory value.</param>
    /// <param name="pipe">The pipe value.</param>
    public CompensateActivityFactoryFilter(ICompensateActivityFactory<TActivity, TLog> factory, IPipe<CompensateActivityContext<TActivity, TLog>> pipe)
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
    public async Task SendAsync(CompensateContext<TLog> context, IPipe<CompensateContext<TLog>> next)
    {
        await _factory.CompensateAsync(context, _pipe).ConfigureAwait(false);

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
