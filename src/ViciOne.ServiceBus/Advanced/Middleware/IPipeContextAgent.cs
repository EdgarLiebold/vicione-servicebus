namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>Combines ownership of a pipe context with a supervised lifecycle.</summary>
/// <typeparam name="TContext">The pipe context type.</typeparam>
public interface IPipeContextAgent<TContext> :
    IPipeContextHandle<TContext>,
    IAgent
    where TContext : class, PipeContext
{
}
