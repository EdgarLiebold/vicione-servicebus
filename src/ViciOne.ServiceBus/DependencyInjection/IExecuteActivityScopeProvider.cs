using System.Threading.Tasks;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>
/// Defines the contract for execute activity scope provider.
/// </summary>
/// <typeparam name="TActivity">The t activity type.</typeparam>
/// <typeparam name="TArguments">The t arguments type.</typeparam>
public interface IExecuteActivityScopeProvider<TActivity, TArguments> :
    IProbeSite
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
    /// <summary>
    /// Gets scope.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    ValueTask<IExecuteScopeContext<TArguments>> GetScopeAsync(ExecuteContext<TArguments> context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets activity scope.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    ValueTask<IExecuteActivityScopeContext<TActivity, TArguments>> GetActivityScopeAsync(ExecuteContext<TArguments> context, CancellationToken cancellationToken = default);
}
