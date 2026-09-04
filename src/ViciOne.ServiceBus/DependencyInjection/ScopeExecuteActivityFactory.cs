using System.Threading.Tasks;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>
/// A factory to create an activity from Autofac, that manages the lifetime scope of the activity
/// </summary>
/// <typeparam name="TActivity"></typeparam>
/// <typeparam name="TArguments"></typeparam>
public class ScopeExecuteActivityFactory<TActivity, TArguments> :
    IExecuteActivityFactory<TActivity, TArguments>
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
    readonly IExecuteActivityScopeProvider<TActivity, TArguments> _scopeProvider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="scopeProvider">The scope provider value.</param>
    public ScopeExecuteActivityFactory(IExecuteActivityScopeProvider<TActivity, TArguments> scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task ExecuteAsync(ExecuteContext<TArguments> context, IPipe<ExecuteActivityContext<TActivity, TArguments>> next, CancellationToken cancellationToken = default)
    {
        await using IExecuteActivityScopeContext<TActivity, TArguments> scope = await _scopeProvider.GetActivityScopeAsync(context, cancellationToken: cancellationToken).ConfigureAwait(false);

        await next.SendAsync(scope.Context).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("scopeExecuteActivityFactory");

        _scopeProvider.Probe(scope);
    }
}
