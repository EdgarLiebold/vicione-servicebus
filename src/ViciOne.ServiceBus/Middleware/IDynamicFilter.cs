namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Defines the contract for dynamic filter.
/// </summary>
/// <typeparam name="TInput">The t input type.</typeparam>
public interface IDynamicFilter<TInput> :
    IFilter<TInput>,
    IPipeConnector,
    IFilterObserverConnector
    where TInput : class, PipeContext
{
}


/// <summary>
/// Defines the contract for dynamic filter.
/// </summary>
/// <typeparam name="TInput">The t input type.</typeparam>
/// <typeparam name="TKey">The t key type.</typeparam>
public interface IDynamicFilter<TInput, in TKey> :
    IFilter<TInput>,
    IPipeConnector,
    IKeyPipeConnector<TKey>,
    IFilterObserverConnector
    where TInput : class, PipeContext
    where TKey : notnull
{
}
