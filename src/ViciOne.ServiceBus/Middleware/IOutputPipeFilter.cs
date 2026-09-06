namespace ViciOne.ServiceBus.Middleware;

/// <summary>Processes output pipe pipeline stages.</summary>
/// <typeparam name="TInput">The input type.</typeparam>
/// <typeparam name="TOutput">The output type.</typeparam>
public interface IOutputPipeFilter<TInput, out TOutput> :
    IFilter<TInput>,
    IPipeConnector<TOutput>,
    IFilterObserverConnector<TOutput>
    where TInput : class, PipeContext
    where TOutput : class, PipeContext
{
}


/// <summary>Processes output pipe pipeline stages.</summary>
/// <typeparam name="TInput">The input type.</typeparam>
/// <typeparam name="TOutput">The output type.</typeparam>
/// <typeparam name="TKey">The key used for lookup.</typeparam>
public interface IOutputPipeFilter<TInput, out TOutput, in TKey> :
    IOutputPipeFilter<TInput, TOutput>,
    IKeyPipeConnector<TKey>
    where TInput : class, PipeContext
    where TOutput : class, PipeContext
{
}
