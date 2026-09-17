using System.Threading.Tasks;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Creates execute activities from dependency-injection scopes and owns each scope for the operation.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
internal sealed class ScopeExecuteActivityFactory<TActivity, TArguments> :
    IExecuteActivityFactory<TActivity, TArguments>
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
    readonly IExecuteActivityScopeProvider<TActivity, TArguments> _scopeProvider;

    /// <summary>Initializes the factory with the provider that owns execution scopes.</summary>
    /// <param name="scopeProvider">The provider used to resolve an activity and its execution scope.</param>
    public ScopeExecuteActivityFactory(IExecuteActivityScopeProvider<TActivity, TArguments> scopeProvider)
    {
        _scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
    }

    /// <summary>Resolves a scoped activity, invokes its pipeline, and releases the scope after completion.</summary>
    /// <param name="context">The execution context for which an activity is resolved.</param>
    /// <param name="next">The activity pipeline invoked within the acquired scope.</param>
    /// <param name="cancellationToken">The token that cancels scope acquisition before the pipeline starts.</param>
    /// <returns>A task that completes after the activity pipeline and scope disposal finish.</returns>
    public async Task ExecuteAsync(ExecuteContext<TArguments> context, IPipe<ExecuteActivityContext<TActivity, TArguments>> next, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        cancellationToken.ThrowIfCancellationRequested();

        IExecuteActivityScopeContext<TActivity, TArguments> acquiredScope =
            await _scopeProvider.GetActivityScopeAsync(context, cancellationToken: cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("The execute activity scope provider returned null.");
        Exception? operationFailure = null;
        try
        {
            await next.SendAsync(acquiredScope.Context).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            operationFailure = exception;
        }

        await OwnedActivityLifetime.ReleaseAfterOperationAsync(acquiredScope, operationFailure).ConfigureAwait(false);
    }

    /// <summary>Adds this scoped factory and its scope provider to the pipeline probe graph.</summary>
    /// <param name="context">The probe context that receives the factory scope.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var scope = context.CreateScope("scopeExecuteActivityFactory");

        _scopeProvider.Probe(scope);
    }
}
