namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Provides filter scope services.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public interface IFilterScopeProvider<TContext> :
    IProbeSite
    where TContext : class, PipeContext
{
    /// <summary>Creates the requested value.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The newly created instance.</returns>
    IFilterScopeContext<TContext> Create(TContext context);
}
