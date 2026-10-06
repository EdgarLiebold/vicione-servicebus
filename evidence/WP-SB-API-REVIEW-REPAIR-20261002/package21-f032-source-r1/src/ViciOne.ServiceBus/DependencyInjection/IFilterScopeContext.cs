using System;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Exposes state for filter scope operations.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public interface IFilterScopeContext<TContext> :
    IAsyncDisposable
    where TContext : class, PipeContext
{
    /// <summary>Gets the filter.</summary>
    /// <remarks>
    /// The default provider caches one successful resolution. Registered filters remain with their DI or caller owner;
    /// an automatically created filter is released asynchronously when supported before an owned DI scope.
    /// A failed activation may be retried. Closing rejects even cached lookups and waits for admitted resolutions and cleanup.
    /// Repeated disposal awaits the same outcome. Recursive resolution during construction is rejected.
    /// </remarks>
    IFilter<TContext> Filter { get; }
    /// <summary>Gets the context.</summary>
    TContext Context { get; }
}
