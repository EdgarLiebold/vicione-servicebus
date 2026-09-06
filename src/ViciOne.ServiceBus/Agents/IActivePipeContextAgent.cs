namespace ViciOne.ServiceBus.Agents;

/// <summary>An active use of a pipe context as an agent.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public interface IActivePipeContextAgent<TContext> :
    ActivePipeContextHandle<TContext>,
    IAgent
    where TContext : class, PipeContext
{
}
