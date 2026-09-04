using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

/// <summary>
/// A source provides the context which is sent to the specified pipe.
/// </summary>
/// <typeparam name="TContext">The pipe context type</typeparam>
public interface IPipeContextSource<out TContext> :
    IProbeSite
    where TContext : class, PipeContext
{
    /// <summary>
    /// Send a context from the source through the specified pipe
    /// </summary>
    /// <param name="pipe">The destination pipe</param>
    /// <param name="cancellationToken">The cancellationToken, which should be included in the context</param>
    Task SendAsync(IPipe<TContext> pipe, CancellationToken cancellationToken = default);
}


/// <summary>
/// A source which provides the context using the input context to select the appropriate source.
/// </summary>
/// <typeparam name="TContext">The output context type</typeparam>
/// <typeparam name="TInput">The input context type</typeparam>
public interface IPipeContextSource<out TContext, in TInput> :
    IProbeSite
    where TContext : class, PipeContext
    where TInput : class, PipeContext
{
    /// <summary>
    /// Send a context from the source through the specified pipe, using the input context to select the proper source.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="pipe"></param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task SendAsync(TInput context, IPipe<TContext> pipe, CancellationToken cancellationToken = default);
}
