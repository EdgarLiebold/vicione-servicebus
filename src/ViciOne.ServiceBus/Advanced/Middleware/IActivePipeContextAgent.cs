namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>Combines one borrowed pipe-context use with a supervised lifecycle.</summary>
/// <typeparam name="TContext">The pipe context type.</typeparam>
public interface IActivePipeContextAgent<TContext> :
    IActivePipeContextHandle<TContext>,
    IAgent
    where TContext : class, PipeContext
{
}
