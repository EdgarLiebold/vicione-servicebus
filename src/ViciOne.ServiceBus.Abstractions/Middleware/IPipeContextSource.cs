using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>Creates an operation context and sends it through a supplied pipeline.</summary>
/// <typeparam name="TContext">The context type produced by the source.</typeparam>
public interface IPipeContextSource<out TContext> :
    IProbeSite
    where TContext : class, PipeContext
{
    /// <summary>Creates a context and sends it through the supplied pipeline.</summary>
    /// <param name="pipe">The pipeline that processes the context.</param>
    /// <param name="cancellationToken">The token that cancels context creation or pipeline execution.</param>
    /// <returns>A task that completes when context creation and pipeline execution complete.</returns>
    Task SendAsync(IPipe<TContext> pipe, CancellationToken cancellationToken = default);
}


/// <summary>Selects and creates an operation context from another pipeline context.</summary>
/// <typeparam name="TContext">The context type produced by the source.</typeparam>
/// <typeparam name="TInput">The context type used to select and create the output context.</typeparam>
public interface IPipeContextSource<out TContext, in TInput> :
    IProbeSite
    where TContext : class, PipeContext
    where TInput : class, PipeContext
{
    /// <summary>Creates a context from the input context and sends it through the supplied pipeline.</summary>
    /// <param name="context">The input context used to select and create the output context.</param>
    /// <param name="pipe">The pipeline that processes the output context.</param>
    /// <param name="cancellationToken">The token that cancels context creation or pipeline execution.</param>
    /// <returns>A task that completes when context selection, creation, and pipeline execution complete.</returns>
    Task SendAsync(TInput context, IPipe<TContext> pipe, CancellationToken cancellationToken = default);
}
