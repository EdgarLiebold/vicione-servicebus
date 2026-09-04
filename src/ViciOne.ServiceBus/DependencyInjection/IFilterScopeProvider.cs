namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>
/// Defines the contract for filter scope provider.
/// </summary>
/// <typeparam name="TContext">The t context type.</typeparam>
public interface IFilterScopeProvider<TContext> :
    IProbeSite
    where TContext : class, PipeContext
{
    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    IFilterScopeContext<TContext> Create(TContext context);
}
