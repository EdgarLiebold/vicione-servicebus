using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>Processes one pipeline context asynchronously.</summary>
/// <typeparam name="TContext">The context accepted by the pipe.</typeparam>
public interface IPipe<in TContext> :
    IProbeSite
    where TContext : class, PipeContext
{
    /// <summary>Sends a context through the pipe.</summary>
    /// <param name="context">The context to process.</param>
    /// <returns>An awaitable result that propagates downstream completion, cancellation, and failure.</returns>
    Task SendAsync(TContext context);
}
