using System.Threading.Tasks;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>
/// A factory to create an activity from Autofac, that manages the lifetime scope of the activity
/// </summary>
/// <typeparam name="TActivity"></typeparam>
/// <typeparam name="TLog"></typeparam>
public class ScopeCompensateActivityFactory<TActivity, TLog> :
    ICompensateActivityFactory<TActivity, TLog>
    where TActivity : class, ICompensateActivity<TLog>
    where TLog : class
{
    readonly ICompensateActivityScopeProvider<TActivity, TLog> _scopeProvider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="scopeProvider">The scope provider value.</param>
    public ScopeCompensateActivityFactory(ICompensateActivityScopeProvider<TActivity, TLog> scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    /// <summary>
    /// Performs the compensate operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task CompensateAsync(CompensateContext<TLog> context, IPipe<CompensateActivityContext<TActivity, TLog>> next, CancellationToken cancellationToken = default)
    {
        await using ICompensateActivityScopeContext<TActivity, TLog> scope = await _scopeProvider.GetActivityScopeAsync(context, cancellationToken: cancellationToken).ConfigureAwait(false);

        await next.SendAsync(scope.Context).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("scopeCompensateActivityFactory");

        _scopeProvider.Probe(scope);
    }
}
