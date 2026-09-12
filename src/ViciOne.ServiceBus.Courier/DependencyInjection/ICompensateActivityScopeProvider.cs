using System.Threading.Tasks;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Provides compensate activity scope services.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TLog">The log type.</typeparam>
internal interface ICompensateActivityScopeProvider<TActivity, TLog> :
    IProbeSite
    where TActivity : class, ICompensateActivity<TLog>
    where TLog : class
{
    /// <summary>Creates or reuses a dependency-injection scope for the compensation context.</summary>
    /// <param name="context">The compensation context to bind to the selected scope.</param>
    /// <param name="cancellationToken">The token that cancels scope acquisition before it starts.</param>
    /// <returns>A value task containing the scoped compensation context.</returns>
    ValueTask<ICompensateScopeContext<TLog>> GetScopeAsync(CompensateContext<TLog> context, CancellationToken cancellationToken = default);

    /// <summary>Creates or reuses a dependency-injection scope and resolves the compensation activity.</summary>
    /// <param name="context">The compensation context to bind to the resolved activity.</param>
    /// <param name="cancellationToken">The token that cancels scope acquisition before it starts.</param>
    /// <returns>A value task containing the resolved activity and its scoped compensation context.</returns>
    ValueTask<ICompensateActivityScopeContext<TActivity, TLog>> GetActivityScopeAsync(CompensateContext<TLog> context, CancellationToken cancellationToken = default);
}
