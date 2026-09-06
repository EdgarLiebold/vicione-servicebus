using System;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Processes tee pipeline stages.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public interface ITeeFilter<TContext> :
    IFilter<TContext>,
    IPipeConnector<TContext>
    where TContext : class, PipeContext
{
    /// <summary>Gets the count.</summary>
    int Count { get; }
}


/// <summary>Processes tee pipeline stages.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
/// <typeparam name="TKey">The key used for lookup.</typeparam>
public interface ITeeFilter<TContext, in TKey> :
    ITeeFilter<TContext>,
    IKeyPipeConnector<TKey>
    where TContext : class, PipeContext
{
}
