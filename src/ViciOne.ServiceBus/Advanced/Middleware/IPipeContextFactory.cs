using System.Threading;

namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>Creates one owned pipe context and borrowed handles for concurrent uses.</summary>
/// <typeparam name="TContext">The pipe context type.</typeparam>
public interface IPipeContextFactory<TContext>
    where TContext : class, PipeContext
{
    /// <summary>Creates the handle that owns a pipe context.</summary>
    /// <param name="supervisor">The supervisor that owns the context.</param>
    /// <returns>The supervised handle that owns the context.</returns>
    IPipeContextAgent<TContext> CreateContext(ISupervisor supervisor);

    /// <summary>Creates a supervised handle for one use of the owned context.</summary>
    /// <param name="supervisor">The supervisor that owns the active use.</param>
    /// <param name="context">The handle that owns the shared context.</param>
    /// <param name="cancellationToken">The token that cancels acquisition of the borrowed handle.</param>
    /// <returns>The supervised borrowed handle.</returns>
    IActivePipeContextAgent<TContext> CreateActiveContext(ISupervisor supervisor, IPipeContextHandle<TContext> context,
        CancellationToken cancellationToken = default);
}
