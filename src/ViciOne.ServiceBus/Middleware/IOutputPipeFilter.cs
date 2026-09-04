namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Defines the contract for output pipe filter.
/// </summary>
/// <typeparam name="TInput">The t input type.</typeparam>
/// <typeparam name="TOutput">The t output type.</typeparam>
public interface IOutputPipeFilter<TInput, out TOutput> :
    IFilter<TInput>,
    IPipeConnector<TOutput>,
    IFilterObserverConnector<TOutput>
    where TInput : class, PipeContext
    where TOutput : class, PipeContext
{
}


/// <summary>
/// Defines the contract for output pipe filter.
/// </summary>
/// <typeparam name="TInput">The t input type.</typeparam>
/// <typeparam name="TOutput">The t output type.</typeparam>
/// <typeparam name="TKey">The t key type.</typeparam>
public interface IOutputPipeFilter<TInput, out TOutput, in TKey> :
    IOutputPipeFilter<TInput, TOutput>,
    IKeyPipeConnector<TKey>
    where TInput : class, PipeContext
    where TOutput : class, PipeContext
{
}
