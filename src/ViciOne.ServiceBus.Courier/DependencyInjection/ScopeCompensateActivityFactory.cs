using System.Threading.Tasks;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Creates compensate activities from dependency-injection scopes and owns each scope for the operation.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TLog">The log type.</typeparam>
internal sealed class ScopeCompensateActivityFactory<TActivity, TLog> :
    ICompensateActivityFactory<TActivity, TLog>
    where TActivity : class, ICompensateActivity<TLog>
    where TLog : class
{
    readonly ICompensateActivityScopeProvider<TActivity, TLog> _scopeProvider;

    /// <summary>Initializes the factory with the provider that owns compensation scopes.</summary>
    /// <param name="scopeProvider">The provider used to resolve an activity and its compensation scope.</param>
    public ScopeCompensateActivityFactory(ICompensateActivityScopeProvider<TActivity, TLog> scopeProvider)
    {
        _scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
    }

    /// <summary>Resolves a scoped activity, invokes its compensation pipeline, and releases the scope after completion.</summary>
    /// <param name="context">The compensation context for which an activity is resolved.</param>
    /// <param name="next">The activity pipeline invoked within the acquired scope.</param>
    /// <param name="cancellationToken">The token that cancels scope acquisition before the pipeline starts.</param>
    /// <returns>A task that completes after the activity pipeline and scope disposal finish.</returns>
    public async Task CompensateAsync(CompensateContext<TLog> context, IPipe<CompensateActivityContext<TActivity, TLog>> next, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        cancellationToken.ThrowIfCancellationRequested();

        ICompensateActivityScopeContext<TActivity, TLog> acquiredScope =
            await _scopeProvider.GetActivityScopeAsync(context, cancellationToken: cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("The compensate activity scope provider returned null.");
        await using ICompensateActivityScopeContext<TActivity, TLog> scope = acquiredScope;

        await next.SendAsync(scope.Context).ConfigureAwait(false);
    }

    /// <summary>Adds this scoped factory and its scope provider to the pipeline probe graph.</summary>
    /// <param name="context">The probe context that receives the factory scope.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var scope = context.CreateScope("scopeCompensateActivityFactory");

        _scopeProvider.Probe(scope);
    }
}
