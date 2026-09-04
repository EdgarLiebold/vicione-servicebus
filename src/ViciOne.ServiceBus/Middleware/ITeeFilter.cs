using System;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Defines the contract for tee filter.
/// </summary>
/// <typeparam name="TContext">The t context type.</typeparam>
public interface ITeeFilter<TContext> :
    IFilter<TContext>,
    IPipeConnector<TContext>
    where TContext : class, PipeContext
{
    /// <summary>
    /// Gets the count value.
    /// </summary>
    int Count { get; }
}


/// <summary>
/// Defines the contract for tee filter.
/// </summary>
/// <typeparam name="TContext">The t context type.</typeparam>
/// <typeparam name="TKey">The t key type.</typeparam>
public interface ITeeFilter<TContext, in TKey> :
    ITeeFilter<TContext>,
    IKeyPipeConnector<TKey>
    where TContext : class, PipeContext
{
}


/// <summary>
/// Defines the contract for request id tee filter.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IRequestIdTeeFilter<TMessage> :
    ITeeFilter<ConsumeContext<TMessage>>,
    IKeyPipeConnector<TMessage, Guid>
    where TMessage : class
{
}
