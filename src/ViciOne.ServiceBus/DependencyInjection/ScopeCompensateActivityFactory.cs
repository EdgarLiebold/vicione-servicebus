using System.Threading.Tasks;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>A factory to create an activity from Autofac, that manages the lifetime scope of the activity.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TLog">The log type.</typeparam>
public class ScopeCompensateActivityFactory<TActivity, TLog> :
    ICompensateActivityFactory<TActivity, TLog>
    where TActivity : class, ICompensateActivity<TLog>
    where TLog : class
{
    readonly ICompensateActivityScopeProvider<TActivity, TLog> _scopeProvider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="scopeProvider">The scope provider.</param>
    public ScopeCompensateActivityFactory(ICompensateActivityScopeProvider<TActivity, TLog> scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    /// <summary>Compensates the completed activity.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task CompensateAsync(CompensateContext<TLog> context, IPipe<CompensateActivityContext<TActivity, TLog>> next, CancellationToken cancellationToken = default)
    {
        await using ICompensateActivityScopeContext<TActivity, TLog> scope = await _scopeProvider.GetActivityScopeAsync(context, cancellationToken: cancellationToken).ConfigureAwait(false);

        await next.SendAsync(scope.Context).ConfigureAwait(false);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("scopeCompensateActivityFactory");

        _scopeProvider.Probe(scope);
    }
}
