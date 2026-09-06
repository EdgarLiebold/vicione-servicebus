namespace ViciOne.ServiceBus.Middleware;

/// <summary>Processes dynamic pipeline stages.</summary>
/// <typeparam name="TInput">The input type.</typeparam>
public interface IDynamicFilter<TInput> :
    IFilter<TInput>,
    IPipeConnector,
    IFilterObserverConnector
    where TInput : class, PipeContext
{
}


/// <summary>Processes dynamic pipeline stages.</summary>
/// <typeparam name="TInput">The input type.</typeparam>
/// <typeparam name="TKey">The key used for lookup.</typeparam>
public interface IDynamicFilter<TInput, in TKey> :
    IFilter<TInput>,
    IPipeConnector,
    IKeyPipeConnector<TKey>,
    IFilterObserverConnector
    where TInput : class, PipeContext
    where TKey : notnull
{
}
