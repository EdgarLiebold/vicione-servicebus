using System;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Exposes state for filter scope operations.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public interface IFilterScopeContext<TContext> :
    IAsyncDisposable
    where TContext : class, PipeContext
{
    /// <summary>Gets the filter.</summary>
    IFilter<TContext> Filter { get; }
    /// <summary>Gets the context.</summary>
    TContext Context { get; }
}
