using System.Threading.Tasks;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Provides execute activity scope services.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
public interface IExecuteActivityScopeProvider<TActivity, TArguments> :
    IProbeSite
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
    /// <summary>Gets scope.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    ValueTask<IExecuteScopeContext<TArguments>> GetScopeAsync(ExecuteContext<TArguments> context, CancellationToken cancellationToken = default);

    /// <summary>Gets activity scope.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    ValueTask<IExecuteActivityScopeContext<TActivity, TArguments>> GetActivityScopeAsync(ExecuteContext<TArguments> context, CancellationToken cancellationToken = default);
}
