using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>
/// Defines the contract for pipe.
/// </summary>
/// <typeparam name="TContext">The t context type.</typeparam>
public interface IPipe<in TContext> :
    IProbeSite
    where TContext : class, PipeContext
{
    /// <summary>
    /// The base primitive, Send delivers the pipe context of T to the pipe.
    /// </summary>
    /// <param name="context">The pipe context of type T</param>
    /// <returns>A task which is completed once the pipe has processed the context</returns>
    Task SendAsync(TContext context);
}
