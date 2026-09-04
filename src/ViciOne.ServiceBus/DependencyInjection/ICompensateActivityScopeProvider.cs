using System.Threading.Tasks;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>
/// Defines the contract for compensate activity scope provider.
/// </summary>
/// <typeparam name="TActivity">The t activity type.</typeparam>
/// <typeparam name="TLog">The t log type.</typeparam>
public interface ICompensateActivityScopeProvider<TActivity, TLog> :
    IProbeSite
    where TActivity : class, ICompensateActivity<TLog>
    where TLog : class
{
    /// <summary>
    /// Gets scope.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    ValueTask<ICompensateScopeContext<TLog>> GetScopeAsync(CompensateContext<TLog> context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets activity scope.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    ValueTask<ICompensateActivityScopeContext<TActivity, TLog>> GetActivityScopeAsync(CompensateContext<TLog> context, CancellationToken cancellationToken = default);
}
