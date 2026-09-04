namespace ViciOne.ServiceBus.Agents;

/// <summary>
/// Defines the contract for async pipe context agent.
/// </summary>
/// <typeparam name="TContext">The t context type.</typeparam>
public interface IAsyncPipeContextAgent<TContext> :
    IAsyncPipeContextHandle<TContext>,
    IPipeContextAgent<TContext>
    where TContext : class, PipeContext
{
}
