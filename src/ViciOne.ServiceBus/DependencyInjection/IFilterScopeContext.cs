using System;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>
/// Defines the contract for filter scope context.
/// </summary>
/// <typeparam name="TContext">The t context type.</typeparam>
public interface IFilterScopeContext<TContext> :
    IAsyncDisposable
    where TContext : class, PipeContext
{
    /// <summary>
    /// Gets the filter value.
    /// </summary>
    IFilter<TContext> Filter { get; }
    /// <summary>
    /// Gets the context value.
    /// </summary>
    TContext Context { get; }
}
