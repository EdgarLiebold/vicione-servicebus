namespace ViciOne.ServiceBus.Agents;

/// <summary>
/// Defines the contract for pipe context agent.
/// </summary>
/// <typeparam name="TContext">The t context type.</typeparam>
public interface IPipeContextAgent<TContext> :
    PipeContextHandle<TContext>,
    IAgent
    where TContext : class, PipeContext
{
}
