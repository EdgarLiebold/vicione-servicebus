using System.Threading;

namespace ViciOne.ServiceBus.Agents;

/// <summary>Creates an owned pipe context and handles for individual active uses.</summary>
/// <typeparam name="TContext">The context type.</typeparam>
public interface IPipeContextFactory<TContext>
    where TContext : class, PipeContext
{
    /// <summary>Creates the owned pipe context.</summary>
    /// <param name="supervisor">The supervisor that owns the context.</param>
    /// <returns>The owned context handle.</returns>
    IPipeContextAgent<TContext> CreateContext(ISupervisor supervisor);

    /// <summary>Creates a handle for one active use of the owned context.</summary>
    /// <param name="supervisor">The supervisor that owns the active use.</param>
    /// <param name="context">The owned context handle.</param>
    /// <param name="cancellationToken">The token that cancels creation of the active handle.</param>
    /// <returns>The active context handle.</returns>
    IActivePipeContextAgent<TContext> CreateActiveContext(ISupervisor supervisor, PipeContextHandle<TContext> context,
        CancellationToken cancellationToken = default);
}
