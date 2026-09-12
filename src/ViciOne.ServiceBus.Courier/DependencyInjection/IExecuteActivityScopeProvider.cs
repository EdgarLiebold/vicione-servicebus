using System.Threading.Tasks;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Provides execute activity scope services.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
internal interface IExecuteActivityScopeProvider<TActivity, TArguments> :
    IProbeSite
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
    /// <summary>Creates or reuses a dependency-injection scope for the execution context.</summary>
    /// <param name="context">The execution context to bind to the selected scope.</param>
    /// <param name="cancellationToken">The token that cancels scope acquisition before it starts.</param>
    /// <returns>A value task containing the scoped execution context.</returns>
    ValueTask<IExecuteScopeContext<TArguments>> GetScopeAsync(ExecuteContext<TArguments> context, CancellationToken cancellationToken = default);

    /// <summary>Creates or reuses a dependency-injection scope and resolves the execution activity.</summary>
    /// <param name="context">The execution context to bind to the resolved activity.</param>
    /// <param name="cancellationToken">The token that cancels scope acquisition before it starts.</param>
    /// <returns>A value task containing the resolved activity and its scoped execution context.</returns>
    ValueTask<IExecuteActivityScopeContext<TActivity, TArguments>> GetActivityScopeAsync(ExecuteContext<TArguments> context, CancellationToken cancellationToken = default);
}
