using System.Threading.Tasks;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Provides compensate activity scope services.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TLog">The log type.</typeparam>
public interface ICompensateActivityScopeProvider<TActivity, TLog> :
    IProbeSite
    where TActivity : class, ICompensateActivity<TLog>
    where TLog : class
{
    /// <summary>Gets scope.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    ValueTask<ICompensateScopeContext<TLog>> GetScopeAsync(CompensateContext<TLog> context, CancellationToken cancellationToken = default);

    /// <summary>Gets activity scope.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    ValueTask<ICompensateActivityScopeContext<TActivity, TLog>> GetActivityScopeAsync(CompensateContext<TLog> context, CancellationToken cancellationToken = default);
}
