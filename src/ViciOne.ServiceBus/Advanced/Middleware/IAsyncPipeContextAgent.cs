namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>Combines asynchronous pipe-context creation with a supervised lifecycle.</summary>
/// <typeparam name="TContext">The pipe context type.</typeparam>
public interface IAsyncPipeContextAgent<TContext> :
    IAsyncPipeContextHandle<TContext>,
    IPipeContextAgent<TContext>
    where TContext : class, PipeContext
{
}
